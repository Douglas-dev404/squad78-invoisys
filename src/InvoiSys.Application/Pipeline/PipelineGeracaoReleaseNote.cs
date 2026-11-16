using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Pipeline;

public sealed class PipelineGeracaoReleaseNote(
    IJiraClient jiraClient,
    ILlmProvider llmProvider,
    IReleaseRepository releaseRepository,
    IBloqueioProcessamento bloqueio,
    string? modeloLlm = null)
{
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
    public Task<Release> ExecutarAsync(
        string chaveRelease,
        CancellationToken cancellationToken = default) =>
        ExecutarAsync(chaveRelease, [PublicoAlvo.Cliente], cancellationToken);

    public async Task<Release> ExecutarAsync(
        string chaveRelease,
        IReadOnlyCollection<PublicoAlvo> publicos,
        CancellationToken cancellationToken = default)
    {
        publicos = publicos.Count == 0 ? [PublicoAlvo.Cliente] : [.. publicos.Distinct().Order()];

        await using var trava = await _bloqueio.TentarAdquirirAsync(chaveRelease, cancellationToken)
            ?? throw new ProcessamentoEmAndamentoException(chaveRelease);

        return await ExecutarSemTravaAsync(chaveRelease, publicos, cancellationToken);
    }

    private async Task<Release> ExecutarSemTravaAsync(
        string chaveRelease,
        IReadOnlyCollection<PublicoAlvo> publicos,
        CancellationToken cancellationToken)
    {
        // Reusar a instância carregada: SalvarAsync decide INSERT/UPDATE pelo rastreamento do EF.
        var release = await _releaseRepository.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken);

        if (release is not null)
        {
            foreach (var publico in publicos)
            {
                release.GarantirQuePodeReprocessar(publico);
            }
        }

        var historias = await _jira.BuscarHistoriasDaReleaseAsync(chaveRelease, cancellationToken);

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

            var grupos = await _llm.AgruparSemelhantesAsync(
                [.. historiasLimpas.Select(h => (h.Chave, h.TextoFonte))],
                cancellationToken);

            var gruposCategorizados = await CategorizarGruposAsync(historiasLimpas, grupos, cancellationToken);

            foreach (var publico in publicos)
            {
                var itens = await ReescreverParaPublicoAsync(gruposCategorizados, publico, cancellationToken);

                var (titulo, resumo) = await _llm.GerarTituloEResumoAsync(
                    [.. itens.Select(i => i.Texto)],
                    publico,
                    cancellationToken);

                release.ConcluirProcessamento(itens, titulo, resumo, publico);
            }

            execucao.MarcarConcluida();
        }
        catch (Exception exc)
        {
            release.MarcarFalha();
            execucao.MarcarFalha(exc.Message);
            await _releaseRepository.SalvarAsync(release, cancellationToken);
            throw;
        }

        await _releaseRepository.SalvarAsync(release, cancellationToken);
        return release;
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

    private sealed record GrupoCategorizado(
        CategoriaAlteracao Categoria,
        IReadOnlyList<string> Chaves,
        IReadOnlyList<string> TextosFonte);

    private async Task<List<GrupoCategorizado>> CategorizarGruposAsync(
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

        var categorizados = new List<GrupoCategorizado>();

        foreach (var grupoChaves in grupos)
        {
            // O LLM pode alucinar chave: filtrar, nunca indexar direto (ADR-017).
            var chavesConhecidas = grupoChaves.Where(porChave.ContainsKey).ToList();

            if (chavesConhecidas.Count == 0)
            {
                continue;
            }

            var historiasDoGrupo = chavesConhecidas.Select(chave => porChave[chave]).ToList();

            var categoria = await _llm.CategorizarAsync(
                historiasDoGrupo[0].TextoFonte,
                cancellationToken);

            categorizados.Add(new GrupoCategorizado(
                categoria,
                chavesConhecidas,
                [.. historiasDoGrupo.Select(h => h.TextoFonte)]));
        }

        return categorizados;
    }

    private async Task<List<ItemComunicado>> ReescreverParaPublicoAsync(
        IReadOnlyList<GrupoCategorizado> grupos,
        PublicoAlvo publico,
        CancellationToken cancellationToken)
    {
        var itens = new List<ItemComunicado>(grupos.Count);

        foreach (var grupo in grupos)
        {
            var texto = await _llm.ReescreverLinguagemNegocioAsync(
                grupo.TextosFonte,
                grupo.Categoria,
                publico,
                cancellationToken);

            itens.Add(new ItemComunicado(grupo.Categoria, texto, grupo.Chaves));
        }

        return itens;
    }
}
