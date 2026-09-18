namespace InvoiSys.Infrastructure.Llm;

/// <summary>
/// O modelo respondeu, mas o conteúdo não pôde ser interpretado no formato esperado
/// (JSON malformado, campo ausente, valor fora do enum).
/// </summary>
public sealed class LlmRespostaInvalidaException(string mensagem) : Exception(mensagem);

/// <summary>
/// Erro de comunicação com a API da OpenRouter, após esgotar as tentativas.
/// </summary>
public sealed class OpenRouterApiException(string mensagem) : Exception(mensagem);

/// <summary>
/// Levantado quando o pipeline tenta chamar o LLM sem um provider real configurado.
/// Traduzido para HTTP 501 na camada de API — o gate honesto, em vez de instanciar um
/// adapter fadado a falhar em runtime.
/// </summary>
public sealed class ProviderNaoConfiguradoException(string mensagem) : Exception(mensagem);
