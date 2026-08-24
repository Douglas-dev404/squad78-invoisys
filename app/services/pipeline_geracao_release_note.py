"""Orquestra o pipeline de 5 estágios de IA sobre uma Release. Depende só das portas
(`JiraClient`, `LLMProvider`) — nunca de adapter concreto. Isso é o que permite este
código existir e ser testável hoje, mesmo sem o LLM provider decidido: em teste,
injetamos um fake de `LLMProvider`; em produção, o adapter real vem via
`app/core/dependencies.py`.

Estágio 1 (Extração e Limpeza) é normalização pura de texto — não chama LLM, fica
aqui mesmo como função auxiliar. Estágios 2-5 chamam a porta `LLMProvider`.
Ver documentação interna de Regras de Negócio para o desenho completo.
"""

from app.domain.entities import HistoriaJira, ItemComunicado, Release
from app.domain.ports import JiraClient, LLMProvider


class PipelineGeracaoReleaseNote:
    def __init__(self, jira_client: JiraClient, llm_provider: LLMProvider) -> None:
        self._jira = jira_client
        self._llm = llm_provider

    async def executar(self, chave_release: str) -> Release:
        historias = await self._jira.buscar_historias_da_release(chave_release)
        release = Release(chave_jira=chave_release, historias=historias)
        release.marcar_processando()

        try:
            historias_limpas = [self._extrair_e_limpar(h) for h in historias]
            grupos = await self._llm.agrupar_semelhantes(
                [(h.chave, h.texto_fonte) for h in historias_limpas]
            )
            itens = await self._processar_grupos(historias_limpas, grupos)
            titulo, resumo = await self._llm.gerar_titulo_e_resumo([item.texto for item in itens])
            release.concluir_processamento(itens, titulo, resumo)
        except Exception:
            release.marcar_falha()
            raise

        return release

    @staticmethod
    def _extrair_e_limpar(historia: HistoriaJira) -> HistoriaJira:
        """Estágio 1: normaliza espaços/quebras de linha do texto fonte. Não chama
        LLM — é limpeza determinística, não tem por que gastar tokens nisso."""
        texto_limpo = " ".join(historia.texto_fonte.split())
        return HistoriaJira(
            chave=historia.chave,
            titulo=historia.titulo.strip(),
            descricao_tecnica=texto_limpo,
            tipo_issue=historia.tipo_issue,
            texto_release_note=historia.texto_release_note,
            labels=historia.labels,
        )

    async def _processar_grupos(
        self, historias: list[HistoriaJira], grupos: list[list[str]]
    ) -> list[ItemComunicado]:
        por_chave = {h.chave: h for h in historias}
        itens: list[ItemComunicado] = []

        for grupo_chaves in grupos:
            historias_do_grupo = [por_chave[chave] for chave in grupo_chaves]
            # Categoriza pela primeira história do grupo — elas já foram agrupadas
            # por serem semanticamente equivalentes, então compartilham categoria.
            categoria = await self._llm.categorizar(historias_do_grupo[0].texto_fonte)
            texto = await self._llm.reescrever_linguagem_negocio(
                [h.texto_fonte for h in historias_do_grupo], categoria
            )
            itens.append(ItemComunicado(categoria=categoria, texto=texto, origens=grupo_chaves))

        return itens
