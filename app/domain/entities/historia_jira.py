"""Representa uma história (issue) do Jira, tal como veio da origem — sem qualquer
processamento de IA ainda. É o dado bruto de entrada do pipeline.
"""

from dataclasses import dataclass, field


@dataclass(frozen=True, slots=True)
class HistoriaJira:
    chave: str  # ex: "INV-1234"
    titulo: str
    descricao_tecnica: str
    tipo_issue: str  # Story, Bug, Task, etc — vocabulário do Jira, não nosso
    texto_release_note: str | None = None  # conteúdo da subtarefa "Release Note", quando existir
    labels: list[str] = field(default_factory=list)

    @property
    def possui_release_note_dedicada(self) -> bool:
        """Regra de negócio: se a subtarefa Release Note existe e tem conteúdo, ela é
        a fonte preferencial de texto — a IA deve priorizá-la sobre a descrição técnica
        crua. Ver documentação interna de Regras de Negócio."""
        return bool(self.texto_release_note and self.texto_release_note.strip())

    @property
    def texto_fonte(self) -> str:
        """Texto que efetivamente alimenta o pipeline de IA: Release Note dedicada
        quando existe, senão a descrição técnica."""
        if self.possui_release_note_dedicada:
            return self.texto_release_note  # type: ignore[return-value]
        return self.descricao_tecnica
