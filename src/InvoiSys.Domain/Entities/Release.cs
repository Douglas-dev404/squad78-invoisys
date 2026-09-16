using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

/// <summary>Levantado quando algo tenta publicar uma Release sem aprovação humana.</summary>
public sealed class RevisaoHumanaObrigatoriaException(string mensagem) : Exception(mensagem);

/// <summary>Levantado quando se tenta aprovar uma Release cujo pipeline de IA não rodou.</summary>
public sealed class ReleaseSemItensProcessadosException(string mensagem) : Exception(mensagem);

/// <summary>
/// Release — agregado raiz do domínio.
///
/// Dona do ciclo de vida do comunicado: recebe histórias brutas do Jira, guarda o
/// resultado do pipeline de IA (itens categorizados), e só libera publicação depois
/// de aprovação humana explícita. Essa regra vive aqui como método, não como `if`
/// espalhado pela API ou pelo service — é a garantia de que ninguém consegue publicar
/// sem o gate, não importa por qual caminho de código chegue até este objeto.
///
/// Por isso as coleções são expostas como somente-leitura e mutadas apenas pelos
/// métodos abaixo: um `List` público deixaria qualquer camada injetar itens sem
/// passar pelo ciclo de vida.
///
/// Ver documentação interna de Regras de Negócio.
/// </summary>
public sealed class Release
{
    private readonly List<HistoriaJira> _historias;
    private List<ItemComunicado> _itens = [];

    public Release(string chaveJira, IEnumerable<HistoriaJira> historias)
    {
        ChaveJira = chaveJira;
        _historias = [.. historias];
    }

    /// <summary>Ex: "RELEASE-2026-08".</summary>
    public string ChaveJira { get; }

    public IReadOnlyList<HistoriaJira> Historias => _historias;

    public StatusPipeline Status { get; private set; } = StatusPipeline.Pendente;

    public string? TituloExecutivo { get; private set; }

    public string? ResumoExecutivo { get; private set; }

    public IReadOnlyList<ItemComunicado> Itens => _itens;

    public string? AprovadoPor { get; private set; }

    public DateTimeOffset? AprovadoEm { get; private set; }

    public void MarcarProcessando() => Status = StatusPipeline.Processando;

    /// <summary>
    /// Chamado pelo orquestrador de IA ao fim do estágio 5 do pipeline. Deixa a
    /// Release pronta para revisão humana — nunca pronta para publicação direta.
    /// </summary>
    public void ConcluirProcessamento(
        IEnumerable<ItemComunicado> itens,
        string tituloExecutivo,
        string resumoExecutivo)
    {
        _itens = [.. itens];
        TituloExecutivo = tituloExecutivo;
        ResumoExecutivo = resumoExecutivo;
        Status = StatusPipeline.AguardandoRevisao;
    }

    public void MarcarFalha() => Status = StatusPipeline.Falhou;

    /// <summary>
    /// Único caminho válido para uma Release avançar para o status Aprovado.
    ///
    /// Invariante de negócio: uma Release sem itens processados pela IA não pode ser
    /// aprovada (nada pra revisar), e o status precisa estar AguardandoRevisao — não
    /// dá pra "pular a fila" de Pendente direto pra Aprovado.
    /// </summary>
    public void Aprovar(string aprovadoPor, DateTimeOffset agora)
    {
        if (_itens.Count == 0)
        {
            throw new ReleaseSemItensProcessadosException(
                $"Release {ChaveJira} não tem itens processados pelo pipeline de IA.");
        }

        if (Status != StatusPipeline.AguardandoRevisao)
        {
            throw new RevisaoHumanaObrigatoriaException(
                $"Release {ChaveJira} está em status {Status.ParaValor()}, "
                + "não pode ser aprovada sem passar por aguardando_revisao.");
        }

        Status = StatusPipeline.Aprovado;
        AprovadoPor = aprovadoPor;
        AprovadoEm = agora;
    }

    /// <summary>
    /// Gate único que toda camada de exportação (Markdown/HTML/PDF) deve checar antes
    /// de gerar o arquivo final. Publicar sem isso é bug, não decisão de produto.
    /// </summary>
    public bool ProntaParaExportar => Status == StatusPipeline.Aprovado;
}
