using System.Net;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Llm;
using InvoiSys.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Unit;

public class OpenAiProviderTests
{
    private const string ChaveDeTeste = "chave-de-teste";

    private sealed class LoggerColetor<T> : ILogger<T>
    {
        public List<string> Mensagens { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Mensagens.Add(formatter(state, exception));
    }

    private sealed class HandlerQueNuncaResponde : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static (OpenAiProvider Provider, FakeHttpMessageHandler Handler, LoggerColetor<OpenAiProvider> Log) Criar(
        Action<FakeHttpMessageHandler> configurarRespostas)
    {
        var handler = new FakeHttpMessageHandler();
        configurarRespostas(handler);
        var log = new LoggerColetor<OpenAiProvider>();

        var provider = new OpenAiProvider(
            new HttpClient(handler),
            Options.Create(new OpenAiOptions { ApiKey = ChaveDeTeste }),
            new PromptLoader(),
            log);

        return (provider, handler, log);
    }

    [Fact]
    public async Task Resposta_valida_e_convertida_no_enum()
    {
        var (provider, _, _) = Criar(h => h.ResponderConteudoLlm("nova_funcionalidade"));

        var categoria = await provider.CategorizarAsync("Passa a permitir emissão em lote.");

        categoria.Should().Be(CategoriaAlteracao.NovaFuncionalidade);
    }

    [Theory]
    [InlineData("`correcao`")]
    [InlineData("\"correcao\"")]
    [InlineData("'correcao'")]
    [InlineData(" `correcao` \n")]
    public async Task Categoria_entre_crases_ou_aspas_e_aceita_como_os_modelos_reais_respondem(string resposta)
    {
        var (provider, _, _) = Criar(h => h.ResponderConteudoLlm(resposta));

        var categoria = await provider.CategorizarAsync("Corrigido o cálculo do ICMS.");

        categoria.Should().Be(CategoriaAlteracao.Correcao);
    }

    [Theory]
    [InlineData("`financeiro`")]
    [InlineData("correção de bug")]
    public async Task Categoria_fora_do_enum_continua_recusada_mesmo_sem_o_invólucro(string resposta)
    {
        var (provider, _, _) = Criar(h => h.ResponderConteudoLlm(resposta));

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        await acao.Should().ThrowAsync<LlmRespostaInvalidaException>();
    }

    [Fact]
    public async Task Agrupamento_aceita_o_objeto_grupos_do_json_mode()
    {
        var (provider, _, _) = Criar(h => h.ResponderConteudoLlm(
            """{"grupos": [["INV-1", "INV-2"], ["INV-3"]]}"""));

        var grupos = await provider.AgruparSemelhantesAsync(
            [("INV-1", "a"), ("INV-2", "b"), ("INV-3", "c")]);

        grupos.Should().HaveCount(2);
    }

    [Fact]
    public async Task Titulo_e_resumo_extraem_os_dois_campos()
    {
        var (provider, _, _) = Criar(h => h.ResponderConteudoLlm(
            """{"titulo": "Release de agosto", "resumo": "Melhorias na emissão."}"""));

        var (titulo, resumo) = await provider.GerarTituloEResumoAsync(["item um"]);

        titulo.Should().Be("Release de agosto");
        resumo.Should().Be("Melhorias na emissão.");
    }

    [Fact]
    public async Task Reescrita_devolve_o_texto_sem_espacos_nas_pontas()
    {
        var (provider, _, _) = Criar(h => h.ResponderConteudoLlm("  Emissão mais rápida.\n"));

        var texto = await provider.ReescreverLinguagemNegocioAsync(["texto"], CategoriaAlteracao.Melhoria);

        texto.Should().Be("Emissão mais rápida.");
    }

    [Fact]
    public async Task Json_malformado_vira_resposta_invalida()
    {
        var (provider, _, _) = Criar(h => h.ResponderConteudoLlm("isto não é json"));

        var acao = async () => await provider.AgruparSemelhantesAsync([("INV-1", "a")]);

        await acao.Should().ThrowAsync<LlmRespostaInvalidaException>();
    }

    [Fact]
    public async Task Resposta_sem_choices_e_reportada_como_invalida_citando_a_OpenAI()
    {
        var (provider, _, _) = Criar(h => h.ResponderJson("""{"id": "abc"}"""));

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        (await acao.Should().ThrowAsync<LlmRespostaInvalidaException>())
            .Which.Message.Should().Contain("Resposta da OpenAI sem choices");
    }

    [Fact]
    public async Task Erro_429_vira_excecao_da_porta_sem_o_corpo_da_resposta()
    {
        var (provider, _, _) = Criar(h => h.ResponderJson(
            """{"error": {"message": "rate limited", "conta": "segredo"}}""",
            HttpStatusCode.TooManyRequests));

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        var excecao = (await acao.Should().ThrowAsync<LlmApiException>()).Which;
        excecao.Message.Should().Be("OpenAI retornou 429.");
        excecao.Message.Should().NotContain("segredo");
    }

    [Fact]
    public async Task Erro_401_vira_excecao_da_porta_e_nao_loga_o_corpo_nem_a_chave()
    {
        var (provider, _, log) = Criar(h => h.ResponderJson(
            """{"error": {"message": "Incorrect API key provided: sk-proj-TRECHO1234."}}""",
            HttpStatusCode.Unauthorized));

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        (await acao.Should().ThrowAsync<LlmApiException>())
            .Which.Message.Should().Be("OpenAI retornou 401.");
        log.Mensagens.Should().NotBeEmpty();
        log.Mensagens.Should().OnlyContain(m => !m.Contains("TRECHO1234") && !m.Contains(ChaveDeTeste));
    }

    [Fact]
    public async Task Erro_429_loga_o_corpo_para_diagnostico()
    {
        var (provider, _, log) = Criar(h => h.ResponderJson(
            """{"error": {"message": "rate limited"}}""",
            HttpStatusCode.TooManyRequests));

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        await acao.Should().ThrowAsync<LlmApiException>();
        log.Mensagens.Should().ContainSingle(m => m.Contains("OpenAI") && m.Contains("429") && m.Contains("rate limited"));
    }

    [Fact]
    public async Task Timeout_do_HttpClient_sobe_como_cancelamento_com_TimeoutException()
    {
        var provider = new OpenAiProvider(
            new HttpClient(new HandlerQueNuncaResponde()) { Timeout = TimeSpan.FromMilliseconds(50) },
            Options.Create(new OpenAiOptions { ApiKey = ChaveDeTeste }),
            new PromptLoader(),
            new LoggerColetor<OpenAiProvider>());

        var acao = async () => await provider.CategorizarAsync("qualquer coisa");

        (await acao.Should().ThrowAsync<TaskCanceledException>())
            .Which.InnerException.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public async Task Corpo_enviado_usa_o_modelo_da_OpenAI_json_mode_e_nao_leva_o_campo_models()
    {
        var (provider, handler, _) = Criar(h => h.ResponderConteudoLlm("""{"grupos": [["INV-1"]]}"""));

        await provider.AgruparSemelhantesAsync([("INV-1", "a")]);

        using var documento = JsonDocument.Parse(handler.CorposEnviados.Single());
        var raiz = documento.RootElement;
        raiz.GetProperty("model").GetString().Should().Be("gpt-4o-mini");
        raiz.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_object");
        raiz.TryGetProperty("models", out _).Should().BeFalse();
        handler.UrisChamadas.Single()!.ToString().Should().Be("https://api.openai.com/v1/chat/completions");
    }

    [Fact]
    public async Task Provider_pendente_da_OpenAI_manda_configurar_a_variavel_certa()
    {
        var acao = async () => await new ProviderPendente("OpenAi:ApiKey").CategorizarAsync("x");

        (await acao.Should().ThrowAsync<ProviderNaoConfiguradoException>())
            .Which.Message.Should().Contain("OpenAi__ApiKey");
    }

    [Fact]
    public async Task Provider_pendente_padrao_continua_apontando_para_a_OpenRouter()
    {
        var acao = async () => await new ProviderPendente().CategorizarAsync("x");

        (await acao.Should().ThrowAsync<ProviderNaoConfiguradoException>())
            .Which.Message.Should().Contain("OpenRouter__ApiKey");
    }
}
