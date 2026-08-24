"""Categorias fixas de alteração usadas para classificar cada história da Release.

Fixas por decisão de negócio — ver documentação interna de Regras de Negócio.
Não adicionar categoria nova sem necessidade real validada com o responsável técnico do projeto.
"""

from enum import StrEnum


class CategoriaAlteracao(StrEnum):
    NOVA_FUNCIONALIDADE = "nova_funcionalidade"
    MELHORIA = "melhoria"
    CORRECAO = "correcao"
    OUTROS = "outros"
