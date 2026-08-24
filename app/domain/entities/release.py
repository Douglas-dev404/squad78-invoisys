"""Release — agregado raiz do domínio.

Dona do ciclo de vida do comunicado: recebe histórias brutas do Jira, guarda o
resultado do pipeline de IA (itens categorizados), e só libera publicação depois de
aprovação humana explícita. Essa regra vive aqui como método, não como `if` espalhado
pela API ou pelo service — é a garantia de que ninguém consegue publicar sem o gate,
não importa por qual caminho de código chegue até este objeto.

Ver documentação interna de Regras de Negócio.
"""

from dataclasses import dataclass, field
from datetime import datetime

from app.domain.entities.historia_jira import HistoriaJira
from app.domain.entities.item_comunicado import ItemComunicado
from app.domain.enums import StatusPipeline


class RevisaoHumanaObrigatoriaError(Exception):
    """Levantado quando algo tenta publicar uma Release sem aprovação humana."""


class ReleaseSemItensProcessadosError(Exception):
    """Levantado quando se tenta aprovar uma Release cujo pipeline de IA não rodou."""


@dataclass(slots=True)
class Release:
    chave_jira: str  # ex: "RELEASE-2026-08"
    historias: list[HistoriaJira]
    status: StatusPipeline = StatusPipeline.PENDENTE
    titulo_executivo: str | None = None
    resumo_executivo: str | None = None
    itens: list[ItemComunicado] = field(default_factory=list)
    aprovado_por: str | None = None
    aprovado_em: datetime | None = None

    def marcar_processando(self) -> None:
        self.status = StatusPipeline.PROCESSANDO

    def concluir_processamento(
        self,
        itens: list[ItemComunicado],
        titulo_executivo: str,
        resumo_executivo: str,
    ) -> None:
        """Chamado pelo orquestrador de IA ao fim do estágio 5 do pipeline. Deixa a
        Release pronta para revisão humana — nunca pronta para publicação direta."""
        self.itens = itens
        self.titulo_executivo = titulo_executivo
        self.resumo_executivo = resumo_executivo
        self.status = StatusPipeline.AGUARDANDO_REVISAO

    def marcar_falha(self) -> None:
        self.status = StatusPipeline.FALHOU

    def aprovar(self, aprovado_por: str, agora: datetime) -> None:
        """Único caminho válido para uma Release avançar para o status APROVADO.

        Invariante de negócio: uma Release sem itens processados pela IA não pode ser
        aprovada (nada pra revisar), e o status precisa estar AGUARDANDO_REVISAO —
        não dá pra "pular a fila" de PENDENTE direto pra APROVADO.
        """
        if not self.itens:
            raise ReleaseSemItensProcessadosError(
                f"Release {self.chave_jira} não tem itens processados pelo pipeline de IA."
            )
        if self.status != StatusPipeline.AGUARDANDO_REVISAO:
            raise RevisaoHumanaObrigatoriaError(
                f"Release {self.chave_jira} está em status {self.status}, "
                "não pode ser aprovada sem passar por AGUARDANDO_REVISAO."
            )
        self.status = StatusPipeline.APROVADO
        self.aprovado_por = aprovado_por
        self.aprovado_em = agora

    @property
    def pronta_para_exportar(self) -> bool:
        """Gate único que toda camada de exportação (Markdown/HTML/PDF) deve checar
        antes de gerar o arquivo final. Publicar sem isso é bug, não decisão de produto."""
        return self.status == StatusPipeline.APROVADO
