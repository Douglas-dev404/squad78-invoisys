using System.Net;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Llm;
using InvoiSys.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Unit;

/// <summary>
/// O adapter da OpenRouter, com o HTTP mockado. O foco aqui é o parsing defensivo: um
/// LLM devolve texto, não estrutura garantida, e o que quebra em produção é justamente
/// a resposta fora do formato combinado.
/// </summary>
public class OpenRouterProviderTests
{
    private static (OpenRouterProvider Provider, FakeHttpMessageHandler Handler) Criar(
        Action<FakeHttpMessageHandler> configurarRespostas)
    {
        var handler = new FakeHttpMessageHandler();
        configurarRespostas(handler);

        var http = new HttpClient(handler);
        var opcoes = Options.Create(new OpenRouterOptions
        {
            ApiKey = "chave-de-teste",
            Modelo = "openai/gpt-4o-mini",
        });

        var provider = new OpenRouterProvider(
            http,
            opcoes,
            new PromptLoader(),
            NullLogger<OpenRouterProvider>.Instance);

        return (provider, handler);
    }

    [Fact]
    public async Task Categorizar_converte_a_palavra_devolvida_no_enum()
    {
        var (provider, _) = Criar(h => h.ResponderConteudoLlm("nova_funcionalidade"));

        var categoria = await provider.CategorizarAsync("Passa a permitir emissão em lote.");

        categoria.Should().Be(CategoriaAlteracao.NovaFuncionalidade);
    }

    [Fact]
    public async Task Categorizar_tolera_aspas_e_espacos_em_volta_da_resposta()
    {
        var (provider, _) = Criar(h => h.ResponderConteudoLlm("  \"correcao\"\n"));

        var categoria = await provider.CategorizarAsync("Corrigido cálculo de imposto.");

        categoria.Should().Be(CategoriaAlteracao.Correcao);
    }

    [Fact]
    public async Task Categorizar_rejeita_categoria_fora_do_enum()
    {
        var (provider, _) = Criar(h => h.ResponderConteudoLlm("categoria_inventada"));

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        await acao.Should().ThrowAsync<LlmRespostaInvalidaException>();
    }

    [Fact]
    public async Task Agrupar_aceita_JSON_embrulhado_em_code_fence()
    {
        // Alguns modelos atrás do gateway ignoram response_format e devolvem markdown.
        var (provider, _) = Criar(h => h.ResponderConteudoLlm(
            "```json\n[[\"INV-1\", \"INV-2\"], [\"INV-3\"]]\n```"));

        var grupos = await provider.AgruparSemelhantesAsync(
            [("INV-1", "a"), ("INV-2", "b"), ("INV-3", "c")]);

        grupos.Should().HaveCount(2);
        grupos[0].Should().Equal("INV-1", "INV-2");
    }

    [Fact]
    public async Task Agrupar_isola_chave_omitida_pelo_modelo_em_vez_de_perder_a_historia()
    {
        // Invariante: toda chave de entrada sai em exatamente um grupo. Perder uma
        // história do comunicado é pior que ela aparecer sem agrupamento.
        var (provider, _) = Criar(h => h.ResponderConteudoLlm("""[["INV-1"]]"""));

        var grupos = await provider.AgruparSemelhantesAsync([("INV-1", "a"), ("INV-2", "b")]);

        grupos.SelectMany(g => g).Should().BeEquivalentTo(["INV-1", "INV-2"]);
        grupos.Should().HaveCount(2);
    }

    [Fact]
    public async Task Agrupar_rejeita_elemento_nao_string_dentro_do_grupo()
    {
        // Validação rasa ("é array?") deixaria isto passar e sujaria Origens.
        var (provider, _) = Criar(h => h.ResponderConteudoLlm("""[["INV-1", 42]]"""));

        var acao = async () => await provider.AgruparSemelhantesAsync([("INV-1", "a")]);

        await acao.Should().ThrowAsync<LlmRespostaInvalidaException>();
    }

    [Fact]
    public async Task Agrupar_rejeita_JSON_malformado()
    {
        var (provider, _) = Criar(h => h.ResponderConteudoLlm("isto não é json"));

        var acao = async () => await provider.AgruparSemelhantesAsync([("INV-1", "a")]);

        await acao.Should().ThrowAsync<LlmRespostaInvalidaException>();
    }

    [Fact]
    public async Task Gerar_titulo_e_resumo_extrai_os_dois_campos()
    {
        var (provider, _) = Criar(h => h.ResponderConteudoLlm(
            """{"titulo": "Release de agosto", "resumo": "Melhorias na emissão."}"""));

        var (titulo, resumo) = await provider.GerarTituloEResumoAsync(["item um"]);

        titulo.Should().Be("Release de agosto");
        resumo.Should().Be("Melhorias na emissão.");
    }

    [Fact]
    public async Task Gerar_titulo_e_resumo_rejeita_objeto_sem_os_campos_esperados()
    {
        var (provider, _) = Criar(h => h.ResponderConteudoLlm("""{"titulo": "só o título"}"""));

        var acao = async () => await provider.GerarTituloEResumoAsync(["item um"]);

        await acao.Should().ThrowAsync<LlmRespostaInvalidaException>();
    }

    [Fact]
    public async Task Erro_HTTP_da_OpenRouter_vira_excecao_de_dominio_do_adapter()
    {
        var (provider, _) = Criar(h =>
            h.ResponderJson("""{"error": {"message": "rate limited"}}""",
                HttpStatusCode.TooManyRequests));

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        await acao.Should().ThrowAsync<OpenRouterApiException>();
    }

    [Fact]
    public async Task Resposta_sem_choices_e_reportada_como_invalida()
    {
        var (provider, _) = Criar(h => h.ResponderJson("""{"id": "abc"}"""));

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        await acao.Should().ThrowAsync<LlmRespostaInvalidaException>();
    }

    [Fact]
    public async Task Corpo_enviado_contem_a_mensagem_completa_e_nao_objeto_vazio()
    {
        // Prova direta da serialização: o array messages precisa carregar role e
        // content de verdade.
        var (provider, handler) = Criar(h => h.ResponderConteudoLlm("melhoria"));

        await provider.CategorizarAsync("texto de teste");

        var corpo = handler.CorposEnviados.Single();
        corpo.Should().NotContain("\"messages\":[{}]", "objeto anônimo serializado como {}");

        using var doc = JsonDocument.Parse(corpo);
        var mensagem = doc.RootElement.GetProperty("messages")[0];
        mensagem.GetProperty("role").GetString().Should().Be("user");
        mensagem.GetProperty("content").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("model").GetString().Should().Be("openai/gpt-4o-mini");
    }

    [Fact]
    public async Task Prompt_enviado_vem_do_arquivo_versionado_com_o_texto_interpolado()
    {
        var (provider, handler) = Criar(h => h.ResponderConteudoLlm("melhoria"));

        await provider.CategorizarAsync("Emissão de notas ficou mais rápida.");

        // Desserializa em vez de casar no texto cru: o System.Text.Json escapa
        // caracteres não-ASCII no corpo enviado, então procurar acento na string
        // serializada falharia mesmo com o prompt correto.
        var corpo = handler.CorposEnviados.Should().ContainSingle().Subject;
        using var documento = JsonDocument.Parse(corpo);
        var promptEnviado = documento.RootElement
            .GetProperty("messages")[0]
            .GetProperty("content")
            .GetString()!;

        promptEnviado.Should().NotBeEmpty(
            "regressão: com payload tipado como Dictionary<string, object>, o "
            + "System.Text.Json serializa pelo tipo declarado e o objeto anônimo da "
            + "mensagem vira {} — o prompt nunca chegaria ao modelo");
        promptEnviado.Should().Contain("Emissão de notas ficou mais rápida.");
        promptEnviado.Should().Contain(
            "nova_funcionalidade",
            "o prompt versionado lista as categorias");
        promptEnviado.Should().NotContain("{{texto_fonte}}", "o placeholder foi interpolado");
    }
}
