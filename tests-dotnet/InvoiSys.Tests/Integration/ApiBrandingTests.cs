using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class ApiBrandingTests(PostgresContainerFixture fixture)
{
    private WebApplicationFactory<Program> CriarApp() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<InvoiSysDbContext>();
                services.AddScoped(_ => fixture.CriarContexto());
                services.AddAuthorization(opcoes => opcoes.FallbackPolicy =
                    new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
            }));

    [Fact]
    public async Task Destaques_sao_publicos_ordenados_e_sem_inativos_com_Postgres_real()
    {
        var primeiro = new DestaqueHero("Primeiro", "Descrição inicial", "task_alt", 10);
        var segundo = new DestaqueHero("Segundo", "Outra descrição", "auto_awesome", 20);
        var inativo = new DestaqueHero("Inativo", "Não exibir", "verified_user", 15);
        inativo.Desativar();

        await using (var contexto = fixture.CriarContexto())
        {
            contexto.DestaquesHero.AddRange(segundo, inativo, primeiro);
            await contexto.SaveChangesAsync();
        }

        using var app = CriarApp();
        using var client = app.CreateClient();

        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var resposta = await client.GetAsync("/api/v1/branding/highlights");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var itens = corpo.EnumerateArray().ToList();
        itens.Should().NotContain(d => d.GetProperty("id").GetGuid() == inativo.Id);

        var semeados = itens.Where(d =>
            d.GetProperty("id").GetGuid() == primeiro.Id || d.GetProperty("id").GetGuid() == segundo.Id)
            .ToList();
        semeados.Select(d => d.GetProperty("id").GetGuid()).Should().Equal(primeiro.Id, segundo.Id);
        semeados[0].EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo("id", "titulo", "descricao", "icone");
        semeados[0].GetProperty("titulo").GetString().Should().Be(primeiro.Titulo);
        semeados[0].GetProperty("descricao").GetString().Should().Be(primeiro.Descricao);
        semeados[0].GetProperty("icone").GetString().Should().Be(primeiro.Icone);
    }
}
