using InvoiSys.Api.Contracts;
using InvoiSys.Application.Consulta;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/dashboard", BuscarAsync)
            .WithTags("dashboard")
            .WithName("ConsultarDashboard")
            .WithSummary("Consulta os indicadores e listas do dashboard.")
            .RequireAuthorization()
            .Produces<DashboardOut>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return rotas;
    }

    private static async Task<IResult> BuscarAsync(ConsultaDashboard consulta, CancellationToken cancellationToken)
    {
        var dados = await consulta.BuscarAsync(cancellationToken);
        return Results.Ok(new DashboardOut(
            new DashboardTotaisOut(dados.Aprovadas, dados.AguardandoRevisao, dados.Falharam, dados.Exportacoes),
            [.. dados.UltimosDias.Select(dia => new DashboardDiaOut(dia.Data, dia.Aprovadas, dia.Geradas))],
            [.. dados.Recentes.Select(release => new DashboardRecenteOut(
                release.ChaveJira, release.Status.ParaValor(), release.TituloExecutivo, release.CriadoEm))],
            new DashboardQualidadeOut(dados.ItensGerados, dados.ItensEditados, dados.ItensExcluidos)));
    }
}
