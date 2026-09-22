using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Pipeline;

/// <summary>
/// Orquestra o pipeline de 5 estágios de IA sobre uma Release. Depende só das portas
/// (<see cref="IJiraClient"/>, <see cref="ILlmProvider"/>) — nunca de adapter
/// concreto. Isso é o que torna este código testável sem rede: em teste injetamos
/// fakes das portas; em produção, os adapters reais vêm pela DI do ASP.NET.
///
/// Estágio 1 (Extração e Limpeza) é normalização pura de texto — não chama LLM, fica
/// aqui mesmo como método auxiliar. Estágios 2-5 chamam a porta ILlmProvider.
/// Ver documentação interna de Regras de Negócio para o desenho completo.
/// </summary>
public sealed class PipelineGeracaoReleaseNote(
    IJiraClient jiraClient,
    ILlmProvider llmProvider,
    string? modeloLlm = null)
{
    private readonly IJiraClient _jira = jiraClient;
    private readonly ILlmProvider _llm = llmProvider;

    /// <summary>Identificação do modelo usado, só para registro no log de execução.</summary>
    private readonly string? _modeloLlm = modeloLlm;

    public async Task<Release> ExecutarAsync(
        string chaveRelease,
        CancellationToken cancellationToken = default)
    {
        var historias = await _jira.BuscarHistoriasDaReleaseAsync(chaveRelease, cancellationToken);
        var release = new Release(chaveRelease, historias);
        release.MarcarProcessando();

        // Rastreabilidade: cada rodada vira uma linha no histórico da Release, mesmo
        // que falhe — o rastro da tentativa é justamente o que o requisito de log pede.
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
            throw;
        }

        return release;
    }

    /// <summary>
    /// Estágio 1: normaliza espaços/quebras de linha do texto fonte. Não chama LLM —
    /// é limpeza determinística, não tem por que gastar tokens nisso.
    ///
    /// Limpa especificamente o campo que <see cref="HistoriaJira.TextoFonte"/> vai
    /// devolver (Release Note dedicada, se existir; senão descrição técnica) — limpar
    /// sempre DescricaoTecnica seria bug quando há Release Note, porque TextoFonte
    /// ignoraria a limpeza e devolveria o texto original sujo.
    /// </summary>
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

    /// <summary>Colapsa qualquer sequência de espaços/quebras em um único espaço.</summary>
    private static string Normalizar(string texto) =>
        string.Join(' ', texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private async Task<List<ItemComunicado>> ProcessarGruposAsync(
        IReadOnlyList<HistoriaJira> historias,
        IReadOnlyList<IReadOnlyList<string>> grupos,
        CancellationToken cancellationToken)
    {
        // ToDictionary lançaria se o Jira devolvesse a mesma chave duas vezes (issue em
        // duas páginas da paginação, por exemplo). A primeira ocorrência vence — são o
        // mesmo dado, e derrubar o processamento por isso seria desproporcional.
        var porChave = new Dictionary<string, HistoriaJira>(historias.Count);
        foreach (var historia in historias)
        {
            porChave.TryAdd(historia.Chave, historia);
        }
        var itens = new List<ItemComunicado>();

        foreach (var grupoChaves in grupos)
        {
            // O LLM pode alucinar uma chave que nunca veio do Jira. Indexar direto
            // (porChave[chave]) lançaria KeyNotFoundException e derrubaria a Release
            // inteira por causa de uma linha inventada — filtramos em vez de confiar.
            // O caso inverso (chave real que o modelo esqueceu) é tratado no adapter,
            // que a reinsere como grupo próprio: história real nunca se perde.
            var chavesConhecidas = grupoChaves.Where(porChave.ContainsKey).ToList();

            if (chavesConhecidas.Count == 0)
            {
                // Grupo formado só por chaves inexistentes: não há texto real para
                // reescrever, e gerar um item a partir do nada seria pior que omiti-lo.
                continue;
            }

            var historiasDoGrupo = chavesConhecidas.Select(chave => porChave[chave]).ToList();

            // Categoriza pela primeira história do grupo — elas já foram agrupadas por
            // serem semanticamente equivalentes, então compartilham categoria.
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
