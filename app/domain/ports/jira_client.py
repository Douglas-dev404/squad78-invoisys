"""Porta (Protocol) para acesso ao Jira. Desacopla o domínio/services do SDK/HTTP
concreto — a implementação real (httpx + REST API v3) vive em
`app/infrastructure/jira/`.

Contrato verificado contra a documentação oficial atualizada em 2026-08-24
(endpoint /rest/api/3/search foi removido, substituído por /rest/api/3/search/jql;
paginação por nextPageToken). Ver fontes em documentação interna de estado do projeto.
"""

from typing import Protocol

from app.domain.entities import HistoriaJira


class JiraClient(Protocol):
    async def buscar_historias_da_release(self, fix_version: str) -> list[HistoriaJira]:
        """Busca todas as issues associadas à Release (fixVersion) e suas subtarefas
        do tipo Release Note, já traduzidas para HistoriaJira — incluindo o texto da
        subtarefa Release Note quando existir. Trata paginação internamente
        (nextPageToken), o chamador recebe a lista completa."""
        ...
