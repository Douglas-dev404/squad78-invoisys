using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Infrastructure.Jira;

public sealed class JiraRestClient(HttpClient http, ILogger<JiraRestClient> logger) : IJiraClient
{
    private const string TipoIssueReleaseNote = "Release Note";

    public async Task<IReadOnlyList<HistoriaJira>> BuscarHistoriasDaReleaseAsync(
        string fixVersion,
        CancellationToken cancellationToken = default)
    {
        var issues = await BuscarTodasIssuesAsync(fixVersion, cancellationToken);

        var historias = new List<HistoriaJira>(issues.Count);
        foreach (var issue in issues)
        {
            var textoReleaseNote = await BuscarTextoReleaseNoteAsync(issue, cancellationToken);
            historias.Add(ParaHistoriaJira(issue, textoReleaseNote));
        }

        return historias;
    }

    private async Task<List<JsonElement>> BuscarTodasIssuesAsync(
        string fixVersion,
        CancellationToken cancellationToken)
    {
        var jql = $"fixVersion = \"{fixVersion}\"";
        var issues = new List<JsonElement>();
        string? nextPageToken = null;

        while (true)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["jql"] = jql;
            query["fields"] = "summary,description,issuetype,labels,subtasks";
            query["maxResults"] = "100";
            if (nextPageToken is not null)
            {
                query["nextPageToken"] = nextPageToken;
            }

            var payload = await GetAsync($"/rest/api/3/search/jql?{query}", cancellationToken);

            if (payload.TryGetProperty("issues", out var lista)
                && lista.ValueKind == JsonValueKind.Array)
            {
                issues.AddRange(lista.EnumerateArray());
            }

            nextPageToken = payload.TryGetProperty("nextPageToken", out var token)
                && token.ValueKind == JsonValueKind.String
                    ? token.GetString()
                    : null;

            var ehUltima = !payload.TryGetProperty("isLast", out var isLast)
                || isLast.ValueKind != JsonValueKind.False;

            if (string.IsNullOrEmpty(nextPageToken) || ehUltima)
            {
                break;
            }
        }

        return issues;
    }

    private async Task<string?> BuscarTextoReleaseNoteAsync(
        JsonElement issue,
        CancellationToken cancellationToken)
    {
        if (!issue.TryGetProperty("fields", out var fields)
            || !fields.TryGetProperty("subtasks", out var subtasks)
            || subtasks.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var subtask in subtasks.EnumerateArray())
        {
            var tipo = subtask.TryGetProperty("fields", out var subFields)
                && subFields.TryGetProperty("issuetype", out var issuetype)
                && issuetype.TryGetProperty("name", out var nome)
                    ? nome.GetString()
                    : null;

            if (tipo != TipoIssueReleaseNote)
            {
                continue;
            }

            var chave = subtask.GetProperty("key").GetString();
            var detalhe = await GetAsync(
                $"/rest/api/3/issue/{chave}?fields=description",
                cancellationToken);

            return detalhe.TryGetProperty("fields", out var detalheFields)
                ? ExtrairTextoDescription(detalheFields)
                : null;
        }

        return null;
    }

    private static HistoriaJira ParaHistoriaJira(JsonElement issue, string? textoReleaseNote)
    {
        var fields = issue.TryGetProperty("fields", out var f) ? f : default;

        return new HistoriaJira
        {
            Chave = issue.GetProperty("key").GetString() ?? string.Empty,
            Titulo = LerString(fields, "summary"),
            DescricaoTecnica = ExtrairTextoDescription(fields),
            TipoIssue = fields.ValueKind == JsonValueKind.Object
                && fields.TryGetProperty("issuetype", out var tipo)
                    ? LerString(tipo, "name")
                    : string.Empty,
            TextoReleaseNote = textoReleaseNote,
            Labels = fields.ValueKind == JsonValueKind.Object
                && fields.TryGetProperty("labels", out var labels)
                && labels.ValueKind == JsonValueKind.Array
                    ? [.. labels.EnumerateArray()
                        .Select(l => l.GetString())
                        .Where(l => l is not null)
                        .Select(l => l!)]
                    : [],
        };
    }

    internal static string ExtrairTextoDescription(JsonElement fields)
    {
        if (fields.ValueKind != JsonValueKind.Object
            || !fields.TryGetProperty("description", out var description))
        {
            return string.Empty;
        }

        return description.ValueKind switch
        {
            JsonValueKind.String => description.GetString() ?? string.Empty,
            JsonValueKind.Object => ExtrairTextoAdf(description),
            _ => string.Empty,
        };
    }

    internal static string ExtrairTextoAdf(JsonElement node)
    {
        var partes = new List<string>();
        ColetarTextoAdf(node, partes);
        return string.Join(' ', partes.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
    }

    private static void ColetarTextoAdf(JsonElement node, List<string> partes)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (node.TryGetProperty("type", out var tipo)
            && tipo.ValueKind == JsonValueKind.String
            && tipo.GetString() == "text"
            && node.TryGetProperty("text", out var texto))
        {
            partes.Add(texto.GetString() ?? string.Empty);
        }

        if (node.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var filho in content.EnumerateArray())
            {
                ColetarTextoAdf(filho, partes);
            }
        }
    }

    private static string LerString(JsonElement elemento, string propriedade) =>
        elemento.ValueKind == JsonValueKind.Object
        && elemento.TryGetProperty(propriedade, out var valor)
        && valor.ValueKind == JsonValueKind.String
            ? valor.GetString() ?? string.Empty
            : string.Empty;

    private async Task<JsonElement> GetAsync(string caminho, CancellationToken cancellationToken)
    {
        using var resposta = await http.GetAsync(caminho, cancellationToken);

        if (!resposta.IsSuccessStatusCode)
        {
            var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
            corpo = corpo.Length > 500 ? corpo[..500] : corpo;

            logger.LogError(
                "Jira respondeu erro {Status} em {Caminho}: {Corpo}",
                (int)resposta.StatusCode,
                caminho,
                corpo);

            // Corpo da resposta só no log: esta mensagem chega a quem chamou a API.
            throw new JiraApiException(
                $"Jira retornou {(int)resposta.StatusCode} em {caminho}.");
        }

        return await resposta.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }
}
