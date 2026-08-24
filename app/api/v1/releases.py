"""Endpoints REST de Release. Camada fina: só traduz HTTP <-> chamada de serviço,
nenhuma regra de negócio mora aqui."""

from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException

from app.api.v1.schemas import (
    HistoriaJiraOut,
    ItemComunicadoOut,
    ReleaseOut,
    ReleaseProcessadaOut,
)
from app.core.dependencies import get_jira_client, get_llm_provider
from app.domain.enums import StatusPipeline
from app.domain.ports import JiraClient, LLMProvider
from app.infrastructure.jira.jira_rest_client import JiraApiError
from app.infrastructure.llm.provider_pendente import ProviderNaoConfiguradoError
from app.services.pipeline_geracao_release_note import PipelineGeracaoReleaseNote

router = APIRouter(prefix="/releases", tags=["releases"])


@router.get("/{chave_release}/historias", response_model=ReleaseOut)
async def buscar_historias(
    chave_release: str,
    jira_client: Annotated[JiraClient, Depends(get_jira_client)],
) -> ReleaseOut:
    """Busca as histórias de uma Release direto do Jira, sem passar pelo pipeline de
    IA — útil pra conferir o que vai entrar no processamento antes de gastar tokens."""
    try:
        historias = await jira_client.buscar_historias_da_release(chave_release)
    except JiraApiError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc

    return ReleaseOut(
        chave_jira=chave_release,
        # Este endpoint só busca no Jira, nunca roda o pipeline — status é sempre
        # PENDENTE aqui (instanciar Release só pra ler o default seria rebuscado).
        status=StatusPipeline.PENDENTE,
        total_historias=len(historias),
        historias=[
            HistoriaJiraOut(
                chave=h.chave,
                titulo=h.titulo,
                tipo_issue=h.tipo_issue,
                possui_release_note_dedicada=h.possui_release_note_dedicada,
            )
            for h in historias
        ],
    )


@router.post("/{chave_release}/processar", response_model=ReleaseProcessadaOut)
async def processar_release(
    chave_release: str,
    jira_client: Annotated[JiraClient, Depends(get_jira_client)],
    llm_provider: Annotated[LLMProvider, Depends(get_llm_provider)],
) -> ReleaseProcessadaOut:
    """Roda o pipeline de IA completo sobre a Release. Resultado fica em
    AGUARDANDO_REVISAO — nunca publicado direto (ver Release.aprovar)."""
    pipeline = PipelineGeracaoReleaseNote(jira_client, llm_provider)
    try:
        release = await pipeline.executar(chave_release)
    except JiraApiError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc
    except ProviderNaoConfiguradoError as exc:
        raise HTTPException(status_code=501, detail=str(exc)) from exc

    return ReleaseProcessadaOut(
        chave_jira=release.chave_jira,
        status=release.status,
        titulo_executivo=release.titulo_executivo,
        resumo_executivo=release.resumo_executivo,
        itens=[
            ItemComunicadoOut(categoria=i.categoria, texto=i.texto, origens=i.origens)
            for i in release.itens
        ],
    )
