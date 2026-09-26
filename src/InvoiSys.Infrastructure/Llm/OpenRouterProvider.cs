using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure.Llm;

/// <summary>
/// Adapter concreto de <see cref="ILlmProvider"/> contra a API da OpenRouter.
///
/// Contrato verificado em 2026-08-24 contra a documentação oficial:
/// - Endpoint único: POST https://openrouter.ai/api/v1/chat/completions
/// - Schema de request/response compatível com OpenAI Chat Completions — não
///   precisamos de SDK dedicado, HttpClient direto resolve.
/// - Saída estruturada via response_format: {"type": "json_object"}.
/// - Modelo especificado como "provedor/modelo" (ex: "openai/gpt-4o-mini").
/// - Fallback nativo entre modelos: parâmetro models (lista) — se o primeiro falhar
///   com 5xx/429, a OpenRouter tenta o próximo automaticamente, sem round-trip nosso.
/// - Erro 429 vem com header Retry-After e corpo {"error": {"code","message","type"}}.
///
/// Fonte: openrouter.ai/docs/api_reference/overview.
///
/// Cada método carrega o prompt correspondente de prompts/*.md (via PromptLoader),
/// interpola os placeholders, e faz uma chamada ao modelo. Parsing de JSON da resposta
/// é defensivo — LLM pode devolver JSON malformado mesmo com response_format pedido.
/// </summary>
public sealed partial class OpenRouterProvider(
    HttpClient http,
    IOptions<OpenRouterOptions> options,
    PromptLoader prompts,
    ILogger<OpenRouterProvider> logger) : ILlmProvider
{
    private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web);

    private readonly OpenRouterOptions _opcoes = options.Value;

    public async Task<CategoriaAlteracao> CategorizarAsync(
        string textoFonte,
        CancellationToken cancellationToken = default)
    {
        var prompt = prompts.Montar("02_categorizar.md", ("texto_fonte", textoFonte));
        var resposta = await ChamarAsync(prompt, jsonMode: false, cancellationToken);

        // Trim('"'): esse estágio não usa jsonMode (a saída é só a palavra da
        // categoria, não um objeto), mas o prompt pede a resposta "apenas com o
        // valor" — alguns modelos ecoam aspas ao redor mesmo fora de JSON mode.
        var valor = resposta.Trim().Trim('"');

        if (!CategoriaAlteracaoExtensions.TentarConverter(valor, out var categoria))
        {
            throw new LlmRespostaInvalidaException(
                $"Categoria '{valor}' fora do enum esperado: {resposta}");
        }

        return categoria;
    }

    public async Task<IReadOnlyList<IReadOnlyList<string>>> AgruparSemelhantesAsync(
        IReadOnlyList<(string Chave, string Texto)> textos,
        CancellationToken cancellationToken = default)
    {
        var listaChaveTexto = JsonSerializer.Serialize(
            textos.Select(t => new[] { t.Chave, t.Texto }),
            JsonOpcoes);

        var prompt = prompts.Montar(
            "03_agrupar_semelhantes.md",
            ("lista_chave_texto", listaChaveTexto));

        var resposta = await ChamarAsync(prompt, jsonMode: true, cancellationToken);
        using var documento = ParseJson(resposta, contexto: "agrupar_semelhantes");
        var grupos = ValidarGrupos(documento.RootElement);

        var chavesEntrada = textos.Select(t => t.Chave).ToHashSet();
        var chavesSaida = grupos.SelectMany(g => g).ToHashSet();

        if (!chavesEntrada.SetEquals(chavesSaida))
        {
            // Regra de negócio: cada chave de entrada aparece em exatamente um grupo.
            // Se o modelo "perdeu" alguma, isolamos ela como grupo próprio em vez de
            // descartar silenciosamente — perder uma história do comunicado é pior que
            // ela aparecer sem agrupamento.
            var faltando = chavesEntrada.Except(chavesSaida).ToList();
            if (faltando.Count > 0)
            {
                logger.LogWarning(
                    "LLM omitiu {Quantidade} chave(s) no agrupamento — isolando como "
                    + "grupo próprio: {Chaves}",
                    faltando.Count,
                    string.Join(", ", faltando));

                grupos.AddRange(faltando.Select(chave => (IReadOnlyList<string>)new[] { chave }));
            }
        }

        return grupos;
    }

    public async Task<string> ReescreverLinguagemNegocioAsync(
        IReadOnlyList<string> textosFonte,
        CategoriaAlteracao categoria,
        CancellationToken cancellationToken = default)
    {
        var prompt = prompts.Montar(
            "04_reescrever_linguagem_negocio.md",
            ("categoria", categoria.ParaValor()),
            ("textos_fonte", string.Join("\n", textosFonte.Select(t => $"- {t}"))));

        var resposta = await ChamarAsync(prompt, jsonMode: false, cancellationToken);
        return resposta.Trim();
    }

    public async Task<(string Titulo, string Resumo)> GerarTituloEResumoAsync(
        IReadOnlyList<string> itensTexto,
        CancellationToken cancellationToken = default)
    {
        var prompt = prompts.Montar(
            "05_gerar_titulo_resumo.md",
            ("itens_texto", string.Join("\n", itensTexto.Select(t => $"- {t}"))));

        var resposta = await ChamarAsync(prompt, jsonMode: true, cancellationToken);
        using var documento = ParseJson(resposta, contexto: "gerar_titulo_e_resumo");
        var raiz = documento.RootElement;

        if (raiz.ValueKind != JsonValueKind.Object
            || !raiz.TryGetProperty("titulo", out var titulo)
            || !raiz.TryGetProperty("resumo", out var resumo)
            || titulo.ValueKind != JsonValueKind.String
            || resumo.ValueKind != JsonValueKind.String)
        {
            throw new LlmRespostaInvalidaException(
                $"Esperava objeto com 'titulo' e 'resumo', recebeu: {resposta}");
        }

        return (titulo.GetString()!, resumo.GetString()!);
    }

    /// <summary>
    /// Valida a forma completa da resposta, não só o nível externo — um LLM pode
    /// devolver [["INV-1", 42]] (elemento não-string no meio) e passar por uma
    /// checagem rasa de "é array?" sem que ninguém perceba até Origens de um
    /// ItemComunicado conter lixo.
    /// </summary>
    private static List<IReadOnlyList<string>> ValidarGrupos(JsonElement raiz)
    {
        if (raiz.ValueKind != JsonValueKind.Array)
        {
            throw new LlmRespostaInvalidaException(
                $"Esperava lista de listas de strings (grupos de chaves), recebeu: {raiz}");
        }

        var grupos = new List<IReadOnlyList<string>>();
        foreach (var grupo in raiz.EnumerateArray())
        {
            if (grupo.ValueKind != JsonValueKind.Array)
            {
                throw new LlmRespostaInvalidaException(
                    $"Esperava lista de listas de strings (grupos de chaves), recebeu: {raiz}");
            }

            var chaves = new List<string>();
            foreach (var chave in grupo.EnumerateArray())
            {
                if (chave.ValueKind != JsonValueKind.String)
                {
                    throw new LlmRespostaInvalidaException(
                        $"Esperava lista de listas de strings (grupos de chaves), recebeu: {raiz}");
                }

                chaves.Add(chave.GetString()!);
            }

            grupos.Add(chaves);
        }

        return grupos;
    }

    /// <summary>
    /// Parsing defensivo: modelo pode envolver o JSON em ```json ... ``` mesmo com
    /// response_format pedido — alguns modelos atrás do gateway ignoram o parâmetro.
    /// Extrai o bloco antes de tentar parsear puro.
    /// </summary>
    private static JsonDocument ParseJson(string resposta, string contexto)
    {
        var texto = resposta.Trim();

        var blocoCodeFence = RegexCodeFence().Match(texto);
        if (blocoCodeFence.Success)
        {
            texto = blocoCodeFence.Groups[1].Value.Trim();
        }

        try
        {
            return JsonDocument.Parse(texto);
        }
        catch (JsonException exc)
        {
            throw new LlmRespostaInvalidaException(
                $"Resposta do modelo em '{contexto}' não é JSON válido: {resposta}",
                exc);
        }
    }

    private async Task<string> ChamarAsync(
        string prompt,
        bool jsonMode,
        CancellationToken cancellationToken)
    {
        // Tipo concreto, não Dictionary<string, object>: o System.Text.Json serializa
        // pelo tipo DECLARADO, então um objeto guardado como `object` sai como {} —
        // o prompt iria vazio para o modelo, sem erro nenhum. Propriedades nulas são
        // omitidas (JsonIgnore abaixo), que é como os campos opcionais somem do corpo.
        var payload = new ChatCompletionRequest
        {
            Model = _opcoes.Modelo,
            Messages = [new ChatMessage { Role = "user", Content = prompt }],
            // Temperatura baixa — queremos consistência, não criatividade, no pipeline.
            Temperature = 0.2,
            Models = _opcoes.ModelosFallback.Count > 0
                ? [_opcoes.Modelo, .. _opcoes.ModelosFallback]
                : null,
            ResponseFormat = jsonMode ? new ResponseFormat { Type = "json_object" } : null,
        };

        var data = await PostAsync(payload, cancellationToken);

        if (!data.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0
            || !choices[0].TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.String)
        {
            throw new LlmRespostaInvalidaException(
                $"Resposta da OpenRouter sem choices[0].message.content: {data}");
        }

        return content.GetString()!;
    }

    private async Task<JsonElement> PostAsync(
        ChatCompletionRequest payload,
        CancellationToken cancellationToken)
    {
        using var resposta = await http.PostAsJsonAsync(
            _opcoes.Endpoint,
            payload,
            JsonOpcoes,
            cancellationToken);

        if (!resposta.IsSuccessStatusCode)
        {
            var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
            corpo = corpo.Length > 500 ? corpo[..500] : corpo;
            var status = (int)resposta.StatusCode;

            logger.LogError("OpenRouter respondeu erro {Status}: {Corpo}", status, corpo);

            // Erro transitório — a policy de resiliência do HttpClient já re-tentou
            // antes de chegar aqui; envelopamos para o chamador não depender de
            // HttpRequestException crua. Corpo só no log: esta mensagem chega a quem chamou.
            throw new LlmApiException($"OpenRouter retornou {status}.");
        }

        return await resposta.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    [GeneratedRegex(@"```(?:json)?\s*(.*?)```", RegexOptions.Singleline)]
    private static partial Regex RegexCodeFence();
}
