using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Pipeline;

public sealed class PipelineGeracaoReleaseNote(
    IJiraClient jiraClient,
    ILlmProvider llmProvider,
    IReleaseRepository releaseRepository,
    IBloqueioProcessamento bloqueio,
    string? modeloLlm = null,
    IMonitorExecucao? monitor = null)
{
    private readonly IMonitorExecucao? _monitor = monitor;

    private readonly IJiraClient _jira = jiraClient;
    private readonly ILlmProvider _llm = llmProvider;
    private readonly IReleaseRepository _releaseRepository = releaseRepository;
    private readonly IBloqueioProcessamento _bloqueio = bloqueio;

    private readonly string? _modeloLlm = modeloLlm;

    /// <summary>
    /// Processa a Release sob uma trava por chave (ADR-031): uma segunda execução simultânea da mesma
    /// Release é recusada na hora, antes de chamar Jira e IA, em vez de gastar as chamadas pagas e só
    /// esbarrar no UNIQUE de <c>ChaveJira</c> no fim.
    /// </summary>
    /// <exception cref="ProcessamentoEmAndamentoException">a mesma Release já está sendo processada.</exception>
    public async Task<Release> ExecutarAsync(
        string chaveRelease,
        CancellationToken cancellationToken = default)
        => await ExecutarAsync(chaveRelease, null, cancellationToken);

    public async Task<Release> ExecutarAsync(
        string chaveRelease,
        IReadOnlyCollection<string>? chavesHistoriasSelecionadas,
        CancellationToken cancellationToken = default)
    {
        using var escopoMonitor = _monitor?.IniciarEscopo(chaveRelease);

        await using var trava = await _bloqueio.TentarAdquirirAsync(chaveRelease, cancellationToken)
            ?? throw new ProcessamentoEmAndamentoException(chaveRelease);

        _monitor?.Publicar(TipoEventoExecucao.PipelineIniciado, $"Processamento da Release {chaveRelease} iniciado.", modelo: _modeloLlm);

        return await ExecutarSemTravaAsync(chaveRelease, chavesHistoriasSelecionadas, cancellationToken);
    }

    private async Task<Release> ExecutarSemTravaAsync(
        string chaveRelease,
        IReadOnlyCollection<string>? chavesHistoriasSelecionadas,
        CancellationToken cancellationToken)
    {
        // Reusar a instância carregada: SalvarAsync decide INSERT/UPDATE pelo rastreamento do EF.
        var release = await _releaseRepository.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken);

        release?.GarantirQuePodeReprocessar();

        Etapa("Buscando as histórias da versão no Jira.");
        var historiasDisponiveis = await _jira.BuscarHistoriasDaReleaseAsync(chaveRelease, cancellationToken);
        var historias = SelecionarHistorias(chaveRelease, historiasDisponiveis, chavesHistoriasSelecionadas);
        Etapa($"{historias.Count} história(s) entram no processamento: {string.Join(", ", historias.Select(h => h.Chave))}.");

        if (release is null)
        {
            release = new Release(chaveRelease, historias);
        }
        else
        {
            release.AtualizarHistorias(historias);
        }

        release.MarcarProcessando();

        var execucao = release.RegistrarExecucao(_modeloLlm);

        try
        {
            var historiasLimpas = historias.Select(ExtrairELimpar).ToList();

            Etapa("Pedindo à IA para agrupar histórias parecidas.");
            var grupos = await _llm.AgruparSemelhantesAsync(
                [.. historiasLimpas.Select(h => (h.Chave, h.TextoFonte))],
                cancellationToken);

            var itens = await ProcessarGruposAsync(historiasLimpas, grupos, cancellationToken);

            Etapa("Pedindo à IA o título e o resumo executivo.");
            var (titulo, resumo) = await _llm.GerarTituloEResumoAsync(
                [.. itens.Select(i => i.Texto)],
                cancellationToken);

            release.ConcluirProcessamento(itens, titulo, resumo);
            execucao.MarcarConcluida();
            _monitor?.Publicar(TipoEventoExecucao.PipelineConcluido, $"Release {chaveRelease} processada: {itens.Count} item(ns) no comunicado.", modelo: _modeloLlm);
        }
        catch (Exception exc)
        {
            release.MarcarFalha();
            execucao.MarcarFalha(exc.Message);
            _monitor?.Publicar(TipoEventoExecucao.PipelineFalhou, $"Falha ao processar {chaveRelease}: {exc.Message}", modelo: _modeloLlm);
            await _releaseRepository.SalvarAsync(release, cancellationToken);
            throw;
        }

        await _releaseRepository.SalvarAsync(release, cancellationToken);
        return release;
    }

    private void Etapa(string mensagem) => _monitor?.Publicar(TipoEventoExecucao.Etapa, mensagem);

    private static IReadOnlyList<HistoriaJira> SelecionarHistorias(
        string chaveRelease,
        IReadOnlyList<HistoriaJira> historiasDisponiveis,
        IReadOnlyCollection<string>? chavesSelecionadas)
    {
        if (chavesSelecionadas is null)
        {
            return historiasDisponiveis;
        }

        if (chavesSelecionadas.Count == 0 || chavesSelecionadas.Any(string.IsNullOrWhiteSpace))
        {
            throw new HistoriasSelecionadasInvalidasException(chaveRelease);
        }

        var selecionadas = chavesSelecionadas
            .Select(chave => chave.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var chavesDisponiveis = historiasDisponiveis
            .Select(historia => historia.Chave)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!selecionadas.IsSubsetOf(chavesDisponiveis))
        {
            throw new HistoriasSelecionadasInvalidasException(chaveRelease);
        }

        return historiasDisponiveis
            .Where(historia => selecionadas.Contains(historia.Chave))
            .ToList();
    }

    // Limpa o campo que TextoFonte devolve: limpar sempre DescricaoTecnica ignoraria a Release Note.
    internal static HistoriaJira ExtrairELimpar(HistoriaJira historia) =>
        historia.PossuiReleaseNoteDedicada
            ? historia with
            {
                Titulo = historia.Titulo.Trim(),
                TextoReleaseNote = Normalizar(historia.TextoReleaseNote!),
            }
            : historia with
            {
                Titulo = historia.Titulo.Trim(),
                DescricaoTecnica = Normalizar(historia.DescricaoTecnica),
            };

    private static string Normalizar(string texto) =>
        string.Join(' ', texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private async Task<List<ItemComunicado>> ProcessarGruposAsync(
        IReadOnlyList<HistoriaJira> historias,
        IReadOnlyList<IReadOnlyList<string>> grupos,
        CancellationToken cancellationToken)
    {
        // TryAdd abaixo: o Jira pode devolver a mesma chave em duas páginas.
        var porChave = new Dictionary<string, HistoriaJira>(historias.Count);
        foreach (var historia in historias)
        {
            porChave.TryAdd(historia.Chave, historia);
        }
        var itens = new List<ItemComunicado>();

        var posicao = 0;
        foreach (var grupoChaves in grupos)
        {
            posicao++;
            // O LLM pode alucinar chave: filtrar, nunca indexar direto (ADR-017).
            var chavesConhecidas = grupoChaves.Where(porChave.ContainsKey).ToList();

            if (chavesConhecidas.Count == 0)
            {
                continue;
            }

            var historiasDoGrupo = chavesConhecidas.Select(chave => porChave[chave]).ToList();
            Etapa($"Grupo {posicao}/{grupos.Count} ({string.Join(", ", chavesConhecidas)}): categorizando e reescrevendo em linguagem de negócio.");

            var categoria = await _llm.CategorizarAsync(
                historiasDoGrupo[0].TextoFonte,
                cancellationToken);

            var texto = await _llm.ReescreverLinguagemNegocioAsync(
                [.. historiasDoGrupo.Select(h => h.TextoFonte)],
                categoria,
                cancellationToken);

            itens.Add(new ItemComunicado(categoria, texto, chavesConhecidas));
        }

        return itens;
    }
}
