namespace InvoiSys.Api.Contracts;

public sealed record SalvarJiraIn(string? BaseUrl, string? Email, string? Projeto, string? ApiToken);

public sealed record SalvarIaIn(string? Provedor, string? Modelo, string? ApiKey);

public sealed record IntegracaoJiraOut(
    string BaseUrl, string Email, string Projeto, bool TokenConfigurado, bool TokenDoSistema, bool Configurado);

public sealed record IntegracaoProvedorIaOut(string Modelo, bool ChaveConfigurada, bool ChaveDoSistema);

public sealed record IntegracaoIaOut(
    string Provedor, IntegracaoProvedorIaOut OpenAi, IntegracaoProvedorIaOut OpenRouter, bool Configurado);

public sealed record IntegracoesOut(IntegracaoJiraOut Jira, IntegracaoIaOut Ia);

public sealed record ResultadoTesteOut(bool Ok, string Mensagem, long DuracaoMs);

public sealed record EventoExecucaoOut(
    long Id,
    DateTimeOffset Em,
    string? Release,
    string Tipo,
    string Mensagem,
    string? Prompt,
    string? Resposta,
    string? Modelo,
    long? DuracaoMs);

public sealed record ExecucoesAoVivoOut(long UltimoId, IReadOnlyList<EventoExecucaoOut> Eventos);
