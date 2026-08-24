"""Testa o comportamento de retry e erro do JiraRestClient contra um transport HTTP
mockado. Cobre a distinção crítica entre erro transitório (429/5xx — retentável) e
erro do cliente (401/404 — propaga na primeira tentativa, retry não ajudaria)."""

import httpx
import pytest

from app.infrastructure.jira.jira_rest_client import JiraApiError, JiraRestClient


def _client_com_respostas(respostas: list[httpx.Response]) -> httpx.AsyncClient:
    fila = list(respostas)

    def handler(request: httpx.Request) -> httpx.Response:
        return fila.pop(0)

    return httpx.AsyncClient(
        base_url="https://fake.atlassian.net", transport=httpx.MockTransport(handler)
    )


def _resposta_busca_vazia() -> httpx.Response:
    return httpx.Response(200, json={"issues": [], "isLast": True})


async def test_erro_503_faz_retry_e_eventualmente_sucede():
    """503 é transitório (>= 500) — o client deve tentar de novo e suceder na
    segunda tentativa, sem propagar o erro pro chamador."""
    client = JiraRestClient(
        base_url="https://fake.atlassian.net",
        email="a@b.com",
        api_token="fake",
        http_client=_client_com_respostas(
            [
                httpx.Response(503, json={"errorMessages": ["indisponível"]}),
                _resposta_busca_vazia(),
            ]
        ),
    )
    historias = await client.buscar_historias_da_release("RELEASE-1")
    assert historias == []


async def test_erro_401_nao_faz_retry_propaga_na_primeira_tentativa():
    """401 (token inválido) nunca deve ser retentado — uma tentativa a mais não
    resolveria um token errado. Antes da correção, isso disparava 3 tentativas
    inúteis (~1+2+4s de espera) antes de propagar o erro."""
    client = JiraRestClient(
        base_url="https://fake.atlassian.net",
        email="a@b.com",
        api_token="token-invalido",
        http_client=_client_com_respostas(
            [httpx.Response(401, json={"errorMessages": ["Unauthorized"]})]
        ),
    )
    with pytest.raises(JiraApiError) as exc_info:
        await client.buscar_historias_da_release("RELEASE-1")
    assert exc_info.value.status_code == 401
    assert not exc_info.value.retentavel


async def test_erro_429_e_retentavel():
    erro_429 = JiraApiError("rate limited", status_code=429)
    assert erro_429.retentavel is True


async def test_erro_404_nao_e_retentavel():
    erro_404 = JiraApiError("not found", status_code=404)
    assert erro_404.retentavel is False


async def test_esgotar_retries_em_503_persistente_propaga_jira_api_error():
    client = JiraRestClient(
        base_url="https://fake.atlassian.net",
        email="a@b.com",
        api_token="fake",
        http_client=_client_com_respostas(
            [
                httpx.Response(503, json={"errorMessages": ["indisponível"]}),
                httpx.Response(503, json={"errorMessages": ["indisponível"]}),
                httpx.Response(503, json={"errorMessages": ["indisponível"]}),
            ]
        ),
    )
    with pytest.raises(JiraApiError) as exc_info:
        await client.buscar_historias_da_release("RELEASE-1")
    assert exc_info.value.status_code == 503
