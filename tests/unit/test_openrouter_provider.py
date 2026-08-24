"""Testa o adapter OpenRouterProvider contra um transport HTTP mockado — sem bater
em rede real nem depender de chave de API. Cobre: parsing de resposta JSON limpa,
resposta envolvida em code fence (comportamento real observado em alguns modelos),
regra de negócio de não perder chave no agrupamento, e retry em erro 5xx/429."""

import httpx
import pytest

from app.domain.enums import CategoriaAlteracao
from app.infrastructure.llm.openrouter_provider import (
    LLMRespostaInvalidaError,
    OpenRouterApiError,
    OpenRouterProvider,
)


def _client_com_respostas(respostas: list[httpx.Response]) -> httpx.AsyncClient:
    """Cria um AsyncClient cujo transport devolve as respostas na ordem dada —
    simula a sequência de chamadas HTTP sem rede real."""
    fila = list(respostas)

    def handler(request: httpx.Request) -> httpx.Response:
        return fila.pop(0)

    return httpx.AsyncClient(transport=httpx.MockTransport(handler))


def _resposta_content(texto: str, status_code: int = 200) -> httpx.Response:
    return httpx.Response(
        status_code,
        json={"choices": [{"message": {"content": texto}}]},
    )


async def test_categorizar_retorna_categoria_valida():
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas([_resposta_content("correcao")]),
    )
    categoria = await provider.categorizar("Corrigido bug no cálculo de ICMS.")
    assert categoria == CategoriaAlteracao.CORRECAO


async def test_categorizar_com_categoria_invalida_levanta_erro():
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas([_resposta_content("categoria-que-nao-existe")]),
    )
    with pytest.raises(LLMRespostaInvalidaError):
        await provider.categorizar("texto qualquer")


async def test_agrupar_semelhantes_parseia_json_limpo():
    resposta = '[["INV-1", "INV-2"], ["INV-3"]]'
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas([_resposta_content(resposta)]),
    )
    grupos = await provider.agrupar_semelhantes([("INV-1", "a"), ("INV-2", "b"), ("INV-3", "c")])
    assert grupos == [["INV-1", "INV-2"], ["INV-3"]]


async def test_agrupar_semelhantes_com_elemento_nao_string_levanta_erro():
    """Regressão: validação rasa (`isinstance(grupos, list)`) deixava passar um
    elemento não-string dentro de um grupo — ex: [["INV-1", 42]] — que só quebraria
    mais adiante, ao virar `origens` de um ItemComunicado."""
    resposta = '[["INV-1", 42]]'
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas([_resposta_content(resposta)]),
    )
    with pytest.raises(LLMRespostaInvalidaError):
        await provider.agrupar_semelhantes([("INV-1", "a")])


async def test_agrupar_semelhantes_parseia_json_com_code_fence():
    """Alguns modelos envolvem a resposta em ```json ... ``` mesmo com
    response_format pedido — o parser precisa lidar com isso."""
    resposta = '```json\n[["INV-1"], ["INV-2"]]\n```'
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas([_resposta_content(resposta)]),
    )
    grupos = await provider.agrupar_semelhantes([("INV-1", "a"), ("INV-2", "b")])
    assert grupos == [["INV-1"], ["INV-2"]]


async def test_agrupar_semelhantes_isola_chave_omitida_pelo_modelo():
    """Regra de negócio: nenhuma história pode desaparecer do comunicado por causa
    de um agrupamento incompleto do modelo."""
    resposta = '[["INV-1"]]'  # modelo "esqueceu" de INV-2
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas([_resposta_content(resposta)]),
    )
    grupos = await provider.agrupar_semelhantes([("INV-1", "a"), ("INV-2", "b")])
    todas_chaves = {chave for grupo in grupos for chave in grupo}
    assert todas_chaves == {"INV-1", "INV-2"}


async def test_reescrever_linguagem_negocio_retorna_texto_puro():
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas(
            [_resposta_content("Corrigimos um problema no cálculo de ICMS.")]
        ),
    )
    texto = await provider.reescrever_linguagem_negocio(
        ["Corrigido bug no ICMS."], CategoriaAlteracao.CORRECAO
    )
    assert texto == "Corrigimos um problema no cálculo de ICMS."


async def test_gerar_titulo_e_resumo_parseia_objeto_json():
    resposta = '{"titulo": "Release de Agosto", "resumo": "Melhorias diversas."}'
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas([_resposta_content(resposta)]),
    )
    titulo, resumo = await provider.gerar_titulo_e_resumo(["item 1", "item 2"])
    assert titulo == "Release de Agosto"
    assert resumo == "Melhorias diversas."


async def test_erro_5xx_faz_retry_e_eventualmente_sucede():
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas(
            [
                httpx.Response(503, json={"error": {"code": 503, "message": "indisponível"}}),
                _resposta_content("melhoria"),
            ]
        ),
    )
    categoria = await provider.categorizar("Melhoria de performance.")
    assert categoria == CategoriaAlteracao.MELHORIA


async def test_erro_4xx_nao_faz_retry_propaga_direto():
    provider = OpenRouterProvider(
        api_key="fake-invalida",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas(
            [httpx.Response(401, json={"error": {"code": 401, "message": "unauthorized"}})]
        ),
    )
    with pytest.raises(httpx.HTTPStatusError):
        await provider.categorizar("texto qualquer")


async def test_esgotar_retries_em_5xx_persistente_levanta_openrouter_api_error():
    provider = OpenRouterProvider(
        api_key="fake",
        modelo="openai/gpt-4o-mini",
        http_client=_client_com_respostas(
            [
                httpx.Response(429, json={"error": {"code": 429, "message": "rate limited"}}),
                httpx.Response(429, json={"error": {"code": 429, "message": "rate limited"}}),
                httpx.Response(429, json={"error": {"code": 429, "message": "rate limited"}}),
            ]
        ),
    )
    with pytest.raises(OpenRouterApiError):
        await provider.categorizar("texto qualquer")
