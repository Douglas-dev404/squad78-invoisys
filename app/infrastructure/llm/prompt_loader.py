"""Carrega os arquivos de prompt versionados em `prompts/` e faz a interpolação de
placeholders `{{chave}}`. Único ponto do código que lê arquivo de prompt — mantém a
regra de "prompt nunca hardcoded em string Python" verificável num lugar só.

Interpolação via `str.replace()`, não `str.format()`: os prompts contêm exemplos de
JSON literal no formato de saída esperado (ex: `{"titulo": "...", "resumo": "..."}`),
e `.format()` interpretaria essas chaves como placeholders, quebrando com KeyError.
Por isso o padrão de placeholder é `{{chave}}` (Mustache-like), que nunca colide com
JSON de exemplo escrito com chave simples.
"""

from functools import lru_cache
from pathlib import Path

_PROMPTS_DIR = Path(__file__).resolve().parents[3] / "prompts"


@lru_cache
def carregar_prompt(nome_arquivo: str) -> str:
    caminho = _PROMPTS_DIR / nome_arquivo
    if not caminho.exists():
        raise FileNotFoundError(
            f"Prompt '{nome_arquivo}' não encontrado em {_PROMPTS_DIR}. "
            "Prompts do pipeline vivem versionados em prompts/, não em string no código."
        )
    return caminho.read_text(encoding="utf-8")


def montar_prompt(nome_arquivo: str, **valores: str) -> str:
    """Carrega o prompt e substitui cada `{{chave}}` pelo valor correspondente."""
    texto = carregar_prompt(nome_arquivo)
    for chave, valor in valores.items():
        texto = texto.replace(f"{{{{{chave}}}}}", valor)
    return texto
