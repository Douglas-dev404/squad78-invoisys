namespace InvoiSys.Api.Contracts;

public sealed record DashboardTotaisOut(int Aprovadas, int AguardandoRevisao, int Falharam, int Exportacoes);
public sealed record DashboardDiaOut(DateOnly Data, int Aprovadas, int Geradas);
public sealed record DashboardRecenteOut(string ChaveJira, string Status, string? TituloExecutivo, DateTimeOffset CriadoEm);
public sealed record DashboardQualidadeOut(int ItensGerados, int ItensEditados, int ItensExcluidos);
public sealed record DashboardOut(
    DashboardTotaisOut Totais,
    IReadOnlyList<DashboardDiaOut> Ultimos7Dias,
    IReadOnlyList<DashboardRecenteOut> Recentes,
    DashboardQualidadeOut Qualidade);
