"""Porta (Protocol) que qualquer provider de LLM precisa implementar.

Este é o contrato que desacopla o pipeline de IA do provider concreto. O domínio e os
serviços de aplicação dependem só desta interface — nunca de `openai`, `anthropic` ou
qualquer SDK específico diretamente. Quando o provider for decidido (ver pendência em
documentação interna de estado do projeto), a implementação entra em
`app/infrastructure/llm/` como um adapter concreto desta porta, e nada no domínio ou
nos services muda.

Cada método corresponde a um estágio do pipeline de 5 estágios (ver documentação
interna de Regras de Negócio) que precisa de fato de uma chamada ao modelo. Extração e
Limpeza (estágio 1) é normalização de texto puro, não chama LLM — fica no domain/services.
"""

from typing import Protocol

from app.domain.enums import CategoriaAlteracao


class LLMProvider(Protocol):
    async def categorizar(self, texto_fonte: str) -> CategoriaAlteracao:
        """Estágio 2: classifica um texto em uma das categorias fixas de negócio."""
        ...

    async def agrupar_semelhantes(self, textos: list[tuple[str, str]]) -> list[list[str]]:
        """Estágio 3: recebe pares (chave_jira, texto_fonte) e retorna grupos de
        chaves que devem ser fundidas em um único item de comunicado por tratarem do
        mesmo assunto. Cada chave de entrada aparece em exatamente um grupo de saída
        (grupos de tamanho 1 = história que não tem duplicata)."""
        ...

    async def reescrever_linguagem_negocio(
        self, textos_fonte: list[str], categoria: CategoriaAlteracao
    ) -> str:
        """Estágio 4: funde um grupo de textos técnicos em um único parágrafo de
        comunicado em linguagem de negócio, coerente com a categoria."""
        ...

    async def gerar_titulo_e_resumo(self, itens_texto: list[str]) -> tuple[str, str]:
        """Estágio 5: gera (título_executivo, resumo_executivo) para a Release inteira,
        a partir dos itens de comunicado já prontos. Retorna (titulo, resumo)."""
        ...
