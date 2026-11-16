using FluentAssertions;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Llm;

namespace InvoiSys.Tests.Unit;

public class PromptsPorPublicoTests
{
    [Fact]
    public void Cliente_continua_usando_os_prompts_originais()
    {
        ChatCompletionsLlmProvider.ArquivoDoPublico("04_reescrever_linguagem_negocio.md", "reescrever", PublicoAlvo.Cliente)
            .Should().Be("04_reescrever_linguagem_negocio.md");
        ChatCompletionsLlmProvider.ArquivoDoPublico("05_gerar_titulo_resumo.md", "titulo_resumo", PublicoAlvo.Cliente)
            .Should().Be("05_gerar_titulo_resumo.md");
    }

    [Theory]
    [InlineData(PublicoAlvo.Comercial)]
    [InlineData(PublicoAlvo.Suporte)]
    [InlineData(PublicoAlvo.Interno)]
    public void Cada_publico_tem_os_dois_prompts_com_os_mesmos_placeholders(PublicoAlvo publico)
    {
        var prompts = new PromptLoader();

        var reescrever = prompts.Carregar(
            ChatCompletionsLlmProvider.ArquivoDoPublico("04_reescrever_linguagem_negocio.md", "reescrever", publico));
        var tituloResumo = prompts.Carregar(
            ChatCompletionsLlmProvider.ArquivoDoPublico("05_gerar_titulo_resumo.md", "titulo_resumo", publico));

        reescrever.Should().Contain("{{categoria}}").And.Contain("{{textos_fonte}}");
        tituloResumo.Should().Contain("{{itens_texto}}").And.Contain("\"titulo\"").And.Contain("\"resumo\"");
    }
}
