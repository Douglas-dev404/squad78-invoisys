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
