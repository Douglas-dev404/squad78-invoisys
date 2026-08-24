"""Status do pipeline de geração de uma Release Note.

O pipeline tem 5 estágios de IA (extração, categorização, agrupamento, reescrita,
título/resumo). Não modelamos um status por estágio — isso é detalhe de execução do
serviço de orquestração, não estado persistente relevante pro domínio. O que o domínio
precisa saber é: a geração ainda não rodou, está rodando, terminou e aguarda revisão
humana, ou já foi aprovada e publicada.

Ver documentação interna de Regras de Negócio — revisão humana é invariante
obrigatório, não opcional.
"""

from enum import StrEnum


class StatusPipeline(StrEnum):
    PENDENTE = "pendente"
    PROCESSANDO = "processando"
    AGUARDANDO_REVISAO = "aguardando_revisao"
    APROVADO = "aprovado"
    FALHOU = "falhou"
