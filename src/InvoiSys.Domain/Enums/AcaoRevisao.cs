namespace InvoiSys.Domain.Enums;

public enum AcaoRevisao
{
    Processar,
    Aprovar,
    Reprovar,
    Reabrir,
    EditarItem,
    ExcluirItem,
    ReincluirItem,
}

public static class AcaoRevisaoExtensions
{
    private static readonly Dictionary<AcaoRevisao, string> PorEnum = new()
    {
        [AcaoRevisao.Processar] = "processar",
        [AcaoRevisao.Aprovar] = "aprovar",
        [AcaoRevisao.Reprovar] = "reprovar",
        [AcaoRevisao.Reabrir] = "reabrir",
        [AcaoRevisao.EditarItem] = "editar_item",
        [AcaoRevisao.ExcluirItem] = "excluir_item",
        [AcaoRevisao.ReincluirItem] = "reincluir_item",
    };

    public static string ParaValor(this AcaoRevisao acao) => PorEnum[acao];
}
