using FluentAssertions;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure;
using InvoiSys.Infrastructure.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiSys.Tests.Unit;

public class EscolhaDoProvedorLlmTests
{
    private static ServiceProvider Montar(params (string Chave, string? Valor)[] extras)
    {
        var valores = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=x;Username=x;Password=x",
            ["Auth:Jwt:Chave"] = ConfiguracaoDeTestes.ChaveJwt,
            ["Auth:RecuperacaoSenha:UrlBase"] = ConfiguracaoDeTestes.UrlRedefinicao,
        };
        foreach (var (chave, valor) in extras)
        {
            valores[chave] = valor;
        }

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new ServiceCollection().AddInfrastructure(configuracao).BuildServiceProvider();
    }

    private static ILlmProvider Resolver(params (string Chave, string? Valor)[] extras)
    {
        using var servicos = Montar(extras);
        using var escopo = servicos.CreateScope();
        return escopo.ServiceProvider.GetRequiredService<ILlmProvider>();
    }

    [Fact]
    public void Sem_a_variavel_continua_OpenRouter()
    {
        Resolver(("OpenRouter:ApiKey", "chave-de-teste")).Should().BeOfType<OpenRouterProvider>();
    }

    [Theory]
    [InlineData("OpenAi")]
    [InlineData("openai")]
    [InlineData(" OPENAI ")]
    public void Provedor_OpenAi_com_chave_usa_o_OpenAiProvider(string valor)
    {
        Resolver(("Llm:Provedor", valor), ("OpenAi:ApiKey", "chave-de-teste"))
            .Should().BeOfType<OpenAiProvider>();
    }

    [Fact]
    public void Provedor_OpenRouter_explicito_usa_o_OpenRouterProvider()
    {
        Resolver(("Llm:Provedor", "OpenRouter"), ("OpenRouter:ApiKey", "chave-de-teste"))
            .Should().BeOfType<OpenRouterProvider>();
    }

    [Fact]
    public void Sem_chave_a_API_sobe_e_o_provider_e_o_pendente()
    {
        Resolver().Should().BeOfType<ProviderPendente>();
        Resolver(("Llm:Provedor", "OpenAi")).Should().BeOfType<ProviderPendente>();
    }

    [Fact]
    public void Chave_do_outro_provedor_nao_vale()
    {
        Resolver(("Llm:Provedor", "OpenAi"), ("OpenRouter:ApiKey", "chave-de-teste"))
            .Should().BeOfType<ProviderPendente>();
        Resolver(("Llm:Provedor", "OpenRouter"), ("OpenAi:ApiKey", "chave-de-teste"))
            .Should().BeOfType<ProviderPendente>();
    }

    [Fact]
    public void Provedor_desconhecido_falha_citando_a_variavel()
    {
        var acao = () => Montar(("Llm:Provedor", "Gemini"));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*Llm__Provedor*");
    }
}
