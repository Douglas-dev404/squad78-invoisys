using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Rotas de revisão humana (aprovar, reprovar, reabrir) pelo pipeline HTTP real, com o
/// repository em memória. As transições em si já são cobertas no domínio
/// (<c>VersaoComunicadoTests</c>); aqui o foco é o contrato HTTP: status, ProblemDetails
/// e o efeito na Release salva.
/// </summary>
public class ApiRevisaoTests
{
    private const string Chave = "RELEASE-2026-08";

    private static WebApplicationFactory<Program> CriarApp(IReleaseRepository repositorio) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReleaseRepository>();
                services.AddScoped(_ => repositorio);
            }));

    private static async Task<(FakeReleaseRepository Repositorio, Release Release)> ReleaseSalva(
        bool comComunicado = true)
    {
        var release = new Release(
            Chave,
            [new HistoriaJira
            {
                Chave = "INV-1",
                Titulo = "História INV-1",
                DescricaoTecnica = "Descrição técnica da implementação.",
                TipoIssue = "Story",
            }]);

        if (comComunicado)
        {
            release.ConcluirProcessamento(
                [new ItemComunicado(CategoriaAlteracao.Correcao, "Texto do comunicado.", ["INV-1"])],
                "Título da Release",
                "Resumo da Release");
        }

        var repositorio = new FakeReleaseRepository();
        await repositorio.SalvarAsync(release);
        return (repositorio, release);
    }

    private static async Task<HttpResponseMessage> Post(
        IReleaseRepository repositorio,
        string acao,
        object corpo,
        string chave = Chave)
    {
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();
        return await client.PostAsJsonAsync($"/api/v1/releases/{chave}/{acao}", corpo);
    }

    [Fact]
    public async Task Aprovar_registra_o_revisor_e_responde_204()
    {
        var (repositorio, release) = await ReleaseSalva();

        var resposta = await Post(repositorio, "aprovar", new { publico = "cliente", aprovadoPor = "victor" });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.Aprovado);
        release.VersaoCliente.RevisadoPor.Should().Be("victor");
        release.Status.Should().Be(StatusPipeline.Aprovado);
    }

    [Fact]
    public async Task Aprovar_sem_revisor_responde_422_e_nao_aprova()
    {
        var (repositorio, release) = await ReleaseSalva();

        var resposta = await Post(repositorio, "aprovar", new { publico = "cliente" });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
        release.VersaoCliente.RevisadoPor.Should().BeNull();
    }

    [Fact]
    public async Task Aprovar_sem_publico_responde_422_em_vez_de_assumir_cliente()
    {
        var (repositorio, release) = await ReleaseSalva();

        var resposta = await Post(repositorio, "aprovar", new { aprovadoPor = "victor" });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
    }

    [Fact]
    public async Task Aprovar_com_publico_invalido_responde_422()
    {
        var (repositorio, _) = await ReleaseSalva();

        var resposta = await Post(repositorio, "aprovar", new { publico = "financeiro", aprovadoPor = "victor" });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Aprovar_release_sem_comunicado_responde_409_em_ProblemDetails()
    {
        var (repositorio, _) = await ReleaseSalva(comComunicado: false);

        var resposta = await Post(repositorio, "aprovar", new { publico = "cliente", aprovadoPor = "victor" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("não tem comunicado gerado");
    }

    [Fact]
    public async Task Aprovar_versao_ja_aprovada_responde_409()
    {
        var (repositorio, release) = await ReleaseSalva();
        release.Aprovar("victor", DateTimeOffset.UtcNow);

        var resposta = await Post(repositorio, "aprovar", new { publico = "cliente", aprovadoPor = "outra.pessoa" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        release.VersaoCliente!.RevisadoPor.Should().Be("victor");
    }

    [Fact]
    public async Task Aprovar_release_inexistente_responde_404()
    {
        var resposta = await Post(
            new FakeReleaseRepository(),
            "aprovar",
            new { publico = "cliente", aprovadoPor = "victor" },
            chave: "RELEASE-INEXISTENTE");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reprovar_registra_motivo_e_revisor_e_responde_204()
    {
        var (repositorio, release) = await ReleaseSalva();

        var resposta = await Post(
            repositorio,
            "reprovar",
            new { publico = "cliente", motivo = "Texto precisa ser revisado.", revisadoPor = "victor" });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.Reprovado);
        release.VersaoCliente.MotivoReprovacao.Should().Be("Texto precisa ser revisado.");
        release.VersaoCliente.RevisadoPor.Should().Be("victor");
    }

    [Fact]
    public async Task Reprovar_sem_motivo_responde_422()
    {
        var (repositorio, release) = await ReleaseSalva();

        var resposta = await Post(
            repositorio,
            "reprovar",
            new { publico = "cliente", motivo = "", revisadoPor = "victor" });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
    }

    [Fact]
    public async Task Reabrir_versao_aprovada_limpa_o_revisor_e_responde_204()
    {
        var (repositorio, release) = await ReleaseSalva();
        release.Aprovar("victor", DateTimeOffset.UtcNow);

        var resposta = await Post(repositorio, "reabrir", new { publico = "cliente" });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
        release.VersaoCliente.RevisadoPor.Should().BeNull();
        release.VersaoCliente.RevisadoEm.Should().BeNull();
        release.Status.Should().Be(StatusPipeline.AguardandoRevisao);
    }

    [Fact]
    public async Task Reabrir_versao_nao_aprovada_responde_409()
    {
        var (repositorio, _) = await ReleaseSalva();

        var resposta = await Post(repositorio, "reabrir", new { publico = "cliente" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
