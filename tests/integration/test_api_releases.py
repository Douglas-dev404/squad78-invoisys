"""Teste de integração: sobe a aplicação FastAPI real (via TestClient) e bate nos
endpoints HTTP ponta a ponta — diferente dos testes unitários, aqui validamos o
wiring completo (DI, rotas, serialização Pydantic), não só a lógica isolada.

Jira é substituído por fake via `dependency_overrides` (não batemos em rede real
nem dependemos de credencial); o LLM não é sobrescrito de propósito no primeiro
teste — isso prova que o gate 501 do ProviderPendente funciona ponta a ponta,
comportamento real do sistema hoje, não uma simulação."""

import pytest
from fastapi.testclient import TestClient

from app.core.dependencies import get_jira_client, get_llm_provider
from app.domain.entities import HistoriaJira
from app.domain.enums import CategoriaAlteracao
from app.main import app


class JiraClientFakeIntegracao:
    async def buscar_historias_da_release(self, fix_version: str) -> list[HistoriaJira]:
        return [
            HistoriaJira(
                chave="INV-1",
                titulo="Corrige cálculo de ICMS",
                descricao_tecnica="Corrigido cálculo de ICMS interestadual.",
                tipo_issue="Bug",
                texto_release_note="Corrigimos um problema no cálculo de ICMS.",
            ),
            HistoriaJira(
                chave="INV-2",
                titulo="Nova tela de dashboard",
                descricao_tecnica="Adicionado gráfico de notas emitidas por mês.",
                tipo_issue="Story",
            ),
        ]


class LLMProviderFakeIntegracao:
    async def categorizar(self, texto_fonte: str) -> CategoriaAlteracao:
        return (
            CategoriaAlteracao.CORRECAO
            if "corrig" in texto_fonte.lower()
            else CategoriaAlteracao.NOVA_FUNCIONALIDADE
        )

    async def agrupar_semelhantes(self, textos):
        return [[chave] for chave, _texto in textos]

    async def reescrever_linguagem_negocio(self, textos_fonte, categoria):
        return f"[{categoria.value}] {textos_fonte[0]}"

    async def gerar_titulo_e_resumo(self, itens_texto):
        return "Release de Integração", f"{len(itens_texto)} novidades nesta versão."


@pytest.fixture
def client_sem_llm():
    app.dependency_overrides[get_jira_client] = lambda: JiraClientFakeIntegracao()
    client = TestClient(app)
    yield client
    app.dependency_overrides.clear()


@pytest.fixture
def client_com_llm():
    app.dependency_overrides[get_jira_client] = lambda: JiraClientFakeIntegracao()
    app.dependency_overrides[get_llm_provider] = lambda: LLMProviderFakeIntegracao()
    client = TestClient(app)
    yield client
    app.dependency_overrides.clear()


def test_health_check_responde_ok():
    with TestClient(app) as client:
        response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_buscar_historias_retorna_dados_do_jira(client_sem_llm):
    response = client_sem_llm.get("/api/v1/releases/INV-REL-1/historias")

    assert response.status_code == 200
    body = response.json()
    assert body["chave_jira"] == "INV-REL-1"
    assert body["total_historias"] == 2
    assert body["historias"][0]["chave"] == "INV-1"
    assert body["historias"][0]["possui_release_note_dedicada"] is True
    assert body["historias"][1]["possui_release_note_dedicada"] is False


def test_processar_release_sem_llm_configurado_retorna_501(client_sem_llm):
    """Prova ponta a ponta que o sistema falha honestamente quando o LLM provider
    ainda não foi decidido — não mente com resultado fake, não quebra com 500 cru."""
    response = client_sem_llm.post("/api/v1/releases/INV-REL-1/processar")

    assert response.status_code == 501
    assert "provider" in response.json()["detail"].lower()


def test_processar_release_com_llm_fake_roda_pipeline_completo(client_com_llm):
    response = client_com_llm.post("/api/v1/releases/INV-REL-1/processar")

    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "aguardando_revisao"
    assert body["titulo_executivo"] == "Release de Integração"
    assert len(body["itens"]) == 2
    categorias = {item["categoria"] for item in body["itens"]}
    assert categorias == {"correcao", "nova_funcionalidade"}
