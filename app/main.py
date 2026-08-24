from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI
from loguru import logger

from app.api.v1.releases import router as releases_router
from app.core.config import get_settings

settings = get_settings()


@asynccontextmanager
async def lifespan(_app: FastAPI) -> AsyncIterator[None]:
    logger.info("InvoiSys Release Notes API iniciada — log_level={}", settings.log_level)
    yield


app = FastAPI(
    title="InvoiSys — Gerador de Release Notes",
    description="Gera comunicados de Release automaticamente a partir do Jira, via IA.",
    version="0.1.0",
    lifespan=lifespan,
)

app.include_router(releases_router, prefix="/api/v1")


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}
