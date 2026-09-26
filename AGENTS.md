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
5. **Dependências conhecidas** — você sabe quais libs, portas (interfaces) e
   convenções de erro a mudança precisa respeitar.

Se qualquer um desses estiver incerto: pare e pergunte ao humano, ou releia este
arquivo e o código de referência — não assuma.

## Invariantes de negócio — nunca violar

1. **Revisão humana é obrigatória antes de publicar.** Nenhum comunicado sai sem que
   um humano visualize, edite se necessário, e aprove. Isso está implementado como
   método em `src/InvoiSys.Domain/Entities/VersaoComunicado.cs`
   (`VersaoComunicado.Aprovar()`, exposto por `Release.Aprovar()`) — qualquer código
   novo que precise "aprovar" ou "publicar" **deve** passar por esse método, nunca
   setar `Status = Aprovado` diretamente. A revisão é **por público-alvo**: aprovar a
   versão do Cliente não libera a do Suporte.
2. **Categorias são fixas**: `nova_funcionalidade`, `melhoria`, `correcao`, `outros`
   (ver `src/InvoiSys.Domain/Enums/CategoriaAlteracao.cs`). Não crie categoria nova sem
   confirmação explícita do humano.
3. **Subtarefa Release Note tem prioridade sobre descrição técnica** como fonte de
   texto para a IA, quando existir (ver `HistoriaJira.TextoFonte`).
4. **Fonte de dados é só a API real do Jira** — sem fallback de JSON/CSV colado
   (decisão de projeto, não é limitação técnica esquecida).

## Arquitetura — respeite as fronteiras

Hexagonal leve (Ports & Adapters). Estrutura:

```
src/
├── InvoiSys.Domain/          # entidades, enums, portas — ZERO dependência de infra
│   ├── Entities/             # Release (raiz), VersaoComunicado, ItemComunicado, ...
│   ├── Enums/
│   └── Ports/                # IJiraClient, ILlmProvider — interfaces que infra implementa
├── InvoiSys.Application/     # orquestração de casos de uso, depende só de PORTAS
│   └── Pipeline/             # PipelineGeracaoReleaseNote (5 estágios)
├── InvoiSys.Infrastructure/  # adapters concretos — implementam as portas do domínio
│   ├── Jira/                 # JiraRestClient real
│   ├── Llm/                  # OpenRouterProvider real; ProviderPendente se sem API key
│   ├── Database/             # EF Core: DbContext, Configurations, Migrations
│   └── DependencyInjection.cs  # composition root
└── InvoiSys.Api/             # ASP.NET Minimal API — camada fina, zero regra de negócio
    ├── Endpoints/
    └── Contracts/            # DTOs — nunca expor entidade de domínio direto

tests-dotnet/InvoiSys.Tests/  # xUnit — unit + integração
frontend/                     # React + Vite (telas de login e recuperação de senha)
prompts/                      # prompts do pipeline, versionados como arquivo
docs/                         # modelagem de domínio + ERD + contratos de integração
```

**Regra dura**: `InvoiSys.Domain` nunca referencia `InvoiSys.Infrastructure`. Se você
precisa que o domínio "converse" com Jira ou LLM, isso passa por uma porta em
`InvoiSys.Domain/Ports/`, nunca por um using direto de SDK. A direção das referências
de projeto (`.csproj`) já força isso — se precisar inverter, você está errando.

**Composition root único**: a amarração porta → adapter concreto acontece só em
`InvoiSys.Infrastructure/DependencyInjection.cs`. Não instancie um adapter direto
dentro de um endpoint ou serviço — sempre via injeção de dependência.

## Decisões já tomadas — não reabrir sem motivo novo

- Stack: **.NET 10 / ASP.NET Minimal API / PostgreSQL 16 / EF Core 10**. A fundação
  original era Python + FastAPI; foi reescrita em .NET no commit `3b3a75a`, alinhando
  com a preferência sinalizada pela InvoiSys (Node ou .NET). A arquitetura hexagonal
  foi preservada na migração — o que mudou foi a linguagem, não o desenho. O código
  Python foi removido do repositório; se precisar consultá-lo, está no histórico do
  git antes de `3b3a75a`.
- LangChain **removido**. Avaliado e descartado: o adapter real de LLM (OpenRouter)
  usa só `httpx`, schema compatível com OpenAI, sem necessidade de SDK de orquestração
  pra um pipeline linear de 5 estágios sem RAG.
- LLM provider: **OpenRouter** (`src/InvoiSys.Infrastructure/Llm/OpenRouterProvider.cs`),
  gateway único pra múltiplos modelos via um schema de API compatível com OpenAI.
  Modelo configurável via `OPENROUTER_MODEL` (formato `provedor/modelo`). Sem
  `OPENROUTER_API_KEY` configurada, o composition root cai para `ProviderPendente` —
  isso é esperado em ambiente sem chave, não um bug a "corrigir" trocando o fallback.
- Um único LLM provider no MVP, nunca dois em paralelo (fallback entre *modelos* via
  `OPENROUTER_FALLBACK_MODELS` é diferente disso — mesma OpenRouter, resiliência).
- Sem fallback de input JSON/CSV — só API real do Jira.
- Contrato completo das duas integrações externas (endpoints, paginação, auth, o que
  já foi verificado contra doc oficial vs. o que ainda é suposição pendente) em
  [docs/contratos-integracao.md](docs/contratos-integracao.md).
- Interpolação de prompt usa `{{chave}}` (Mustache-like) via `PromptLoader.Montar()` em
  `src/InvoiSys.Infrastructure/Llm/PromptLoader.cs`, nunca interpolação de string nativa
  — os prompts têm JSON literal de exemplo no formato de saída, que seria interpretado
  como placeholder e quebraria (bug real já corrigido uma vez, não reintroduza).
- **Múltiplos públicos-alvo** (Cliente, Comercial, Suporte, Interno) são modelados como
  `VersaoComunicado`, uma por público, cada uma com seu próprio ciclo de revisão. Ver
  [docs/modelagem-de-dominio.md](docs/modelagem-de-dominio.md).
- **`IHistoriaJiraRepository` é uma porta de leitura isolada, não um repository de
  agregado.** `HistoriaJira` é filha do agregado `Release` (1:N, FK shadow `ReleaseId`,
  cascade delete — ver `HistoriaJiraConfiguration.cs`/`ReleaseConfiguration.cs`) e
  continua sendo gravada só via `IReleaseRepository.SalvarAsync`. A porta existe para
  consultar histórias (por id, por chave dentro de uma Release, ou por Release) sem
  carregar o agregado inteiro; por isso é somente leitura e sem tracking — uma porta de
  escrita deixaria gravar uma história por fora do agregado. A regra "repository é por
  agregado, não por entidade filha" (docstring de
  `src/InvoiSys.Domain/Ports/IReleaseRepository.cs`) segue valendo para as demais
  filhas: `ExecucaoPipeline` ganhou a mesma porta somente leitura
  (`IExecucaoPipelineRepository`, histórico de execuções/rastreabilidade);
  `VersaoComunicado` e `ItemComunicado` **não têm porta própria** — aprovar, reprovar,
  editar ou excluir item passa obrigatoriamente pela `Release` carregada (é ela que
  recalcula o status quando todas as versões estão aprovadas). Ciclo de vida completo
  coberto em `PersistenciaAgregadoReleaseTests`.
- **Repositories de agregado próprio**: `IUsuarioRepository` (busca por id/e-mail +
  `SalvarAsync`, sem delete — desligar é `Usuario.Desativar()`),
  `IComunicadoExportadoRepository` (append-only: só inclui e consulta, nunca altera nem
  apaga — é auditoria de publicação).
- **Ids são gerados no domínio (`Guid.NewGuid()`), nunca no banco**: toda
  configuration mapeia `Id` com `.ValueGeneratedNever()`. Sem isso o EF trata filho novo
  com chave preenchida como linha existente e o save vira UPDATE de nada
  (`DbUpdateConcurrencyException`) — bug real, já corrigido; não remova.
- **Testes de banco usam Testcontainers com Postgres real** (`postgres:16-alpine`,
  mesma imagem do `docker-compose.yml`), não EF InMemory nem SQLite — o mapeamento de
  `text[]` de `HistoriaJira.Labels` via value converter customizado
  (`ArrayConversionHelper`) não seria validado fielmente por um provider fake. Ver
  `tests-dotnet/InvoiSys.Tests/Integration/PostgresContainerFixture.cs`.

## Padrões de código

- Nomes de domínio em **português** (é o vocabulário de negócio da InvoiSys/Jira
  deles) — `Release`, `HistoriaJira`, `CategoriaAlteracao`. Nomes técnicos genéricos
  (framework, infra) seguem convenção inglesa normal (`JiraClient`, `LLMProvider`).
- Toda chamada externa (Jira, LLM) passa por retry via
  `Microsoft.Extensions.Http.Resilience` — não é opcional.
- Prompts do pipeline de IA vivem em `prompts/*.md`, versionados como arquivo — nunca
  hardcoded como string no C#.
- Testes usam fakes das portas (interfaces), não mocks de biblioteca externa — ver
  `tests-dotnet/InvoiSys.Tests/Unit/PipelineGeracaoReleaseNoteTests.cs` como referência.
- Erros de infraestrutura viram exceção de domínio explícita antes de subir pra API
  (ex: `JiraApiException`, `ProviderNaoConfiguradoException`), nunca uma exception crua
  de `HttpClient` vazando até o endpoint.
- Entidades de domínio são **ricas**: invariante vive dentro do objeto, coleções são
  expostas como `IReadOnlyList` e mutadas só pelos métodos de ciclo de vida. Não
  adicione setter público "porque o EF precisa" — use `private init` e construtor
  privado, como nas entidades existentes.

## Workflow de git

Ver [CONTRIBUTING.md](CONTRIBUTING.md) para o fluxo completo de branch/PR. Resumo:
`main` é protegida (sem push direto), trabalho novo sai de `develop`, PR obrigatório
com checklist preenchido, CI precisa passar antes de merge.

## Rodando o projeto

Backend:

```bash
dotnet restore InvoiSys.slnx
dotnet build InvoiSys.slnx
dotnet test InvoiSys.slnx          # suíte completa
dotnet format InvoiSys.slnx        # formatação (o CI verifica com --verify-no-changes)
dotnet run --project src/InvoiSys.Api
```

Frontend:

```bash
cd frontend
npm ci
npm run dev
```

Banco (migrations EF Core):

```bash
dotnet ef migrations add <Nome> --project src/InvoiSys.Infrastructure --startup-project src/InvoiSys.Api --output-dir Database/Migrations
dotnet ef database update --project src/InvoiSys.Infrastructure --startup-project src/InvoiSys.Api
```

Tudo junto via Docker: `docker compose up --build` (API em `:8080`, Postgres em `:5432`).
