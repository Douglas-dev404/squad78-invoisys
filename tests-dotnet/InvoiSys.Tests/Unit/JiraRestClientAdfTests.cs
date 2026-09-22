using System.Text.Json;
using FluentAssertions;
using InvoiSys.Infrastructure.Jira;

namespace InvoiSys.Tests.Unit;

/// <summary>
/// O campo description da Jira API v3 vem em Atlassian Document Format, não texto puro.
/// Se essa extração falhar, o pipeline de IA recebe string vazia e o comunicado sai oco
/// — sem erro nenhum. Por isso ela é testada isoladamente.
/// </summary>
public class JiraRestClientAdfTests
{
    private static JsonElement Json(string texto) => JsonDocument.Parse(texto).RootElement;

    [Fact]
    public void Extrai_texto_de_documento_ADF_aninhado()
    {
        var fields = Json("""
        {
          "description": {
            "type": "doc",
            "version": 1,
            "content": [
              {
                "type": "paragraph",
                "content": [
                  { "type": "text", "text": "Corrigido o cálculo" },
                  { "type": "text", "text": "do imposto retido." }
                ]
              }
            ]
          }
        }
        """);

        JiraRestClient.ExtrairTextoDescription(fields)
            .Should().Be("Corrigido o cálculo do imposto retido.");
    }

    [Fact]
    public void Aceita_description_em_string_pura()
    {
        // Instância antiga ou config diferente pode devolver string direto.
        var fields = Json("""{ "description": "Texto simples" }""");

        JiraRestClient.ExtrairTextoDescription(fields).Should().Be("Texto simples");
    }

    [Fact]
    public void Description_ausente_ou_nula_vira_string_vazia()
    {
        JiraRestClient.ExtrairTextoDescription(Json("{}")).Should().BeEmpty();
        JiraRestClient.ExtrairTextoDescription(Json("""{ "description": null }"""))
            .Should().BeEmpty();
    }

    [Fact]
    public void Ignora_nos_sem_texto_como_imagens_e_regras()
    {
        var fields = Json("""
        {
          "description": {
            "type": "doc",
            "content": [
              { "type": "rule" },
              {
                "type": "paragraph",
                "content": [{ "type": "text", "text": "Sobrou só isto." }]
              },
              { "type": "mediaSingle", "content": [{ "type": "media", "attrs": {} }] }
            ]
          }
        }
        """);

        JiraRestClient.ExtrairTextoDescription(fields).Should().Be("Sobrou só isto.");
    }
}
