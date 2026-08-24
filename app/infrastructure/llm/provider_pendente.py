"""Adapter placeholder de LLMProvider — o provider de IA ainda não foi decidido
(pendência registrada em documentação interna de estado do projeto).

Existe só para provar que a arquitetura compila e roda estruturalmente ponta a ponta
mesmo sem provider escolhido: FastAPI sobe, DI resolve, o pipeline chama a porta
`LLMProvider` normalmente. Levanta erro explícito em vez de mentir com um resultado
fake — não é implementação, é o assento reservado do adapter real.

Quando o provider for decidido: criar `app/infrastructure/llm/openai_provider.py` ou
`anthropic_provider.py` implementando `LLMProvider`, trocar o binding em
`app/core/dependencies.py`, e apagar este arquivo. Nenhuma outra camada muda.
"""

from app.domain.enums import CategoriaAlteracao


class ProviderNaoConfiguradoError(Exception):
    """Levantado quando o pipeline tenta chamar o LLM antes de um provider real ser
    configurado. Ver decisão pendente em documentação interna de estado do projeto."""


class ProviderPendente:
    """Implementa a porta `app.domain.ports.LLMProvider` apenas estruturalmente."""

    async def categorizar(self, texto_fonte: str) -> CategoriaAlteracao:
        raise ProviderNaoConfiguradoError(
            "Nenhum LLM provider configurado ainda — ver pendência 'Decidir o LLM "
            "provider único do MVP' em documentação interna de estado do projeto."
        )

    async def agrupar_semelhantes(self, textos: list[tuple[str, str]]) -> list[list[str]]:
        raise ProviderNaoConfiguradoError("Nenhum LLM provider configurado ainda.")

    async def reescrever_linguagem_negocio(
        self, textos_fonte: list[str], categoria: CategoriaAlteracao
    ) -> str:
        raise ProviderNaoConfiguradoError("Nenhum LLM provider configurado ainda.")

    async def gerar_titulo_e_resumo(self, itens_texto: list[str]) -> tuple[str, str]:
        raise ProviderNaoConfiguradoError("Nenhum LLM provider configurado ainda.")
