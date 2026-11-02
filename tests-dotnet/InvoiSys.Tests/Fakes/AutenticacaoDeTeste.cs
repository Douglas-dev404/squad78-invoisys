using System.Net.Http.Headers;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Fakes;

public static class AutenticacaoDeTeste
{
    public const string Emissor = "invoisys";

    public const string Audiencia = "invoisys-api";

    public static readonly Usuario Revisor =
        new("Victor Revisor", "victor@invoisys.com", "hash", "release_manager");

    public static Usuario UmRevisor(string nome = "Revisora") =>
        new(nome, $"revisor-{Guid.NewGuid():N}@invoisys.com", "hash", "release_manager");

    public static string CriarToken(
        Usuario usuario,
        TimeProvider? relogio = null,
        string? chave = null,
        double horas = 8)
    {
        var opcoes = new AuthOptions
        {
            Jwt = new JwtOptions
            {
                Chave = chave ?? ConfiguracaoDeTestes.ChaveJwt,
                Emissor = Emissor,
                Audiencia = Audiencia,
                ExpiracaoHoras = horas,
            },
        };

        return new JwtEmissorToken(Options.Create(opcoes), relogio ?? TimeProvider.System)
            .Emitir(usuario).Token;
    }

    public static IServiceCollection ComUsuarios(this IServiceCollection services, params Usuario[] usuarios)
    {
        var repositorio = new FakeUsuarioRepository();
        repositorio.Usuarios.AddRange(usuarios);

        services.RemoveAll<IUsuarioRepository>();
        services.AddSingleton<IUsuarioRepository>(repositorio);
        return services;
    }

    public static HttpClient ComoUsuario(this HttpClient client, Usuario usuario, string? token = null)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token ?? CriarToken(usuario));
        return client;
    }

    public static HttpClient CriarClienteAutenticado(
        this WebApplicationFactory<Program> app,
        Usuario? usuario = null) =>
        app.CreateClient().ComoUsuario(usuario ?? Revisor);
}
