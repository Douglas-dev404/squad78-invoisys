using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Pipeline;

public sealed class PipelineGeracaoReleaseNote(
    IJiraClient jiraClient,
    ILlmProvider llmProvider,
    IReleaseRepository releaseRepository,
    string? modeloLlm = null)
{
    private readonly IJiraClient _jira = jiraClient;
    private readonly ILlmProvider _llm = llmProvider;
    private readonly IReleaseRepository _releaseRepository = releaseRepository;

    private readonly string? _modeloLlm = modeloLlm;

    public async Task<Release> ExecutarAsync(
        string chaveRelease,
        CancellationToken cancellationToken = default)
    {
        // Reusar a instância carregada: SalvarAsync decide INSERT/UPDATE pelo rastreamento do EF.
        var release = await _releaseRepository.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken);

        release?.GarantirQuePodeReprocessar();

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

            var itens = await ProcessarGruposAsync(historiasLimpas, grupos, cancellationToken);

            var (titulo, resumo) = await _llm.GerarTituloEResumoAsync(
                [.. itens.Select(i => i.Texto)],
                cancellationToken);

            release.ConcluirProcessamento(itens, titulo, resumo);
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

            var texto = await _llm.ReescreverLinguagemNegocioAsync(
                [.. historiasDoGrupo.Select(h => h.TextoFonte)],
                categoria,
                cancellationToken);

            itens.Add(new ItemComunicado(categoria, texto, chavesConhecidas));
        }

        return itens;
    }
}
