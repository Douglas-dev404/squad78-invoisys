using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure.Llm;

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

    private static List<IReadOnlyList<string>> ValidarGrupos(JsonElement raiz)
    {
        // JSON mode exige objeto na raiz: o prompt pede {"grupos": [...]}; lista crua segue aceita.
        if (raiz.ValueKind == JsonValueKind.Object && raiz.TryGetProperty("grupos", out var listaDeGrupos))
        {
            raiz = listaDeGrupos;
        }

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
        // Tipo concreto, não Dictionary<string, object>: o System.Text.Json serializaria o prompt como {} em silêncio.
        var payload = new ChatCompletionRequest
        {
            Model = _opcoes.Modelo,
            Messages = [new ChatMessage { Role = "user", Content = prompt }],
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

            // Corpo da resposta só no log: esta mensagem chega a quem chamou a API.
            throw new LlmApiException($"OpenRouter retornou {status}.");
        }

        return await resposta.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    [GeneratedRegex(@"```(?:json)?\s*(.*?)```", RegexOptions.Singleline)]
    private static partial Regex RegexCodeFence();
}
