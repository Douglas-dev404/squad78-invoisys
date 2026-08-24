"""Testa o pipeline de orquestração usando fakes das portas JiraClient/LLMProvider —
prova que domínio e services não dependem de nenhum SDK concreto, nem de Jira real
nem de LLM provider decidido. Isso é a arquitetura de portas rendendo na prática."""

import pytest

from app.domain.entities import HistoriaJira
from app.domain.enums import CategoriaAlteracao, StatusPipeline
from app.services.pipeline_geracao_release_note import PipelineGeracaoReleaseNote


class JiraClientFake:
    def __init__(self, historias: list[HistoriaJira]) -> None:
        self._historias = historias

    async def buscar_historias_da_release(self, fix_version: str) -> list[HistoriaJira]:
        return self._historias


class LLMProviderFake:
    """Fake determinístico: agrupa tudo junto que compartilha a palavra 'ICMS',
    categoriza por palavra-chave simples, sem chamar nenhum LLM de verdade."""

    async def categorizar(self, texto_fonte: str) -> CategoriaAlteracao:
        if "corrig" in texto_fonte.lower():
            return CategoriaAlteracao.CORRECAO
        return CategoriaAlteracao.MELHORIA

    async def agrupar_semelhantes(self, textos: list[tuple[str, str]]) -> list[list[str]]:
        icms = [chave for chave, texto in textos if "icms" in texto.lower()]
        outros = [chave for chave, texto in textos if "icms" not in texto.lower()]
        grupos = [g for g in ([icms] + [[c] for c in outros]) if g]
        return grupos

    async def reescrever_linguagem_negocio(
        self, textos_fonte: list[str], categoria: CategoriaAlteracao
    ) -> str:
        return f"[{categoria.value}] Resumo de negócio para {len(textos_fonte)} item(ns)."

    async def gerar_titulo_e_resumo(self, itens_texto: list[str]) -> tuple[str, str]:
        return "Release de Teste", f"{len(itens_texto)} itens nesta release."


async def test_pipeline_roda_ponta_a_ponta_com_fakes():
    historias = [
        HistoriaJira(
            chave="INV-1",
            titulo="Corrige ICMS",
            descricao_tecnica="Corrigido cálculo de ICMS interestadual.",
            tipo_issue="Bug",
        ),
        HistoriaJira(
            chave="INV-2",
            titulo="Ajuste ICMS complementar",
            descricao_tecnica="Corrigido arredondamento no ICMS complementar.",
            tipo_issue="Bug",
        ),
        HistoriaJira(
            chave="INV-3",
            titulo="Nova tela de dashboard",
            descricao_tecnica="Adicionado gráfico de notas emitidas por mês.",
            tipo_issue="Story",
        ),
    ]
    pipeline = PipelineGeracaoReleaseNote(
        jira_client=JiraClientFake(historias),
        llm_provider=LLMProviderFake(),
    )

    release = await pipeline.executar("INV-REL-1")

    assert release.status == StatusPipeline.AGUARDANDO_REVISAO
    assert release.titulo_executivo == "Release de Teste"
    # INV-1 + INV-2 agrupados (ambos citam ICMS) + INV-3 sozinho = 2 itens
    assert len(release.itens) == 2
    origens_agrupadas = next(i.origens for i in release.itens if len(i.origens) == 2)
    assert set(origens_agrupadas) == {"INV-1", "INV-2"}


async def test_pipeline_marca_falha_quando_llm_estoura_excecao():
    """Regressão: `release.marcar_falha()` só tem efeito observável se alguém
    guardar/consultar essa instância. Como `executar()` propaga a exceção antes de
    retornar a Release, capturamos o objeto via monkeypatch do construtor de Release
    para garantir que o status FALHOU foi de fato setado, não só assumido pela leitura
    do código."""
    import app.services.pipeline_geracao_release_note as pipeline_module

    class LLMProviderQuebrado(LLMProviderFake):
        async def agrupar_semelhantes(self, textos):
            raise RuntimeError("simulação de falha do provider")

    historias = [HistoriaJira(chave="INV-1", titulo="X", descricao_tecnica="Y", tipo_issue="Task")]
    pipeline = PipelineGeracaoReleaseNote(
        jira_client=JiraClientFake(historias),
        llm_provider=LLMProviderQuebrado(),
    )

    releases_criadas: list = []
    ReleaseOriginal = pipeline_module.Release

    def release_espia(*args, **kwargs):
        instancia = ReleaseOriginal(*args, **kwargs)
        releases_criadas.append(instancia)
        return instancia

    pipeline_module.Release = release_espia
    try:
        with pytest.raises(RuntimeError):
            await pipeline.executar("INV-REL-2")
    finally:
        pipeline_module.Release = ReleaseOriginal

    assert len(releases_criadas) == 1
    assert releases_criadas[0].status == StatusPipeline.FALHOU
