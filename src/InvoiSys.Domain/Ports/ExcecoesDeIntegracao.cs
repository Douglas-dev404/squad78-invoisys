namespace InvoiSys.Domain.Ports;

public abstract class IntegracaoExternaException(string mensagem, Exception? causa = null)
    : Exception(mensagem, causa);

public sealed class JiraApiException(string mensagem) : IntegracaoExternaException(mensagem);

public sealed class LlmApiException(string mensagem) : IntegracaoExternaException(mensagem);

public sealed class LlmRespostaInvalidaException(string mensagem, Exception? causa = null)
    : IntegracaoExternaException(mensagem, causa);

public sealed class ProviderNaoConfiguradoException(string mensagem) : Exception(mensagem);
