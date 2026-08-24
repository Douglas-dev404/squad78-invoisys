"""Composition root: único lugar do projeto onde uma porta (Protocol) é amarrada a um
adapter concreto. Nenhuma outra camada deve importar um adapter de
`app.infrastructure.*` diretamente — sempre via `Depends()` daqui.

LLM provider: OpenRouter (gateway único pra múltiplos modelos, decisão de projeto —
ver documentação interna de estado do projeto). Sem `OPENROUTER_API_KEY` configurada,
cai para `ProviderPendente` — mantém o gate honesto de 501 em vez de instanciar um
adapter fadado a falhar em runtime.

Nota: os singletons abaixo não usam `@lru_cache` sobre `Settings` — `BaseSettings` do
Pydantic não é hasheável (unhashable type), então cachear por esse parâmetro quebra em
runtime. Como `get_settings()` já é `@lru_cache` (singleton por processo), cacheamos os
adapters sem parâmetro, lendo `get_settings()` internamente.
"""

from functools import lru_cache

from app.core.config import get_settings
from app.domain.ports import JiraClient, LLMProvider
from app.infrastructure.jira.jira_rest_client import JiraRestClient
from app.infrastructure.llm.openrouter_provider import OpenRouterProvider
from app.infrastructure.llm.provider_pendente import ProviderPendente


@lru_cache
def _jira_client_singleton() -> JiraRestClient:
    settings = get_settings()
    return JiraRestClient(
        base_url=settings.jira_base_url,
        email=settings.jira_email,
        api_token=settings.jira_api_token,
    )


def get_jira_client() -> JiraClient:
    return _jira_client_singleton()


@lru_cache
def _llm_provider_singleton() -> LLMProvider:
    settings = get_settings()
    if not settings.openrouter_api_key:
        return ProviderPendente()
    return OpenRouterProvider(
        api_key=settings.openrouter_api_key,
        modelo=settings.openrouter_model,
        modelos_fallback=settings.openrouter_fallback_models,
    )


def get_llm_provider() -> LLMProvider:
    return _llm_provider_singleton()
