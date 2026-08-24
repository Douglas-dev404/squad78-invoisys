"""Adapter concreto de LLMProvider contra a API da OpenRouter.

Contrato verificado em 2026-08-24 contra a documentação oficial:
- Endpoint único: POST https://openrouter.ai/api/v1/chat/completions
- Schema de request/response compatível com OpenAI Chat Completions — não precisamos
  de SDK dedicado, `httpx` direto resolve (decisão que reforça a ressalva já registrada
  sobre LangChain ser peso morto pra esse escopo).
- Saída estruturada via `response_format: {"type": "json_object"}`.
- Modelo especificado como "provedor/modelo" (ex: "openai/gpt-4o-mini").
- Fallback nativo entre modelos: parâmetro `models` (lista) — se o primeiro falhar
  com 5xx/429, a OpenRouter tenta o próximo automaticamente, sem round-trip nosso.
- Erro 429 vem com header `Retry-After` e corpo `{"error": {"code", "message", "type"}}`.

Fonte: openrouter.ai/docs/api_reference/overview.

Cada método carrega o prompt correspondente de `prompts/*.md` (via prompt_loader),
interpola os placeholders, e faz uma chamada ao modelo. Parsing de JSON da resposta é
defensivo — LLM pode devolver JSON malformado mesmo com response_format pedido.
"""

from __future__ import annotations

import json
import re

import httpx
from loguru import logger
from tenacity import retry, retry_if_exception_type, stop_after_attempt, wait_exponential

from app.domain.enums import CategoriaAlteracao
from app.infrastructure.llm.prompt_loader import montar_prompt

_ENDPOINT = "https://openrouter.ai/api/v1/chat/completions"


class LLMRespostaInvalidaError(Exception):
    """O modelo respondeu, mas o conteúdo não pôde ser interpretado no formato
    esperado (JSON malformado, campo ausente, valor fora do enum)."""


class OpenRouterApiError(Exception):
    """Erro de comunicação com a API da OpenRouter, após esgotar as tentativas."""


class OpenRouterProvider:
    """Implementa a porta `app.domain.ports.LLMProvider`."""

    def __init__(
        self,
        api_key: str,
        modelo: str,
        modelos_fallback: list[str] | None = None,
        http_client: httpx.AsyncClient | None = None,
    ) -> None:
        self._modelo = modelo
        self._modelos_fallback = modelos_fallback or []
        self._http = http_client or httpx.AsyncClient(
            headers={
                "Authorization": f"Bearer {api_key}",
                "Content-Type": "application/json",
            },
            timeout=httpx.Timeout(60.0, connect=5.0),
        )

    async def categorizar(self, texto_fonte: str) -> CategoriaAlteracao:
        prompt = montar_prompt("02_categorizar.md", texto_fonte=texto_fonte)
        resposta = await self._chamar(prompt, json_mode=False)
        # .strip('"'): esse estágio não usa json_mode (a saída é só a palavra da
        # categoria, não um objeto), mas o prompt pede a resposta "apenas com o
        # valor" — alguns modelos ecoam aspas ao redor mesmo fora de JSON mode.
        valor = resposta.strip().strip('"').lower()
        try:
            return CategoriaAlteracao(valor)
        except ValueError as exc:
            raise LLMRespostaInvalidaError(
                f"Categoria '{valor}' fora do enum esperado: {resposta!r}"
            ) from exc

    async def agrupar_semelhantes(self, textos: list[tuple[str, str]]) -> list[list[str]]:
        lista_chave_texto = json.dumps(
            [[chave, texto] for chave, texto in textos], ensure_ascii=False
        )
        prompt = montar_prompt("03_agrupar_semelhantes.md", lista_chave_texto=lista_chave_texto)
        resposta = await self._chamar(prompt, json_mode=True)
        bruto = self._parse_json(resposta, contexto="agrupar_semelhantes")
        grupos = self._validar_grupos(bruto)

        chaves_entrada = {chave for chave, _ in textos}
        chaves_saida = {chave for grupo in grupos for chave in grupo}
        if chaves_entrada != chaves_saida:
            # Regra de negócio: cada chave de entrada aparece em exatamente um grupo.
            # Se o modelo "perdeu" alguma, isolamos ela como grupo próprio em vez de
            # descartar silenciosamente — perder uma história do comunicado é pior
            # que ela aparecer sem agrupamento.
            faltando = chaves_entrada - chaves_saida
            logger.warning(
                "LLM omitiu {} chave(s) no agrupamento — isolando como grupo próprio: {}",
                len(faltando),
                faltando,
            )
            grupos.extend([[chave] for chave in faltando])

        return grupos

    async def reescrever_linguagem_negocio(
        self, textos_fonte: list[str], categoria: CategoriaAlteracao
    ) -> str:
        prompt = montar_prompt(
            "04_reescrever_linguagem_negocio.md",
            categoria=categoria.value,
            textos_fonte="\n".join(f"- {t}" for t in textos_fonte),
        )
        resposta = await self._chamar(prompt, json_mode=False)
        return resposta.strip()

    async def gerar_titulo_e_resumo(self, itens_texto: list[str]) -> tuple[str, str]:
        prompt = montar_prompt(
            "05_gerar_titulo_resumo.md",
            itens_texto="\n".join(f"- {t}" for t in itens_texto),
        )
        resposta = await self._chamar(prompt, json_mode=True)
        dados = self._parse_json(resposta, contexto="gerar_titulo_e_resumo")

        if not isinstance(dados, dict) or "titulo" not in dados or "resumo" not in dados:
            raise LLMRespostaInvalidaError(
                f"Esperava objeto com 'titulo' e 'resumo', recebeu: {dados!r}"
            )
        return dados["titulo"], dados["resumo"]

    @staticmethod
    def _validar_grupos(bruto: object) -> list[list[str]]:
        """Valida a forma completa da resposta, não só o nível externo — um LLM pode
        devolver `[["INV-1", 42]]` (elemento não-string no meio) e passar por um
        `isinstance(bruto, list)` raso sem que ninguém perceba até `origens` de um
        `ItemComunicado` conter lixo."""
        if not isinstance(bruto, list) or not all(
            isinstance(grupo, list) and all(isinstance(chave, str) for chave in grupo)
            for grupo in bruto
        ):
            raise LLMRespostaInvalidaError(
                f"Esperava lista de listas de strings (grupos de chaves), recebeu: {bruto!r}"
            )
        return bruto

    @staticmethod
    def _parse_json(resposta: str, contexto: str) -> object:
        """Parsing defensivo: modelo pode envolver o JSON em ```json ... ``` mesmo
        com response_format pedido — alguns modelos atrás do gateway ignoram o
        parâmetro. Extrai o bloco antes de tentar parsear puro."""
        texto = resposta.strip()
        bloco_code_fence = re.search(r"```(?:json)?\s*(.*?)```", texto, re.DOTALL)
        if bloco_code_fence:
            texto = bloco_code_fence.group(1).strip()
        try:
            return json.loads(texto)
        except json.JSONDecodeError as exc:
            raise LLMRespostaInvalidaError(
                f"Resposta do modelo em '{contexto}' não é JSON válido: {resposta!r}"
            ) from exc

    async def _chamar(self, prompt: str, json_mode: bool) -> str:
        payload: dict = {
            "model": self._modelo,
            "messages": [{"role": "user", "content": prompt}],
            "temperature": 0.2,  # baixa — queremos consistência, não criatividade, no pipeline
        }
        if self._modelos_fallback:
            payload["models"] = [self._modelo, *self._modelos_fallback]
        if json_mode:
            payload["response_format"] = {"type": "json_object"}

        data = await self._post(payload)
        try:
            return data["choices"][0]["message"]["content"]
        except (KeyError, IndexError) as exc:
            raise LLMRespostaInvalidaError(
                f"Resposta da OpenRouter sem choices[0].message.content: {data!r}"
            ) from exc

    @retry(
        retry=retry_if_exception_type((httpx.TransportError, OpenRouterApiError)),
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=1, max=30),
        reraise=True,
    )
    async def _post(self, payload: dict) -> dict:
        try:
            response = await self._http.post(_ENDPOINT, json=payload)
            response.raise_for_status()
            return response.json()
        except httpx.HTTPStatusError as exc:
            status = exc.response.status_code
            corpo = exc.response.text[:500]
            logger.error("OpenRouter respondeu erro {}: {}", status, corpo)
            if status == 429 or status >= 500:
                # Erro transitório — deixa o @retry decidir se tenta de novo.
                raise OpenRouterApiError(f"OpenRouter retornou {status}: {corpo}") from exc
            # Erro do lado do cliente (400, 401, etc.) não adianta retry — propaga direto.
            raise

    async def aclose(self) -> None:
        await self._http.aclose()
