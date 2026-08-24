"""Um item já processado pela IA: uma ou mais histórias do Jira condensadas em um único
parágrafo de comunicado, já categorizado e em linguagem de negócio.

Um ItemComunicado pode se originar de várias HistoriaJira (agrupamento semântico —
estágio 3 do pipeline, ver documentação interna de Regras de Negócio) — por isso `origens` é lista,
nunca uma chave única.
"""

from dataclasses import dataclass, field

from app.domain.enums import CategoriaAlteracao


@dataclass(slots=True)
class ItemComunicado:
    categoria: CategoriaAlteracao
    texto: str  # já em linguagem de negócio, pronto pro cliente ler
    origens: list[str]  # chaves Jira que originaram este item (ex: ["INV-1234", "INV-1240"])
    texto_editado_manualmente: str | None = field(default=None)

    @property
    def texto_final(self) -> str:
        """Texto que efetivamente vai pro comunicado publicado: a edição humana
        sobrescreve o texto gerado pela IA quando existir. Nunca o contrário —
        revisão humana é a última palavra (invariante de negócio)."""
        return self.texto_editado_manualmente or self.texto
