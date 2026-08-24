"""Adapter concreto de JiraClient contra a Jira Cloud REST API v3.

Contrato verificado em 2026-08-24 contra a documentação oficial da Atlassian:
- Endpoint legado /rest/api/3/search foi REMOVIDO. Usamos /rest/api/3/search/jql.
- Paginação por `nextPageToken` (não mais `startAt`) — token expira em 7 dias, não
  cacheamos entre execuções.
- Autenticação: Basic Auth com email + API token (não senha).
- `fields.subtasks` retorna só id/key/summary/issuetype por padrão; para pegar o
  corpo de texto da subtarefa Release Note é preciso uma segunda chamada em
  /rest/api/3/issue/{key} pedindo o campo `description`.

Fontes: developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issue-search,
confluence.atlassian.com/jirakb/run-jql-search-query-using-jira-cloud-rest-api.
"""

from __future__ import annotations

import httpx
from loguru import logger
from tenacity import retry, retry_if_exception_type, stop_after_attempt, wait_exponential

from app.domain.entities import HistoriaJira

_TIPO_ISSUE_RELEASE_NOTE = "Release Note"  # nome do subtask type no Jira da InvoiSys —
# confirmar exato quando tivermos acesso à instância real (pendência rastreada
# na documentação interna de estado do projeto)


class JiraApiError(Exception):
    """Erro de comunicação com a API do Jira, após esgotar as tentativas de retry."""


class JiraRestClient:
    """Implementa a porta `app.domain.ports.JiraClient`."""

    def __init__(
        self,
        base_url: str,
        email: str,
        api_token: str,
        http_client: httpx.AsyncClient | None = None,
    ) -> None:
        self._base_url = base_url.rstrip("/")
        self._auth = (email, api_token)
        # Cliente injetável — em teste, passamos um AsyncClient com transport mockado
        # em vez de bater na rede de verdade.
        self._http = http_client or httpx.AsyncClient(
            base_url=self._base_url,
            auth=self._auth,
            headers={"Accept": "application/json"},
            timeout=httpx.Timeout(15.0, connect=5.0),
        )

    async def buscar_historias_da_release(self, fix_version: str) -> list[HistoriaJira]:
        issues_raw = await self._buscar_todas_issues(fix_version)
        historias: list[HistoriaJira] = []
        for issue in issues_raw:
            texto_release_note = await self._buscar_texto_release_note(issue)
            historias.append(self._to_historia_jira(issue, texto_release_note))
        return historias

    async def _buscar_todas_issues(self, fix_version: str) -> list[dict]:
        jql = f'fixVersion = "{fix_version}"'
        issues: list[dict] = []
        next_page_token: str | None = None

        while True:
            params: dict[str, str | int] = {
                "jql": jql,
                "fields": "summary,description,issuetype,labels,subtasks",
                "maxResults": 100,
            }
            if next_page_token:
                params["nextPageToken"] = next_page_token

            payload = await self._get("/rest/api/3/search/jql", params)
            issues.extend(payload.get("issues", []))

            next_page_token = payload.get("nextPageToken")
            if not next_page_token or payload.get("isLast", True):
                break

        return issues

    async def _buscar_texto_release_note(self, issue: dict) -> str | None:
        """Procura, entre as subtarefas da issue, uma do tipo Release Note e retorna
        seu texto. Custa uma chamada extra por subtarefa candidata — aceitável no
        volume de uma Release (dezenas de issues, não milhares)."""
        subtasks = issue.get("fields", {}).get("subtasks", [])
        for subtask in subtasks:
            tipo = subtask.get("fields", {}).get("issuetype", {}).get("name", "")
            if tipo == _TIPO_ISSUE_RELEASE_NOTE:
                detalhe = await self._get(
                    f"/rest/api/3/issue/{subtask['key']}",
                    {"fields": "description"},
                )
                return self._extrair_texto_description(detalhe.get("fields", {}))
        return None

    @staticmethod
    def _to_historia_jira(issue: dict, texto_release_note: str | None) -> HistoriaJira:
        fields = issue.get("fields", {})
        return HistoriaJira(
            chave=issue["key"],
            titulo=fields.get("summary", ""),
            descricao_tecnica=JiraRestClient._extrair_texto_description(fields),
            tipo_issue=fields.get("issuetype", {}).get("name", ""),
            texto_release_note=texto_release_note,
            labels=fields.get("labels", []),
        )

    @staticmethod
    def _extrair_texto_description(fields: dict) -> str:
        """O campo `description` da API v3 vem em Atlassian Document Format (ADF),
        não texto puro. Extração completa de ADF (tabelas, listas, formatação) fica
        para quando tivermos exemplos reais do Jira da InvoiSys — por ora extrai só
        os nós de texto simples, suficiente para alimentar o pipeline de IA."""
        description = fields.get("description")
        if not description:
            return ""
        if isinstance(description, str):
            return description  # instância antiga/config diferente pode devolver string direto
        return JiraRestClient._extrair_texto_adf(description)

    @staticmethod
    def _extrair_texto_adf(node: dict) -> str:
        partes: list[str] = []
        if node.get("type") == "text":
            partes.append(node.get("text", ""))
        for filho in node.get("content", []):
            partes.append(JiraRestClient._extrair_texto_adf(filho))
        return " ".join(p for p in partes if p).strip()

    @retry(
        retry=retry_if_exception_type(httpx.TransportError),
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=1, max=8),
        reraise=True,
    )
    async def _get(self, path: str, params: dict) -> dict:
        try:
            response = await self._http.get(path, params=params)
            response.raise_for_status()
            return response.json()
        except httpx.HTTPStatusError as exc:
            logger.error(
                "Jira respondeu erro {} em {}: {}",
                exc.response.status_code,
                path,
                exc.response.text[:500],
            )
            raise JiraApiError(f"Jira retornou {exc.response.status_code} em {path}") from exc

    async def aclose(self) -> None:
        await self._http.aclose()
