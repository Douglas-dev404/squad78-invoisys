"""Testa a regra de priorização: subtarefa Release Note dedicada vence descrição
técnica crua. Ver documentação interna de Regras de Negócio."""

from app.domain.entities import HistoriaJira


def test_usa_release_note_quando_existe():
    historia = HistoriaJira(
        chave="INV-1",
        titulo="Corrige cálculo de ICMS",
        descricao_tecnica="Ajustado método CalculaIcms() para considerar alíquota interestadual.",
        tipo_issue="Bug",
        texto_release_note="Corrigimos um problema no cálculo de ICMS interestadual.",
    )
    assert historia.possui_release_note_dedicada
    assert historia.texto_fonte == "Corrigimos um problema no cálculo de ICMS interestadual."


def test_usa_descricao_tecnica_quando_nao_ha_release_note():
    historia = HistoriaJira(
        chave="INV-2",
        titulo="Ajusta índice no banco",
        descricao_tecnica="Criado índice composto em nota_fiscal(tenant_id, emissao).",
        tipo_issue="Task",
        texto_release_note=None,
    )
    assert not historia.possui_release_note_dedicada
    assert historia.texto_fonte == "Criado índice composto em nota_fiscal(tenant_id, emissao)."


def test_release_note_em_branco_conta_como_ausente():
    historia = HistoriaJira(
        chave="INV-3",
        titulo="Refatoração interna",
        descricao_tecnica="Extraído serviço de validação de XML.",
        tipo_issue="Task",
        texto_release_note="   ",
    )
    assert not historia.possui_release_note_dedicada
    assert historia.texto_fonte == "Extraído serviço de validação de XML."
