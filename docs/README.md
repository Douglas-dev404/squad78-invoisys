# Documentação — índice

| Documento | Para quem | Conteúdo |
|---|---|---|
| [decisoes-arquiteturais.md](decisoes-arquiteturais.md) | todo mundo que vai mudar algo estrutural | **ADRs**: cada decisão com contexto, alternativas e consequências, mais as decisões em aberto |
| [modelagem-de-dominio.md](modelagem-de-dominio.md) | quem mexe em entidade, migration ou query | ERD, relacionamentos, fluxo de estados, tabela por tabela, índices, CHECKs, triggers |
| [contratos-integracao.md](contratos-integracao.md) | quem mexe nos adapters Jira/OpenRouter | O que foi verificado contra a doc oficial × o que ainda é suposição |
| [erd.png](erd.png) / [erd.svg](erd.svg) | — | Diagrama entidade-relacionamento |
| [plano-historias-jira-data-access.md](plano-historias-jira-data-access.md), [checklist-historias-jira-data-access.md](checklist-historias-jira-data-access.md) | histórico | Plano e checklist de uma tarefa já entregue (repository de `HistoriaJira` + Testcontainers). Ficam como referência de como planejar uma tarefa neste repo |

Documentação por módulo (o que cada parte faz, métodos principais, o que falta):

- [README principal](../README.md): fluxo ponta a ponta, percurso do dado, roadmap
- [src/](../src/README.md) · [Domain](../src/InvoiSys.Domain/README.md) ·
  [Application](../src/InvoiSys.Application/README.md) ·
  [Infrastructure](../src/InvoiSys.Infrastructure/README.md) ·
  [Api](../src/InvoiSys.Api/README.md)
- [tests-dotnet/](../tests-dotnet/README.md) · [frontend/](../frontend/README.md) ·
  [prompts/](../prompts/README.md)

## Mantendo a documentação viva

- Mudou regra de negócio ou invariante → atualize o README do módulo e, se for decisão,
  **adicione** um ADR (não reescreva o antigo).
- Mudou schema → atualize `modelagem-de-dominio.md` junto com a migration.
- Verificou algo de integração contra a instância real → mova de "pendência" para
  "verificado" em `contratos-integracao.md`, com data.
