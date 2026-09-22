using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

/// <summary>Levantado quando se tenta aprovar uma versão cujo pipeline não produziu itens.</summary>
public sealed class VersaoSemItensException(string mensagem) : Exception(mensagem);

/// <summary>Levantado quando uma transição de status da versão não é válida.</summary>
public sealed class TransicaoDeStatusInvalidaException(string mensagem) : Exception(mensagem);

/// <summary>
/// O comunicado de uma <see cref="Release"/> escrito para um público específico
/// (Cliente, Comercial, Suporte, Interno) — diferencial documentado no enunciado.
///
/// Existe como entidade própria, e não como campo em <see cref="ItemComunicado"/>,
/// porque o que varia por audiência não é só o texto de cada item: o título e o
/// resumo executivo também mudam, e principalmente a <b>revisão humana é por
/// público</b>. Alguém pode aprovar a versão do Cliente e ainda estar ajustando a do
/// Suporte; com um único status na Release isso seria impossível de representar.
///
/// É esta entidade — não a Release — que responde "pode exportar?". Ver
/// <see cref="ComunicadoExportado"/>.
/// </summary>
public sealed class VersaoComunicado
{
    private List<ItemComunicado> _itens = [];

    /// <summary>Uso exclusivo do EF Core para materializar a entidade vinda do banco.</summary>
    private VersaoComunicado()
    {
    }

    public VersaoComunicado(Guid releaseId, PublicoAlvo publico)
    {
        Id = Guid.NewGuid();
        ReleaseId = releaseId;
        Publico = publico;
        Status = StatusRevisao.AguardandoRevisao;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private init; }

    public Guid ReleaseId { get; private init; }

    public PublicoAlvo Publico { get; private init; }

    public StatusRevisao Status { get; private set; }

    /// <summary>Título executivo desta audiência — pode diferir entre públicos.</summary>
    public string? TituloExecutivo { get; private set; }

    /// <summary>Resumo executivo desta audiência — pode diferir entre públicos.</summary>
    public string? ResumoExecutivo { get; private set; }

    public IReadOnlyList<ItemComunicado> Itens => _itens;

    /// <summary>
    /// Itens que efetivamente vão para o comunicado publicado: os que o revisor não
    /// excluiu. É esta lista que a camada de exportação deve renderizar, nunca
    /// <see cref="Itens"/> cru.
    /// </summary>
    public IReadOnlyList<ItemComunicado> ItensPublicaveis =>
        [.. _itens.Where(item => item.Incluido)];

    public string? RevisadoPor { get; private set; }

    public DateTimeOffset? RevisadoEm { get; private set; }

    /// <summary>Justificativa registrada quando o revisor reprova — obrigatória.</summary>
    public string? MotivoReprovacao { get; private set; }

    public DateTimeOffset CriadoEm { get; private init; }

    /// <summary>
    /// Gate único que a camada de exportação (Markdown/HTML/PDF) checa antes de gerar
    /// o arquivo. Publicar sem isso é bug, não decisão de produto.
    /// </summary>
    public bool ProntaParaExportar =>
        Status == StatusRevisao.Aprovado && ItensPublicaveis.Count > 0;

    /// <summary>
    /// Preenche o conteúdo gerado pelo pipeline de IA para este público. Deixa a
    /// versão pronta para revisão humana — nunca pronta para publicação direta.
    /// </summary>
    public void PreencherConteudo(
        IEnumerable<ItemComunicado> itens,
        string tituloExecutivo,
        string resumoExecutivo)
    {
        _itens = [.. itens];
        TituloExecutivo = tituloExecutivo;
        ResumoExecutivo = resumoExecutivo;
        Status = StatusRevisao.AguardandoRevisao;
    }

    /// <summary>
    /// Único caminho válido para esta versão avançar para Aprovado.
    ///
    /// Invariantes: precisa ter itens processados pela IA, precisa sobrar ao menos um
    /// item não excluído (aprovar comunicado vazio não faz sentido), e o status
    /// precisa ser AguardandoRevisao — não dá para "pular a fila" nem reaprovar o que
    /// já foi aprovado.
    /// </summary>
    public void Aprovar(string revisadoPor, DateTimeOffset agora)
    {
        if (_itens.Count == 0)
        {
            throw new VersaoSemItensException(
                $"Versão {Publico.ParaValor()} não tem itens processados pelo pipeline de IA.");
        }

        if (ItensPublicaveis.Count == 0)
        {
            throw new VersaoSemItensException(
                $"Versão {Publico.ParaValor()} teve todos os itens excluídos na revisão — "
                + "não há comunicado a publicar.");
        }

        if (Status != StatusRevisao.AguardandoRevisao)
        {
            throw new TransicaoDeStatusInvalidaException(
                $"Versão {Publico.ParaValor()} está em status {Status.ParaValor()}, "
                + "não pode ser aprovada sem passar por aguardando_revisao.");
        }

        Status = StatusRevisao.Aprovado;
        RevisadoPor = revisadoPor;
        RevisadoEm = agora;
        MotivoReprovacao = null;
    }

    /// <summary>
    /// O revisor olhou e considerou o resultado inaceitável. Diferente de excluir
    /// itens: aqui o comunicado inteiro volta para a fila, e o motivo fica registrado
    /// para orientar o reprocessamento.
    /// </summary>
    public void Reprovar(string revisadoPor, string motivo, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException(
                "Reprovar exige motivo — é o que orienta o reprocessamento.",
                nameof(motivo));
        }

        if (Status != StatusRevisao.AguardandoRevisao)
        {
            throw new TransicaoDeStatusInvalidaException(
                $"Versão {Publico.ParaValor()} está em status {Status.ParaValor()}, "
                + "só faz sentido reprovar o que está aguardando revisão.");
        }

        Status = StatusRevisao.Reprovado;
        RevisadoPor = revisadoPor;
        RevisadoEm = agora;
        MotivoReprovacao = motivo;
    }

    /// <summary>
    /// Devolve uma versão já aprovada para revisão — caso de uso real: erro descoberto
    /// depois da aprovação, às vezes depois de já ter exportado.
    ///
    /// Não apaga <see cref="ComunicadoExportado"/> anteriores de propósito: o que já
    /// foi publicado aconteceu, e o histórico de auditoria não pode ser reescrito por
    /// uma correção posterior.
    /// </summary>
    public void Reabrir()
    {
        if (Status != StatusRevisao.Aprovado)
        {
            throw new TransicaoDeStatusInvalidaException(
                $"Versão {Publico.ParaValor()} está em status {Status.ParaValor()}, "
                + "só faz sentido reabrir o que já foi aprovado.");
        }

        Status = StatusRevisao.AguardandoRevisao;
        RevisadoPor = null;
        RevisadoEm = null;
    }
}
