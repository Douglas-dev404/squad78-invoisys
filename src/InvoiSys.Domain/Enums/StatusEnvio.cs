namespace InvoiSys.Domain.Enums;

public enum StatusEnvio
{
    Enviado,
    Falhou,
}

public static class StatusEnvioExtensions
{
    private static readonly Dictionary<StatusEnvio, string> PorEnum = new()
    {
        [StatusEnvio.Enviado] = "enviado",
        [StatusEnvio.Falhou] = "falhou",
    };

    public static string ParaValor(this StatusEnvio status) => PorEnum[status];

    public static StatusEnvio DeValor(string valor) =>
        PorEnum.First(par => par.Value == valor).Key;
}
