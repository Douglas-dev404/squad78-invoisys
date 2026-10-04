using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

public sealed class VersaoSemItensException(string mensagem) : Exception(mensagem);

public sealed class TransicaoDeStatusInvalidaException(string mensagem) : Exception(mensagem);

public sealed class VersaoComunicado
{
    private List<ItemComunicado> _itens = [];

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

    public string? TituloExecutivo { get; private set; }

    public string? ResumoExecutivo { get; private set; }

    public IReadOnlyList<ItemComunicado> Itens => _itens;

    public IReadOnlyList<ItemComunicado> ItensPublicaveis =>
        [.. _itens.Where(item => item.Incluido)];

    public string? RevisadoPor { get; private set; }

    public DateTimeOffset? RevisadoEm { get; private set; }

    public string? MotivoReprovacao { get; private set; }

    public DateTimeOffset CriadoEm { get; private init; }

    public bool ProntaParaExportar =>
        Status == StatusRevisao.Aprovado && ItensPublicaveis.Count > 0;

    public void PreencherConteudo(
        IEnumerable<ItemComunicado> itens,
        string tituloExecutivo,
        string resumoExecutivo)
    {
        if (Status == StatusRevisao.Aprovado)
        {
            throw new TransicaoDeStatusInvalidaException(
                $"Versão {Publico.ParaValor()} está aprovada — reabra a revisão antes de "
                + "reprocessar; conteúdo aprovado por um humano não é sobrescrito pela IA.");
        }

        _itens = [.. itens];
        TituloExecutivo = tituloExecutivo;
        ResumoExecutivo = resumoExecutivo;
        Status = StatusRevisao.AguardandoRevisao;
    }

    public void Aprovar(string revisadoPor, DateTimeOffset agora)
    {
        ExigirRevisor(revisadoPor);

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

    public void EditarItem(ItemComunicado item, string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new ArgumentException(
                "A edição de um item precisa de texto — para tirar o item do comunicado, exclua-o.",
                nameof(texto));
        }

        GarantirQuePodeAlterarItem(item);
        item.EditarManualmente(texto.Trim());
    }

    public void ExcluirItem(ItemComunicado item, string? motivo)
    {
        GarantirQuePodeAlterarItem(item);
        item.Excluir(string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim());
    }

    public void ReincluirItem(ItemComunicado item)
    {
        GarantirQuePodeAlterarItem(item);
        item.Reincluir();
    }

    private void GarantirQuePodeAlterarItem(ItemComunicado item)
    {
        if (!_itens.Contains(item))
        {
            throw new InvalidOperationException(
                $"O item {item.Id} não pertence à versão {Publico.ParaValor()}.");
        }

        if (Status == StatusRevisao.Aprovado)
        {
            throw new TransicaoDeStatusInvalidaException(
                $"Versão {Publico.ParaValor()} está aprovada — reabra a revisão antes de "
                + "alterar itens; o texto aprovado por um humano não muda sem nova revisão.");
        }
    }

    public void Reprovar(string revisadoPor, string motivo, DateTimeOffset agora)
    {
        ExigirRevisor(revisadoPor);

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

    private static void ExigirRevisor(string revisadoPor)
    {
        if (string.IsNullOrWhiteSpace(revisadoPor))
        {
            throw new ArgumentException(
                "Revisão exige identificar o revisor — é a trilha de auditoria da decisão humana.",
                nameof(revisadoPor));
        }
    }
}
