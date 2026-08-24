"""DTOs da API — nunca expor entidades de domínio direto na resposta HTTP. Isso
mantém o contrato de API estável mesmo que o domínio mude internamente."""

from pydantic import BaseModel

from app.domain.enums import CategoriaAlteracao, StatusPipeline


class HistoriaJiraOut(BaseModel):
    chave: str
    titulo: str
    tipo_issue: str
    possui_release_note_dedicada: bool


class ReleaseOut(BaseModel):
    chave_jira: str
    status: StatusPipeline
    total_historias: int
    historias: list[HistoriaJiraOut]


class ItemComunicadoOut(BaseModel):
    categoria: CategoriaAlteracao
    texto: str
    origens: list[str]


class ReleaseProcessadaOut(BaseModel):
    chave_jira: str
    status: StatusPipeline
    titulo_executivo: str | None
    resumo_executivo: str | None
    itens: list[ItemComunicadoOut]
