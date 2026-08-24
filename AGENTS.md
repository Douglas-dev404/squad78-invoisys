# AGENTS.md — InvoiSys Release Notes

Este arquivo existe para qualquer agente de IA (Claude, Copilot, Cursor, GPT, etc.) que
venha a trabalhar neste repositório, incluindo os de outros colaboradores. Leia isto
**antes** de escrever ou alterar qualquer código.

## O que este projeto é

Residência IV — Squad 78, InvoiSys. Gera automaticamente comunicados de Release
(Release Notes) para clientes, a partir das histórias de uma Release no Jira,
processadas por IA. Ver [README.md](README.md) para o enunciado completo do desafio.

## Antes de codar — gate obrigatório

Não escreva código para uma tarefa não-trivial sem primeiro confirmar, para si mesmo:

1. **Escopo definido** — a ação concreta está clara, não é uma suposição sua.
2. **Arquivo(s) alvo definidos** — você sabe exatamente que arquivo(s) vai tocar.
3. **Regra de negócio presente** — pelo menos um invariante crítico da seção abaixo
   está mapeado e respeitado pela mudança.
4. **Padrão de referência identificado** — existe módulo/componente similar no
   código atual que mostra o padrão a seguir (camadas, nomenclatura, tratamento de erro).
5. **Dependências conhecidas** — você sabe quais libs, portas (`Protocol`) e
   convenções de erro a mudança precisa respeitar.

Se qualquer um desses estiver incerto: pare e pergunte ao humano, ou releia este
arquivo e o código de referência — não assuma.

## Invariantes de negócio — nunca violar

1. **Revisão humana é obrigatória antes de publicar.** Nenhum comunicado sai sem que
   um humano visualize, edite se necessário, e aprove. Isso está implementado como
   método em `app/domain/entities/release.py` (`Release.aprovar()`) — qualquer código
   novo que precise "aprovar" ou "publicar" uma Release **deve** passar por esse
   método, nunca setar `status = APROVADO` diretamente.
2. **Categorias são fixas**: `nova_funcionalidade`, `melhoria`, `correcao`, `outros`
   (ver `app/domain/enums/categoria_alteracao.py`). Não crie categoria nova sem
   confirmação explícita do humano.
3. **Subtarefa Release Note tem prioridade sobre descrição técnica** como fonte de
   texto para a IA, quando existir (ver `HistoriaJira.texto_fonte`).
4. **Fonte de dados é só a API real do Jira** — sem fallback de JSON/CSV colado
   (decisão de projeto, não é limitação técnica esquecida).

## Arquitetura — respeite as fronteiras

Hexagonal leve (Ports & Adapters). Estrutura:

```
app/
├── domain/          # entidades, enums, portas (Protocol) — ZERO import de infra
│   ├── entities/
│   ├── enums/
│   └── ports/        # JiraClient, LLMProvider — interfaces que infra implementa
├── infrastructure/   # adapters concretos — implementam as portas do domain
│   ├── jira/          # JiraRestClient real
│   ├── llm/            # ProviderPendente (stub) até LLM provider ser decidido
│   └── database/        # SQLAlchemy — ainda não implementado
├── services/         # orquestração de casos de uso, depende só de PORTAS, nunca de adapter concreto
├── api/v1/           # FastAPI — camada fina, só traduz HTTP <-> service, zero regra de negócio
├── core/             # config (Settings) e composition root (dependencies.py)
└── export/           # Markdown/HTML/PDF — ainda não implementado
```

**Regra dura**: `app/domain/` nunca importa nada de `app/infrastructure/`. Se você
precisa que o domínio "converse" com Jira ou LLM, isso passa por uma porta em
`app/domain/ports/`, nunca por um import direto de SDK.

**Composition root único**: a amarração porta → adapter concreto acontece só em
`app/core/dependencies.py`. Não instancie um adapter direto dentro de um endpoint ou
service — sempre via `Depends()`.

## Decisões já tomadas — não reabrir sem motivo novo

- Stack: Python 3.12+ / FastAPI / PostgreSQL / SQLAlchemy 2.0.
- LangChain mantido no pipeline de IA (decisão registrada com ressalva — ver documentação interna
  do projeto se você tiver acesso; peso morto para o escopo atual, aceito porque a
  visão é evoluir para RAG/agents depois).
- LLM provider: **ainda não decidido**. Não implemente um adapter real em
  `app/infrastructure/llm/` sem confirmação explícita de qual provider usar — até lá,
  `ProviderPendente` é o adapter ativo, e isso é esperado, não um bug a "corrigir"
  sozinho.
- Um único LLM provider no MVP, nunca dois em paralelo.
- Sem fallback de input JSON/CSV — só API real do Jira.

## Padrões de código

- Nomes de domínio em **português** (é o vocabulário de negócio da InvoiSys/Jira
  deles) — `Release`, `HistoriaJira`, `CategoriaAlteracao`. Nomes técnicos genéricos
  (framework, infra) seguem convenção inglesa normal (`JiraClient`, `LLMProvider`).
- Toda chamada externa (Jira, LLM) passa por retry com `tenacity` — não é opcional.
- Prompts do pipeline de IA vivem em `prompts/*.md`, versionados como arquivo — nunca
  hardcoded como string no Python.
- Testes usam fakes das portas (`Protocol`), não mocks de biblioteca externa — ver
  `tests/unit/test_pipeline_geracao_release_note.py` como referência de padrão.
- Erros de infraestrutura viram exceção de domínio explícita antes de subir pra API
  (ex: `JiraApiError`, `ProviderNaoConfiguradoError`), nunca uma exception crua de
  `httpx` vazando até o endpoint.

## Workflow de git

Ver [CONTRIBUTING.md](CONTRIBUTING.md) para o fluxo completo de branch/PR. Resumo:
`main` é protegida (sem push direto), trabalho novo sai de `develop`, PR obrigatório
com checklist preenchido, CI precisa passar antes de merge.

## Rodando o projeto

```bash
python -m venv .venv && source .venv/Scripts/activate  # Windows Git Bash
pip install -e ".[dev]"
pre-commit install     # hooks de git — roda lint/format antes de cada commit
pytest                 # roda a suíte de testes
uvicorn app.main:app --reload
```

Ou via Docker: `docker compose up`.
