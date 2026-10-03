namespace InvoiSys.Api.Contracts;

/// <summary>
/// DTOs de entrada da revisão humana. <c>Publico</c> é obrigatório e vem no formato de
/// <c>ParaValor()</c> (<c>cliente</c>, <c>suporte</c>...): a revisão é por público, e
/// não existe default — aprovar o público errado em silêncio é pior que um 422.
///
/// Os campos são anuláveis porque é o que o binder JSON entrega quando o campo falta;
/// a obrigatoriedade é validada no domínio, não pela anotação do tipo.
///
/// <c>AprovadoPor</c>/<c>RevisadoPor</c> são texto livre enquanto não há autenticação —
/// quando houver, saem do corpo e passam a vir do usuário autenticado.
/// </summary>
public sealed record AprovarIn(string? Publico, string? AprovadoPor);

public sealed record ReprovarIn(string? Publico, string? Motivo, string? RevisadoPor);

public sealed record ReabrirIn(string? Publico);
