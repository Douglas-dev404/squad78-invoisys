using FluentAssertions;
using InvoiSys.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiSys.Tests.Integration;

public class AuthStartupTests
{
    private static IConfiguration Config(params (string Chave, string? Valor)[] extras)
    {
        var valores = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=x;Username=x;Password=x",
            ["Auth:RecuperacaoSenha:UrlBase"] = ConfiguracaoDeTestes.UrlRedefinicao,
        };
        foreach (var (chave, valor) in extras)
        {
            valores[chave] = valor;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
    }

    [Fact]
    public void Sem_a_chave_JWT_o_composition_root_falha_citando_a_variavel()
    {
        var acao = () => new ServiceCollection().AddInfrastructure(Config());

        acao.Should().Throw<InvalidOperationException>().WithMessage("*Auth__Jwt__Chave*");
    }

    [Fact]
    public void Sem_a_URL_de_redefinicao_o_composition_root_falha_citando_a_variavel()
    {
        var acao = () => new ServiceCollection().AddInfrastructure(Config(
            ("Auth:Jwt:Chave", ConfiguracaoDeTestes.ChaveJwt),
            ("Auth:RecuperacaoSenha:UrlBase", "")));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*Auth__RecuperacaoSenha__UrlBase*");
    }

    [Fact]
    public void Chave_curta_demais_tambem_impede_o_startup()
    {
        var acao = () => new ServiceCollection().AddInfrastructure(Config(("Auth:Jwt:Chave", "curta")));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*Auth__Jwt__Chave*");
    }

    [Fact]
    public void Com_chave_valida_o_composition_root_registra_sem_erro()
    {
        var acao = () => new ServiceCollection()
            .AddInfrastructure(Config(("Auth:Jwt:Chave", ConfiguracaoDeTestes.ChaveJwt)));

        acao.Should().NotThrow();
    }

    [Fact]
    public void API_sobe_quando_a_chave_vem_das_variaveis_de_ambiente()
    {
        using var app = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();

        using var client = app.CreateClient();

        client.Should().NotBeNull();
    }
}
