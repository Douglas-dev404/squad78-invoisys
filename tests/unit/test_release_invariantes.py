"""Testa o invariante mais crítico do domínio: nenhuma Release é aprovada sem passar
por AGUARDANDO_REVISAO com itens processados. Ver documentação interna de Regras de Negócio."""

from datetime import datetime

import pytest

from app.domain.entities import (
    ItemComunicado,
    Release,
    ReleaseSemItensProcessadosError,
    RevisaoHumanaObrigatoriaError,
)
from app.domain.enums import CategoriaAlteracao, StatusPipeline


def _release_pendente() -> Release:
    return Release(chave_jira="INV-REL-1", historias=[])


def test_nao_aprova_release_pendente_sem_passar_por_revisao():
    """Mesmo com itens já presentes (estado artificial, só pra isolar esta regra),
    status PENDENTE nunca pode pular direto pra APROVADO — precisa passar por
    AGUARDANDO_REVISAO."""
    release = _release_pendente()
    release.itens = [
        ItemComunicado(
            categoria=CategoriaAlteracao.MELHORIA,
            texto="Item de teste.",
            origens=["INV-1"],
        )
    ]

    with pytest.raises(RevisaoHumanaObrigatoriaError):
        release.aprovar(aprovado_por="darth.code", agora=datetime.now())


def test_nao_aprova_release_aguardando_revisao_sem_itens():
    release = _release_pendente()
    release.status = StatusPipeline.AGUARDANDO_REVISAO  # simula estado inconsistente

    with pytest.raises(ReleaseSemItensProcessadosError):
        release.aprovar(aprovado_por="darth.code", agora=datetime.now())


def test_aprova_release_com_itens_apos_revisao():
    release = _release_pendente()
    release.concluir_processamento(
        itens=[
            ItemComunicado(
                categoria=CategoriaAlteracao.MELHORIA,
                texto="Melhoria de performance no módulo fiscal.",
                origens=["INV-1"],
            )
        ],
        titulo_executivo="Release de Agosto",
        resumo_executivo="Melhorias de performance.",
    )
    assert release.status == StatusPipeline.AGUARDANDO_REVISAO
    assert not release.pronta_para_exportar

    release.aprovar(aprovado_por="darth.code", agora=datetime.now())

    assert release.status == StatusPipeline.APROVADO
    assert release.pronta_para_exportar
    assert release.aprovado_por == "darth.code"


def test_item_comunicado_prioriza_edicao_manual_sobre_texto_da_ia():
    item = ItemComunicado(
        categoria=CategoriaAlteracao.CORRECAO,
        texto="Texto gerado pela IA.",
        origens=["INV-2"],
    )
    assert item.texto_final == "Texto gerado pela IA."

    item.texto_editado_manualmente = "Texto revisado por humano."
    assert item.texto_final == "Texto revisado por humano."
