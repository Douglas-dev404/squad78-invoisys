"""Testa a extração de texto do formato ADF (Atlassian Document Format) usado no
campo `description` da API v3. Não testa contra Jira real (sem instância disponível
ainda), mas contra a estrutura documentada do ADF."""

from app.infrastructure.jira.jira_rest_client import JiraRestClient


def test_extrai_texto_simples_de_paragrafo_adf():
    description = {
        "type": "doc",
        "version": 1,
        "content": [
            {
                "type": "paragraph",
                "content": [{"type": "text", "text": "Corrigido cálculo de ICMS."}],
            }
        ],
    }
    assert (
        JiraRestClient._extrair_texto_description({"description": description})
        == "Corrigido cálculo de ICMS."
    )


def test_extrai_texto_de_multiplos_paragrafos():
    description = {
        "type": "doc",
        "content": [
            {"type": "paragraph", "content": [{"type": "text", "text": "Primeira linha."}]},
            {"type": "paragraph", "content": [{"type": "text", "text": "Segunda linha."}]},
        ],
    }
    texto = JiraRestClient._extrair_texto_description({"description": description})
    assert "Primeira linha." in texto
    assert "Segunda linha." in texto


def test_description_ausente_retorna_string_vazia():
    assert JiraRestClient._extrair_texto_description({}) == ""
    assert JiraRestClient._extrair_texto_description({"description": None}) == ""


def test_description_como_string_direta_e_aceita():
    # Alguma config/instância antiga pode devolver texto puro em vez de ADF.
    assert JiraRestClient._extrair_texto_description({"description": "texto puro"}) == "texto puro"
