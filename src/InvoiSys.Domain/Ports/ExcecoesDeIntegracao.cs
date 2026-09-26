namespace InvoiSys.Domain.Ports;

/// <summary>
/// Contrato de erro das portas <see cref="IJiraClient"/> e <see cref="ILlmProvider"/>.
///
/// Vive aqui, junto das portas, e não nos adapters: o erro faz parte do contrato da
/// porta tanto quanto o retorno. Se ficasse em InvoiSys.Infrastructure, a API (e
/// qualquer caso de uso) teria que importar o adapter concreto só para capturar a
/// falha — trocar de adapter quebraria o tratamento de erro em silêncio (virava 500).
///
/// Mensagens são seguras para chegar a quem chamou a API: sem corpo de resposta HTTP
/// externo, que vai só para o log do adapter.
/// </summary>
public abstract class IntegracaoExternaException(string mensagem, Exception? causa = null)
    : Exception(mensagem, causa);

/// <summary>
/// O Jira respondeu erro (credencial inválida, Release inexistente, indisponível),
/// depois de esgotado o retry do HttpClient.
/// </summary>
public sealed class JiraApiException(string mensagem) : IntegracaoExternaException(mensagem);

/// <summary>O provedor de LLM respondeu erro, depois de esgotado o retry.</summary>
public sealed class LlmApiException(string mensagem) : IntegracaoExternaException(mensagem);

/// <summary>
/// O modelo respondeu, mas o conteúdo não pôde ser interpretado no formato esperado
/// (JSON malformado, campo ausente, valor fora do enum).
/// </summary>
public sealed class LlmRespostaInvalidaException(string mensagem, Exception? causa = null)
    : IntegracaoExternaException(mensagem, causa);

/// <summary>
/// A integração (Jira ou LLM) não tem configuração para rodar — o composition root
/// entregou o adapter "pendente". Não é falha externa: é o gate honesto de ambiente sem
/// credencial, traduzido para HTTP 501.
/// </summary>
public sealed class ProviderNaoConfiguradoException(string mensagem) : Exception(mensagem);
