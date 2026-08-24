"""Testa o estágio 1 do pipeline (Extração e Limpeza) isoladamente.

Regressão: a limpeza precisa normalizar o campo que `HistoriaJira.texto_fonte`
efetivamente devolve — quando há Release Note dedicada, é `texto_release_note` que
importa, não `descricao_tecnica`. Limpar o campo errado deixa espaços/quebras de linha
soltos chegando ao LLM sem que nenhum teste do pipeline em si perceba, porque os fakes
de teste já usam texto bem-comportado."""

from app.domain.entities import HistoriaJira
from app.services.pipeline_geracao_release_note import PipelineGeracaoReleaseNote


def test_limpa_descricao_tecnica_quando_nao_ha_release_note():
    historia = HistoriaJira(
        chave="INV-1",
        titulo="  Ajusta índice  ",
        descricao_tecnica="Criado   índice\ncomposto  em nota_fiscal.",
        tipo_issue="Task",
        texto_release_note=None,
    )
    limpa = PipelineGeracaoReleaseNote._extrair_e_limpar(historia)
    assert limpa.texto_fonte == "Criado índice composto em nota_fiscal."
    assert limpa.titulo == "Ajusta índice"


def test_limpa_texto_release_note_quando_existe_release_note_dedicada():
    """Este é o caso que estava quebrado: a limpeza normalizava descricao_tecnica,
    mas texto_fonte devolve texto_release_note quando ele existe — a sujeira nunca
    era removida do texto que de fato alimenta o LLM."""
    historia = HistoriaJira(
        chave="INV-2",
        titulo="Corrige ICMS",
        descricao_tecnica="Ajustado CalculaIcms() para considerar alíquota.",
        tipo_issue="Bug",
        texto_release_note="Corrigimos   um problema\nno cálculo  de ICMS.",
    )
    limpa = PipelineGeracaoReleaseNote._extrair_e_limpar(historia)
    assert limpa.texto_fonte == "Corrigimos um problema no cálculo de ICMS."
    # descricao_tecnica não é o que alimenta o LLM neste caso — não precisa ser limpa,
    # e de fato permanece como veio (comportamento documentado, não descuido).
    assert limpa.descricao_tecnica == "Ajustado CalculaIcms() para considerar alíquota."
