"""Configuração central via variáveis de ambiente. Única porta de entrada de config —
nunca ler `os.environ` direto em outra camada."""

from functools import lru_cache

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    # Jira
    jira_base_url: str = Field(default="", description="Ex: https://suaempresa.atlassian.net")
    jira_email: str = Field(default="")
    jira_api_token: str = Field(default="")

    # LLM — OpenRouter (gateway único, mesmo schema de request da OpenAI)
    openrouter_api_key: str = Field(default="")
    openrouter_model: str = Field(
        default="openai/gpt-4o-mini",
        description="Formato provedor/modelo, ex: openai/gpt-4o-mini, anthropic/claude-3.5-sonnet",
    )
    openrouter_fallback_models: list[str] = Field(
        default_factory=list,
        description="Modelos alternativos, usados pela OpenRouter em erro 5xx/rate limit",
    )

    # Banco
    database_url: str = Field(
        default="postgresql+asyncpg://invoisys:invoisys@localhost:5432/invoisys"
    )

    # Auth
    jwt_secret: str = Field(default="troque-em-producao")
    jwt_algorithm: str = "HS256"
    jwt_expira_minutos: int = 60

    # App
    log_level: str = "INFO"


@lru_cache
def get_settings() -> Settings:
    return Settings()
