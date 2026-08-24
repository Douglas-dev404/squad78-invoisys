"""Composition root: único lugar do projeto onde uma porta (Protocol) é amarrada a um
adapter concreto. Nenhuma outra camada deve importar um adapter de
`app.infrastructure.*` diretamente — sempre via `Depends()` daqui.

Trocar o LLM provider quando for decidido = mudar só `get_llm_provider` abaixo.
"""

from functools import lru_cache
from typing import Annotated

from fastapi import Depends

from app.core.config import Settings, get_settings
from app.domain.ports import JiraClient, LLMProvider
from app.infrastructure.jira.jira_rest_client import JiraRestClient
from app.infrastructure.llm.provider_pendente import ProviderPendente


@lru_cache
def _jira_client_singleton(settings: Settings) -> JiraRestClient:
    return JiraRestClient(
        base_url=settings.jira_base_url,
        email=settings.jira_email,
        api_token=settings.jira_api_token,
    )


def get_jira_client(
    settings: Annotated[Settings, Depends(get_settings)],
) -> JiraClient:
    return _jira_client_singleton(settings)


def get_llm_provider() -> LLMProvider:
    # TODO(provider-de-ia-pendente): trocar por adapter real quando o provider for
    # decidido — ver documentação interna de estado do projeto.
    return ProviderPendente()
