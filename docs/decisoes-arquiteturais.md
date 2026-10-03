# InvoiSys — Registro de Decisões Arquiteturais (ADRs)

Cada decisão relevante do projeto, com o **contexto** que a motivou, as **alternativas**
consideradas, e as **consequências** (boas e ruins) que ela traz. O código mostra *o
que* foi feito; este documento guarda *por que*.

**Como usar:** antes de propor mudar algo listado aqui, leia o ADR. Se o motivo original
ainda vale, a decisão fica. Se surgiu um fato novo, abra um PR que **adicione** um ADR
novo marcando o antigo como "Substituído por ADR-0XX". Não apague decisões antigas: o
histórico também é documentação.

**Status possíveis:** `Aceita` · `Substituída` · `Proposta` (aguardando decisão).

---

## Índice

| # | Decisão | Status | Data |
|---|---|---|---|
| [001](#adr-001--arquitetura-hexagonal-ports--adapters) | Arquitetura hexagonal (Ports & Adapters) | Aceita | 2026-08-24 |
| [002](#adr-002--backend-em-net-10-migrado-de-python) | Backend em .NET 10 (migrado de Python) | Aceita | ~2026-09-13 |
| [003](#adr-003--openrouter-como-provider-único-de-llm-sem-sdk-de-orquestração) | OpenRouter como provider único de LLM, sem SDK de orquestração | Aceita | 2026-08-24 |
| [004](#adr-004--pipeline-linear-de-5-estágios-estágio-1-sem-llm) | Pipeline linear de 5 estágios, estágio 1 sem LLM | Aceita | 2026-08-24 |
| [005](#adr-005--prompts-versionados-como-arquivo-interpolação-chave) | Prompts versionados como arquivo, interpolação `{{chave}}` | Aceita | 2026-08-24 |
| [006](#adr-006--fonte-única-api-real-do-jira) | Fonte única: API real do Jira | Aceita | 2026-08-24 |
| [007](#adr-007--revisão-humana-como-invariante-do-domínio-entidades-ricas) | Revisão humana como invariante do domínio (entidades ricas) | Aceita | 2026-08-24 |
| [008](#adr-008--múltiplos-públicos-como-versaocomunicado-geração--revisão) | Múltiplos públicos como `VersaoComunicado`; geração ≠ revisão | Aceita | 2026-09-18 |
| [009](#adr-009--repository-por-agregado-portas-de-leitura-isoladas) | Repository por agregado, portas de leitura isoladas | Aceita | 2026-09-26 |
| [010](#adr-010--ids-gerados-no-domínio-com-valuegeneratednever) | Ids gerados no domínio, com `ValueGeneratedNever()` | Aceita | 2026-09-26 |
| [011](#adr-011--ef-core-10-code-first--postgresql-16) | EF Core 10 Code-First + PostgreSQL 16 | Aceita | 2026-09-18 |
| [012](#adr-012--testes-de-banco-com-testcontainers-e-postgres-real) | Testes de banco com Testcontainers e Postgres real | Aceita | 2026-09-26 |
| [013](#adr-013--exceções-de-integração-são-contrato-da-porta) | Exceções de integração são contrato da porta | Aceita | 2026-09-26 |
| [014](#adr-014--adapters-pendente-quando-falta-credencial-501-honesto) | Adapters "Pendente" quando falta credencial (501 honesto) | Aceita | 2026-08-24 |
| [015](#adr-015--resiliência-http-no-composition-root) | Resiliência HTTP no composition root | Aceita | 2026-09-13 |
| [016](#adr-016--migração-de-banco-no-startup-é-opt-in) | Migração de banco no startup é opt-in | Aceita | 2026-09-18 |
| [017](#adr-017--desconfiança-estruturada-da-saída-do-llm) | Desconfiança estruturada da saída do LLM | Aceita | 2026-08-24 |
| [018](#adr-018--categorias-e-públicos-fixos) | Categorias e públicos fixos | Aceita | 2026-08-24 |
| [019](#adr-019--entrega-em-fatias-verticais) | Entrega em fatias verticais | Aceita | 2026-08-24 |
| [020](#adr-020--versão-aprovada-não-é-reprocessada-sem-reabrir) | Versão aprovada não é reprocessada sem reabrir | Aceita | 2026-09-29 |
| [021](#adr-021--item-de-versão-aprovada-não-é-editado-sem-reabrir) | Item de versão aprovada não é editado sem reabrir | Aceita | 2026-09-30 |
| [022](#adr-022--endpoint-só-chama-caso-de-uso-erros-de-revisão-padronizados) | Endpoint só chama caso de uso; erros de revisão padronizados | Aceita | 2026-10-03 |
| [P-02](#p-02--desenho-da-autenticação) | Desenho da autenticação | **Proposta** | — |
| [P-03](#p-03--onde-mora-o-render-da-exportação) | Onde mora o render da exportação | **Proposta** | — |

---

## ADR-001 — Arquitetura hexagonal (Ports & Adapters)

**Contexto.** O coração do produto depende de duas integrações externas **incertas**: o
Jira da InvoiSys (ainda não validado contra a instância real) e um LLM (modelo e
provedor podem mudar por custo ou qualidade). Além disso, a regra mais importante do
negócio, "nada publica sem revisão humana", não pode depender de um `if` que alguém
esqueça num endpoint.

**Decisão.** Quatro projetos com dependência apontando só para dentro:

- `InvoiSys.Domain`: entidades, enums e **portas** (interfaces). Não referencia nada.
- `InvoiSys.Application`: casos de uso, dependendo só de portas.
- `InvoiSys.Infrastructure`: **adapters** que implementam as portas (Jira, LLM, EF Core)
  e o **composition root** (`DependencyInjection.cs`), o único lugar que liga porta a
  adapter.
- `InvoiSys.Api`: adapter de entrada HTTP. Camada fina, sem regra de negócio.

A direção é forçada pelas referências dos `.csproj`: o domínio não *consegue* importar
infraestrutura.

**Alternativas descartadas.**
- *Camadas tradicionais (Controller → Service → Repository concreto):* o service
  acoplaria ao `HttpClient` do Jira e ao EF, e testar o pipeline exigiria rede e banco.
- *Clean Architecture completa (CQRS, MediatR, Use Case por classe):* cerimônia demais
  para um MVP com um caso de uso central. O hexagonal "leve" dá o isolamento sem os
  pacotes extras.

**Consequências.**
- ✅ Pipeline 100% testável com fakes (`FakeJiraClient`, `FakeLlmProvider`), sem rede e
  sem token.
- ✅ Trocar de LLM = escrever um adapter + mudar uma linha no composition root.
- ✅ A migração Python → .NET ([ADR-002](#adr-002--backend-em-net-10-migrado-de-python))
  preservou o desenho inteiro.
- ⚠️ Mais arquivos e indireção do que um CRUD simples. Quem chega precisa entender
  "porta × adapter" antes de contribuir (ver [AGENTS.md](../AGENTS.md)).

---

## ADR-002 — Backend em .NET 10 (migrado de Python)

**Contexto.** A fundação nasceu em Python + FastAPI (ecossistema de IA maduro). Depois,
a InvoiSys sinalizou preferência por **Node ou .NET**. Manter Python seria entregar um
MVP numa stack que o cliente não quer manter.

**Decisão.** Reescrever o backend em **.NET 10 / ASP.NET Minimal API** (commit
`3b3a75a`), preservando a arquitetura hexagonal. O código Python foi removido do
repositório (continua no histórico do git antes de `3b3a75a`).

**Alternativas descartadas.**
- *Node/TypeScript:* também atendia à preferência. .NET foi escolhido pela tipagem forte
  do domínio rico, pelo EF Core maduro para o modelo relacional, e pela familiaridade do
  time.
- *Manter Python:* tecnicamente adequado, estrategicamente desalinhado com o cliente.

**Consequências.**
- ✅ Stack alinhada com quem vai herdar o código.
- ✅ Contrato de API preservado: enums saem como `"nova_funcionalidade"`,
  `"aguardando_revisao"`, igual ao Python, via `ParaValor()` (ver
  [ADR-018](#adr-018--categorias-e-públicos-fixos)).
- ⚠️ Resíduos de nomenclatura Python em docs e variáveis de ambiente precisaram ser
  caçados depois (ex.: `.env.example` usava `JIRA_BASE_URL`, que o .NET não lê; corrigido
  para `Jira__BaseUrl`).

---

## ADR-003 — OpenRouter como provider único de LLM, sem SDK de orquestração

**Contexto.** O pipeline precisa de um LLM com saída estruturada (JSON). O desafio pede
viabilidade, não um modelo específico, e o custo por Release importa.

**Decisão.**
- **OpenRouter** como gateway único: um endpoint (`/api/v1/chat/completions`), schema
  compatível com OpenAI, modelo escolhido por configuração (`provedor/modelo`).
- **`HttpClient` direto**, sem LangChain nem Semantic Kernel.
- **Um único provider no MVP.** Resiliência entre *modelos* via parâmetro `models` da
  própria OpenRouter (`OpenRouter:ModelosFallback`), sem round-trip nosso.

**Alternativas descartadas.**
- *LangChain / Semantic Kernel:* o pipeline é linear, com 5 estágios, sem RAG, sem
  agentes e sem memória. O SDK traria abstração e dependência sem resolver nenhum problema
  real. Foi avaliado e removido.
- *Dois providers em paralelo (ex.: OpenAI + Anthropic direto):* dobra a superfície de
  configuração e de teste. O fallback nativo da OpenRouter cobre o caso de
  indisponibilidade.

**Consequências.**
- ✅ Trocar de modelo é mudar uma variável de ambiente.
- ✅ Adapter pequeno e totalmente testável com `FakeHttpMessageHandler`.
- ⚠️ Dependência de um gateway terceiro. Mitigada pela porta `ILlmProvider`: um adapter
  direto para outro provedor não mexe no pipeline.

---

## ADR-004 — Pipeline linear de 5 estágios, estágio 1 sem LLM

**Contexto.** O enunciado pede: interpretar, usar a subtarefa Release Note, resumir
semelhantes, eliminar duplicatas, adaptar a linguagem, categorizar e sugerir título e
resumo.

**Decisão.** Cinco estágios, cada um com responsabilidade única e prompt próprio:

1. **Extração e limpeza:** normalização determinística em C#, **sem LLM** (não faz
   sentido pagar token para colapsar espaços).
2. **Categorização**, 3. **Agrupamento semântico**, 4. **Reescrita em linguagem de
   negócio**, 5. **Título e resumo:** um método cada em `ILlmProvider`.

Na execução, o agrupamento (3) roda **antes** da categorização (2): categoriza-se o
grupo pela primeira história, já que o grupo trata do mesmo assunto.

**Alternativas descartadas.**
- *Um prompt único "gere o comunicado":* impossível de testar por etapa, de depurar
  quando sai ruim, e de garantir o formato (categorias fixas, rastreio de origens).
- *Status persistido por estágio:* detalhe de execução, não estado de negócio. O domínio
  só precisa de pendente/processando/aguardando revisão/aprovado/falhou.

**Consequências.**
- ✅ Cada estágio pode ter seu prompt ajustado e testado isoladamente.
- ✅ `ItemComunicado.Origens` rastreia quais histórias geraram cada parágrafo.
- ⚠️ Custo = `2 + 2·G` chamadas (G = número de grupos). Aceitável para Releases de
  dezenas de histórias. Se virar gargalo, dá para paralelizar o loop por grupo.

---

## ADR-005 — Prompts versionados como arquivo, interpolação `{{chave}}`

**Contexto.** Tom de voz e few-shot vão ser ajustados muitas vezes, provavelmente por
quem não mexe em C#.

**Decisão.** Um arquivo `.md` por estágio em [`prompts/`](../prompts/), carregado em
runtime pelo `PromptLoader`, que é o **único** ponto que lê prompt. Placeholders usam
`{{chave}}` e são substituídos por `string.Replace` literal.

**Por que não interpolação nativa (`$"..."`, `string.Format`):** os prompts contêm JSON
literal de exemplo (`{"titulo": "...", "resumo": "..."}`), que um formatador trataria
como placeholder e quebraria. Isso já aconteceu uma vez; não reintroduza.

**Consequências.**
- ✅ Mudança de prompt aparece como diff limpo de texto, separado de mudança de lógica.
- ✅ A imagem Docker copia `prompts/` como conteúdo (`COPY prompts ./prompts`).
- ⚠️ Prompt ausente só é detectado em runtime (`FileNotFoundException` com mensagem
  clara), não em compilação.

---

## ADR-006 — Fonte única: API real do Jira

**Contexto.** Um "modo manual" (colar JSON/CSV das histórias) facilitaria a demo sem
acesso ao Jira.

**Decisão.** **Sem fallback de input.** A única entrada é a Jira Cloud REST API v3.
Acesso ao Jira foi confirmado como priorizado. Contrato verificado contra a documentação
oficial em 2026-08-24:

- `GET /rest/api/3/search/jql` (o antigo `/search` foi **removido** pela Atlassian);
- paginação por `nextPageToken` (não mais `startAt`);
- Basic Auth com e-mail + **API token**;
- corpo da subtarefa exige uma chamada extra em `/rest/api/3/issue/{key}`;
- `description` em ADF, achatado para texto.

**Consequências.**
- ✅ Um caminho só para manter, testar e documentar.
- ✅ O MVP prova a integração real, que é o que o cliente precisa ver.
- ⚠️ Sem Jira configurado, não há demo do pipeline (a API responde 501). Os testes
  usam `FakeJiraClient`.
- Pendências reais em [contratos-integracao.md](contratos-integracao.md).

---

## ADR-007 — Revisão humana como invariante do domínio (entidades ricas)

**Contexto.** Publicar um comunicado errado para cliente fiscal tem custo real. O
enunciado exige revisão humana antes da publicação.

**Decisão.** Entidades **ricas**: o invariante vive dentro do objeto.

- `VersaoComunicado.Aprovar()` é o **único** caminho para `aprovado`, e exige itens, ao
  menos um item não excluído, e status `aguardando_revisao`.
- `ComunicadoExportado` só é construído a partir de versão `ProntaParaExportar`; senão
  lança `ReleaseNaoAprovadaException`.
- Coleções expostas como `IReadOnlyList`; propriedades com `private set`/`private init`;
  construtor privado só para o EF.

**Alternativas descartadas.**
- *Entidades anêmicas + validação no service/endpoint:* qualquer caminho novo (job,
  endpoint, script) poderia esquecer a checagem.

**Consequências.**
- ✅ Não existe caminho de código que publique sem aprovação, e há teste provando.
- ✅ CHECKs no banco repetem as regras críticas (ex.: reprovação sem motivo), valendo até
  para SQL direto.
- ⚠️ O EF precisa de mapeamento por campo (`PropertyAccessMode.Field`) e construtores
  privados. **Não** adicione setter público "porque o EF precisa".

---

## ADR-008 — Múltiplos públicos como `VersaoComunicado`; geração ≠ revisão

**Contexto.** Diferencial do enunciado: versões do comunicado para Cliente, Comercial,
Suporte e Interno. O primeiro desenho tinha um único status na Release, e não conseguia
representar "Cliente aprovado, Suporte ainda em ajuste".

**Decisão.**
- `VersaoComunicado` é entidade própria, **uma por público** (unique
  `(release_id, publico)`), com título, resumo, itens e **ciclo de revisão próprio**.
- Dois enums separados: `StatusPipeline` (na Release: *a IA rodou?*) e `StatusRevisao`
  (na versão: *o humano aprovou?*).
- A Release só vira `aprovado` quando **todas** as versões geradas estão aprovadas.
- `ItemComunicado` pertence à versão, não à Release.

**Consequências.**
- ✅ O diferencial cabe no modelo sem remendo.
- ✅ Atalhos (`Release.Itens`, `TituloExecutivo`, `VersaoCliente`) mantêm simples o uso
  do caso mais comum (só Cliente).
- ⚠️ Hoje o pipeline gera **só a versão Cliente**. Gerar as demais é trabalho de
  aplicação (Fase 6 do roadmap), sem mudança de schema.

---

## ADR-009 — Repository por agregado, portas de leitura isoladas

**Contexto.** Com várias entidades filhas, a tentação é um repository por tabela, o que
deixaria gravar uma versão ou item **por fora** da Release e pular o recálculo de status
e os invariantes.

**Decisão.**
- **Escrita só pelo agregado:** `IReleaseRepository.SalvarAsync` persiste Release +
  histórias + versões + itens + execuções. `VersaoComunicado` e `ItemComunicado` **não
  têm porta**.
- **Portas somente leitura, sem tracking** para consultas que não precisam do agregado
  inteiro: `IHistoriaJiraRepository`, `IExecucaoPipelineRepository`.
- **Agregados próprios:** `IUsuarioRepository` (sem delete: desligar é
  `Usuario.Desativar()`), `IComunicadoExportadoRepository` (**append-only**: auditoria de
  publicação), `IDestaqueHeroRepository` (leitura da tela de login).
- `SalvarAsync` não distingue Add/Update: o ChangeTracker do EF decide.

**Consequências.**
- ✅ Aprovar, editar ou excluir item obrigatoriamente carrega a Release, e os invariantes
  rodam.
- ✅ Consultas leves (histórico de execuções, histórias de uma Release) não pagam o custo
  do agregado completo.
- ⚠️ Carregar o agregado faz `Include` de 3 coleções + itens. Aceitável no volume de uma
  Release. Se crescer, usar split query.

---

## ADR-010 — Ids gerados no domínio, com `ValueGeneratedNever()`

**Contexto.** Bug real encontrado em 2026-09-26: as entidades geram `Id = Guid.NewGuid()`
no construtor, mas o mapeamento EF não dizia isso. O EF assume "chave preenchida = linha
existente", então uma versão, item ou execução **nova** adicionada a uma Release já
carregada virava `UPDATE` de linha inexistente → `DbUpdateConcurrencyException`.

**Decisão.** Toda configuration mapeia `Id` com `.ValueGeneratedNever()` (mais
`gen_random_uuid()` como default, só para INSERT via SQL direto). Migration
`ChavesGeradasNoDominio` (sem SQL; só snapshot).

**Por que Id no domínio e não no banco:** a entidade nasce com identidade, antes de
tocar o banco. A `ExecucaoPipeline` referencia `ReleaseId` no construtor, e os testes
unitários funcionam sem persistência.

**Consequências.**
- ✅ Coberto por `PersistenciaAgregadoReleaseTests` (5 cenários que falhavam antes).
- ⚠️ **Não remova** `ValueGeneratedNever()` de nenhuma configuration nova.

---

## ADR-011 — EF Core 10 Code-First + PostgreSQL 16

**Contexto.** Modelo relacional claro (8 tabelas, FKs, CHECKs), com domínio rico em C#.

**Decisão.**
- **EF Core Code-First** com `IEntityTypeConfiguration` por entidade; migrations
  versionadas em `Infrastructure/Database/Migrations`.
- **PostgreSQL 16** (mesma imagem no compose e nos testes).
- `snake_case` no SQL (`EFCore.NamingConventions`), `PascalCase` em C#.
- Enums gravados como **texto** (`ParaValor()`) + CHECK constraint com o vocabulário.
- `text[]` para `Labels` e `Origens` (atributos de valor sem ciclo de vida), com índice
  GIN em `origens`.
- Trigger `set_atualizado_em()` nas três tabelas que sofrem UPDATE.

**Alternativas descartadas.**
- *SQL puro + Dapper:* schema e código divergiriam com o tempo. Code-First mantém os
  dois sincronizados.
- *Enum como inteiro:* ilegível no banco e frágil a reordenação.

**Consequências.**
- ✅ Schema auditável por migration, com decisões explicadas em
  [modelagem-de-dominio.md](modelagem-de-dominio.md).
- ⚠️ O mapeamento de `text[]` exige value converter + comparer customizados
  (`ArrayConversionHelper`), o que motivou o [ADR-012](#adr-012--testes-de-banco-com-testcontainers-e-postgres-real).

---

## ADR-012 — Testes de banco com Testcontainers e Postgres real

**Contexto.** Providers falsos (EF InMemory, SQLite) não validam `text[]`, CHECK
constraints, triggers nem índices GIN, justamente onde estão os riscos.

**Decisão.** Testes de integração sobem `postgres:16-alpine` via **Testcontainers**
(`PostgresContainerFixture`), aplicam as migrations reais e exercitam os repositories.

**Consequências.**
- ✅ O bug do [ADR-010](#adr-010--ids-gerados-no-domínio-com-valuegeneratednever) foi
  achado exatamente por esses testes.
- ⚠️ `dotnet test` exige Docker rodando. O CI (ubuntu-latest) já tem.

---

## ADR-013 — Exceções de integração são contrato da porta

**Contexto.** As exceções do Jira e do LLM viviam nos adapters (`Infrastructure.Jira`,
`Infrastructure.Llm`), e o endpoint precisava importar a infraestrutura para capturá-las.
Trocar de adapter quebraria o tratamento de erro em silêncio (virava 500). Além disso, o
corpo da resposta externa ia parar no `detail` HTTP e no banco.

**Decisão.** Exceções em `Domain/Ports/ExcecoesDeIntegracao.cs`:

| Exceção | Quando | HTTP |
|---|---|---|
| `IntegracaoExternaException` (base) | qualquer falha externa | 502 |
| `JiraApiException` | Jira respondeu erro após retry | 502 |
| `LlmApiException` | OpenRouter respondeu erro após retry | 502 |
| `LlmRespostaInvalidaException` | modelo respondeu fora do formato | 502 |
| `ProviderNaoConfiguradoException` | adapter "Pendente" (sem credencial) | 501 |

`Endpoints/` **não pode** ter `using InvoiSys.Infrastructure.*`. Corpo de resposta
externa vai **só para o log**, nunca para a mensagem da exceção.

**Consequências.**
- ✅ A API trata erro pelo tipo do domínio, sem saber se o adapter é real, pendente ou
  fake.
- ✅ Não vaza detalhe da conta Jira ou OpenRouter para quem chama a API.
- ⚠️ **Implementação incompleta:** os adapters só traduzem resposta HTTP de
  erro. Falha de transporte (`HttpRequestException`, `TimeoutRejectedException`,
  `BrokenCircuitException`) ainda vaza e vira 500. Precisa de `catch` em
  `JiraRestClient.GetAsync` e `OpenRouterProvider.PostAsync`.

---

## ADR-014 — Adapters "Pendente" quando falta credencial (501 honesto)

**Contexto.** Sem `BaseUrl`, o `HttpClient` do Jira estoura lá no fundo, e o usuário vê
um 500 com stack trace. Sem chave do LLM, um adapter "fake que devolve texto de
exemplo" mentiria sobre o funcionamento.

**Decisão.** O composition root verifica `JiraOptions.EstaConfigurado` /
`OpenRouterOptions.EstaConfigurado` e, se falso, injeta `JiraClientPendente` /
`ProviderPendente`, que lançam `ProviderNaoConfiguradoException` com **a instrução do
que configurar** → HTTP 501.

**Consequências.**
- ✅ A API sobe e roda estruturalmente sem nenhuma credencial (útil em CI e para
  onboarding).
- ⚠️ Isso **não é bug** a ser "corrigido" trocando por um fake. É comportamento
  esperado.

---

## ADR-015 — Resiliência HTTP no composition root

**Contexto.** Jira e LLM falham de forma transitória (429, 5xx, timeout).

**Decisão.** `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler`) em
cada `HttpClient`, configurado no `DependencyInjection.cs`, e não no adapter:

| | Jira | OpenRouter |
|---|---|---|
| Tentativas | 3 (1 + 2 retries), backoff exponencial | 3, backoff exponencial |
| Delay base | 1 s | 2 s |
| Timeout por tentativa / total | 15 s / 60 s | 60 s / 180 s |
| Circuit breaker (janela) | 30 s | 120 s |

4xx de cliente (401, 404) **não** é retentado: tentar de novo não muda o resultado.

**Consequências.**
- ✅ Adapter só traduz HTTP em domínio. Transporte é problema do transporte.
- ⚠️ Toda nova chamada externa **deve** passar por um `HttpClient` registrado com policy.
  Não é opcional.

---

## ADR-016 — Migração de banco no startup é opt-in

**Contexto.** `docker compose up` com banco vazio dava "relation does not exist" na
primeira query.

**Decisão.** Flag `Database:MigrarAoIniciar` (padrão `false`). O compose liga; produção
não.

**Por que não sempre:** em produção com várias instâncias, todas correriam para aplicar
a mesma migration, e uma migration destrutiva entraria sem revisão. Lá, migrar é passo
próprio do deploy. E os testes de integração da API sobem a aplicação sem banco.

---

## ADR-017 — Desconfiança estruturada da saída do LLM

**Contexto.** LLM alucina, esquece itens, embrulha JSON em markdown e ignora
`response_format`.

**Decisão.** Toda saída do modelo é validada **na forma completa**, e cada falha tem
tratamento definido:

| Falha do modelo | Tratamento | Onde |
|---|---|---|
| Chave inventada no agrupamento | descartada; grupo só com chaves inventadas é omitido | `PipelineGeracaoReleaseNote.ProcessarGruposAsync` |
| Chave real esquecida no agrupamento | reinserida como grupo próprio + log de aviso | `OpenRouterProvider.AgruparSemelhantesAsync` |
| JSON dentro de ```` ```json ```` | extraído antes do parse | `ParseJson` |
| Elemento não-string dentro de grupo | `LlmRespostaInvalidaException` | `ValidarGrupos` |
| Categoria fora do enum | `LlmRespostaInvalidaException` | `CategorizarAsync` |
| Chave Jira duplicada entre páginas | primeira ocorrência vence | `ProcessarGruposAsync` |

Princípio: **perder uma história real é pior que ela aparecer sem agrupamento**; inventar
um item a partir do nada é pior que omiti-lo.

---

## ADR-018 — Categorias e públicos fixos

**Decisão.** `CategoriaAlteracao` = `nova_funcionalidade`, `melhoria`, `correcao`,
`outros`. `PublicoAlvo` = `cliente`, `comercial`, `suporte`, `interno`. Ambos são enums
com valor serializado em `snake_case` (via `ParaValor()`) e CHECK no banco. Categoria ou
público novo **só com confirmação explícita** do responsável pelo projeto.

**Por que fixo:** o prompt de categorização lista as categorias válidas, e o comunicado
é organizado por elas. Uma categoria criada "por conveniência" quebraria prompt,
parser, CHECK e front de uma vez.

---

## ADR-019 — Entrega em fatias verticais

**Contexto.** Time pequeno, disponibilidade parcial, prazo 2026-12-31. O plano original
previa construir tudo em paralelo, com squad maior.

**Decisão.** Primeiro um caminho **ponta a ponta mínimo** (Jira → IA → revisão →
Markdown), depois os diferenciais (múltiplos públicos, HTML/PDF, métricas).

**Consequências.**
- ✅ Sempre há algo demonstrável; risco de integração aparece cedo.
- ⚠️ O domínio já comporta diferenciais que a aplicação ainda não usa (ex.: públicos além
  de Cliente). Isso é intencional, não código morto.

---

## ADR-020 — Versão aprovada não é reprocessada sem reabrir

**Contexto.** Com a persistência do pipeline (#20), `/processar` sobre uma Release já
existente reaproveita o agregado. Sem regra, reprocessar uma Release cuja versão já foi
**aprovada** trocava histórias, itens, título e resumo e devolvia a versão para
`aguardando_revisao` **em silêncio**: o texto que um humano aprovou sumia da versão viva
(as exportações antigas sobreviviam, por serem append-only).

**Opções consideradas.**
1. Bloquear o reprocessamento de versão aprovada e exigir `Reabrir()` antes.
2. Permitir, registrando na `ExecucaoPipeline` que houve sobrescrita.
3. Versionar o conteúdo (histórico de versões por público), com schema novo.

**Decisão.** Opção 1.

- `Release.GarantirQuePodeReprocessar(publico)` lança `RevisaoHumanaObrigatoriaException`
  se a versão daquele público estiver aprovada.
- O pipeline chama essa verificação **antes** do Jira e do LLM: a recusa não gasta
  token, não altera o agregado e não registra execução (nada chegou a rodar).
- `ConcluirProcessamento` repete a verificação, e `VersaoComunicado.PreencherConteudo`
  recusa conteúdo novo em versão aprovada: última linha de defesa para qualquer caminho
  de código futuro.
- A API responde **409 Conflict**, com a instrução de reabrir a revisão.
- Versão **reprovada**, **reaberta** ou aguardando revisão continua podendo ser
  reprocessada. Reprocessar um público não é bloqueado pela aprovação de outro.

**Consequências.**
- ✅ Coerente com o invariante central ([ADR-007](#adr-007--revisão-humana-como-invariante-do-domínio-entidades-ricas)):
  aprovação é decisão humana e só um humano a desfaz.
- ✅ Sem mudança de schema.
- ⚠️ Um passo a mais para o revisor quando quiser gerar de novo algo já aprovado
  (reabrir → reprocessar → revisar).
- Coberto por testes de domínio, do pipeline, da API (409) e de integração com Postgres
  real (`PipelineGeracaoReleaseNotePersistenciaTests`).

---

## ADR-021 — Item de versão aprovada não é editado sem reabrir

**Contexto.** A #23 expõe na API a revisão item a item (editar texto, excluir,
reincluir). No domínio, `ItemComunicado.EditarManualmente/Excluir/Reincluir` eram
públicos e não conheciam o status da versão: dava para mudar o texto de um item de uma
versão **já aprovada**, e a exportação sairia com um texto que nenhum humano aprovou.
É o mesmo buraco que a [ADR-020](#adr-020--versão-aprovada-não-é-reprocessada-sem-reabrir)
fechou para o reprocessamento, agora pela porta da edição manual.

**Opções consideradas.**
1. Bloquear alteração de item em versão aprovada e exigir `Reabrir()` antes.
2. Permitir e devolver a versão para `aguardando_revisao` automaticamente.
3. Deixar como estava e confiar na tela.

**Decisão.** Opção 1, a mesma regra da ADR-020 aplicada a itens.

- A revisão de item passa pelo agregado: `Release.EditarItem/ExcluirItem/ReincluirItem(itemId, …)`
  acha a versão dona do item (o id é global) e delega para
  `VersaoComunicado.EditarItem/ExcluirItem/ReincluirItem`, que lança
  `TransicaoDeStatusInvalidaException` se a versão estiver aprovada.
- Os métodos de `ItemComunicado` passaram a `internal`: de fora do domínio não há como
  alterar um item sem passar pela verificação.
- Item que não existe na Release: `ItemNaoEncontradoException`. Texto de edição vazio:
  `ArgumentException` (para tirar o item do comunicado, o caminho é excluir).
- A API responde **409** (versão aprovada, com a instrução de reabrir), **404** (Release
  ou item não encontrado, inclusive item de outra Release) e **422** (texto vazio, ver
  [ADR-022](#adr-022--endpoint-só-chama-caso-de-uso-erros-de-revisão-padronizados)).
- A aprovação de um público não trava os itens de outro. Versão reprovada ou aguardando
  revisão continua editável.

**Consequências.**
- ✅ O texto exportado é sempre o texto que foi aprovado.
- ✅ Sem mudança de schema.
- ⚠️ Corrigir um detalhe depois de aprovar exige reabrir e aprovar de novo.
- Coberto por `RevisaoDeItensTests` (domínio), `ApiItensTests` (API) e
  `PersistenciaAgregadoReleaseTests` (Postgres real).

---

## ADR-022 — Endpoint só chama caso de uso; erros de revisão padronizados

**Contexto.** A #22 (aprovar/reprovar/reabrir) chegou com o endpoint buscando a Release
no repository, decidindo o público default e revalidando o motivo, com `try/catch`
repetido por rota e status divergentes para o mesmo tipo de erro (aprovar 2× = 409,
reabrir inválido = 422). A #23 (itens) seguia o mesmo caminho e respondia 400 para
entrada inválida. Sem regra explícita, cada PR inventava a sua.

**Opções consideradas.**
1. Endpoint orquestra (repository → domínio → salvar) direto, por serem operações curtas.
2. Orquestração em caso de uso na Application; API só converte DTO e traduz erro.

**Decisão.** Opção 2, aplicando a [ADR-001](#adr-001--arquitetura-hexagonal-ports--adapters).

- Endpoint **não** recebe repository nem toma decisão: converte o DTO, chama o caso de
  uso (`RevisaoComunicado` para revisão) e traduz o resultado em HTTP.
- Um arquivo de endpoints por recurso (`ReleaseEndpoints`, `RevisaoEndpoints`, ...).
- Release inexistente é `ReleaseNaoEncontradaException`, lançada pelo caso de uso → **404**.
- Estado que impede a operação (sem comunicado, já aprovada, transição inválida) → **409**.
- Entrada que o domínio recusa (`ArgumentException`: revisor, motivo, texto vazio) e
  público ausente ou desconhecido → **422**. Não usamos 400 para isso: o JSON é válido,
  é o conteúdo que não passa na regra.
- Erro sempre em `ProblemDetails`, mapeado num ponto só por arquivo de endpoints.
- Público é obrigatório na revisão: sem default para Cliente.
- Revisor obrigatório no domínio (`VersaoComunicado.Aprovar/Reprovar`): aprovação sem
  quem aprovou não serve como trilha da revisão humana
  ([ADR-007](#adr-007--revisão-humana-como-invariante-do-domínio-entidades-ricas)).

**Consequências.**
- ✅ A API não tem regra de negócio para divergir do domínio.
- ✅ Contrato de erro previsível para o frontend: 404 / 409 / 422.
- ⚠️ Mais uma classe na Application por grupo de casos de uso.
- Coberto por `ApiRevisaoTests` (HTTP) e `VersaoComunicadoTests` (domínio).

---

## Decisões em aberto

### P-02 — Desenho da autenticação

A decidir: JWT stateless (sem tabela de refresh tokens: decisão já registrada na
modelagem) × sessão; algoritmo de hash (bcrypt × argon2id); normalização de e-mail
(hoje comparado exato); fluxo de recuperação de senha (o front espera
`{ message, expiresInMinutes }`, o que implica um token de reset com expiração, ainda
não modelado); papéis (`Usuario.Papel` é texto livre de propósito até o RBAC existir).

### P-03 — Onde mora o render da exportação

A decidir: um serviço de aplicação (`ExportarComunicado`) que monta o Markdown a partir
de `VersaoComunicado.ItensPublicaveis`, agrupado por categoria, e grava via
`IComunicadoExportadoRepository`; ou uma porta `IRenderizadorComunicado` com um adapter
por formato (Markdown agora; HTML/PDF depois). A segunda opção encaixa melhor no
diferencial de múltiplos formatos.
