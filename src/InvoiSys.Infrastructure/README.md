# InvoiSys.Infrastructure — adapters de saída

Tudo o que fala com o mundo externo: **Jira**, **LLM (OpenRouter)** e **PostgreSQL**.
Cada classe aqui implementa uma **porta** de `InvoiSys.Domain.Ports`. Nenhuma outra
camada instancia estas classes direto; elas chegam via DI, amarradas no
**composition root**.

```
InvoiSys.Infrastructure/
├── DependencyInjection.cs      # composition root: porta → adapter, HttpClient, resiliência
├── Configuration/              # JiraOptions, OpenRouterOptions (ligadas ao appsettings/env)
├── Jira/                       # JiraRestClient, JiraClientPendente
├── Llm/                        # OpenRouterProvider, PromptLoader, ProviderPendente, ChatCompletionRequest
└── Database/                   # InvoiSysDbContext, repositories, Configurations/, Migrations/
```

---

## Composition root: `DependencyInjection.AddInfrastructure`

É o **único lugar** do projeto que decide qual implementação atende cada porta:

| Porta | Adapter | Condição |
|---|---|---|
| `IJiraClient` | `JiraRestClient` | `Jira:BaseUrl`, `Jira:Email` e `Jira:ApiToken` preenchidos |
| `IJiraClient` | `JiraClientPendente` | qualquer um vazio → **501** com instrução |
| `ILlmProvider` | `OpenRouterProvider` | `OpenRouter:ApiKey` preenchida |
| `ILlmProvider` | `ProviderPendente` | chave vazia → **501** com instrução |
| `IReleaseRepository`, `IHistoriaJiraRepository`, `IExecucaoPipelineRepository`, `IComunicadoExportadoRepository`, `IUsuarioRepository`, `IDestaqueHeroRepository` | `*Repository` em `Database/` | sempre (Scoped) |

Também é aqui que vivem as **políticas de transporte** (não no adapter):

| HttpClient | Auth | Timeout | Resiliência (`AddStandardResilienceHandler`) |
|---|---|---|---|
| Jira | Basic (e-mail + API token) | 30 s | 3 tentativas, backoff exp. 1 s, 15 s/tentativa, 60 s total, circuit breaker 30 s |
| OpenRouter | Bearer (API key) | 120 s | 3 tentativas, backoff exp. 2 s, 60 s/tentativa, 180 s total, circuit breaker 120 s |

Detalhes e motivos: [ADR-014](../../docs/decisoes-arquiteturais.md#adr-014--adapters-pendente-quando-falta-credencial-501-honesto)
e [ADR-015](../../docs/decisoes-arquiteturais.md#adr-015--resiliência-http-no-composition-root).

---

## Jira: `JiraRestClient`

Implementa `IJiraClient` contra a **Jira Cloud REST API v3** (contrato em
[docs/contratos-integracao.md](../../docs/contratos-integracao.md)).

| Método | O que faz |
|---|---|
| `BuscarHistoriasDaReleaseAsync(fixVersion)` | Ponto de entrada: busca todas as issues e, para cada uma, o texto da subtarefa Release Note |
| `BuscarTodasIssuesAsync` | `GET /rest/api/3/search/jql` com `jql=fixVersion="X"`, `maxResults=100`, laço por `nextPageToken` até `isLast` |
| `BuscarTextoReleaseNoteAsync` | Procura subtarefa com `issuetype.name == "Release Note"` e busca a `description` dela em `/rest/api/3/issue/{key}` (1 chamada extra por issue que tem a subtarefa) |
| `ParaHistoriaJira` | `JsonElement` → `HistoriaJira` (chave, título, descrição, tipo, labels, texto da Release Note) |
| `ExtrairTextoDescription` / `ExtrairTextoAdf` | Achata o ADF (árvore `type`/`content`/`text`) em texto. Aceita `description` string (instâncias antigas) |
| `GetAsync` | Erro HTTP → loga corpo (até 500 chars) → lança `JiraApiException` **sem** o corpo |

⚠️ **Pendências:** confirmar o nome exato do subtask type na instância real da InvoiSys
(constante `TipoIssueReleaseNote`); ampliar a extração de ADF se as histórias reais
usarem listas ou tabelas.

---

## LLM: `OpenRouterProvider` + `PromptLoader`

Implementa `ILlmProvider`. Cada método: **carrega o prompt** → **interpola** →
**chama o modelo** → **valida a resposta**.

| Método da porta | Prompt | Modo | Validação |
|---|---|---|---|
| `CategorizarAsync(texto)` | `02_categorizar.md` (`{{texto_fonte}}`) | texto | `Trim('"')` + `TentarConverter` → fora do enum = `LlmRespostaInvalidaException` |
| `AgruparSemelhantesAsync(pares)` | `03_agrupar_semelhantes.md` (`{{lista_chave_texto}}`) | JSON | `ValidarGrupos`: objeto `{"grupos": [...]}`, porque o `json_object` da OpenRouter/OpenAI exige objeto na raiz (lista crua segue aceita); lista de listas de **strings**, forma completa; chave omitida vira grupo próprio + `LogWarning` |
| `ReescreverLinguagemNegocioAsync(textos, cat)` | `04_...md` (`{{categoria}}`, `{{textos_fonte}}`) | texto | `Trim()` |
| `GerarTituloEResumoAsync(itens)` | `05_...md` (`{{itens_texto}}`) | JSON | exige `titulo` e `resumo` string |

Internos:

- **`ChamarAsync`** monta `ChatCompletionRequest` (tipo concreto: um
  `Dictionary<string, object>` serializaria o prompt como `{}` em silêncio), com
  `temperature = 0.2`, `models = [principal, ...fallback]` quando houver fallback, e
  `response_format = json_object` no modo JSON.
- **`ParseJson`** remove cerca ```` ```json ```` antes de parsear.
- **`PostAsync`**: erro HTTP → corpo só no log → `LlmApiException("OpenRouter retornou N.")`.

**`PromptLoader`**: único ponto que lê arquivo de prompt. Sobe a partir de
`AppContext.BaseDirectory` até achar `prompts/` (funciona em `bin/Debug`, em teste e no
container), com cache em `ConcurrentDictionary`. `Montar(arquivo, (chave, valor)...)`
substitui `{{chave}}` literalmente ([ADR-005](../../docs/decisoes-arquiteturais.md#adr-005--prompts-versionados-como-arquivo-interpolação-chave)).

---

## Banco: EF Core 10 + PostgreSQL 16

- [`InvoiSysDbContext`](Database/InvoiSysDbContext.cs): 8 `DbSet`s. Mapeamento todo
  em `Configurations/` (`ApplyConfigurationsFromAssembly`). `snake_case` via
  `UseSnakeCaseNamingConvention()`.
- **Configurations**, uma por entidade. Padrões que se repetem:
  - `Id` com `.ValueGeneratedNever()` + default `gen_random_uuid()` ([ADR-010](../../docs/decisoes-arquiteturais.md#adr-010--ids-gerados-no-domínio-com-valuegeneratednever));
  - enums → texto via `ParaValor()` + CHECK constraint com o vocabulário;
  - coleções do agregado mapeadas pelo **campo privado** (`HasField("_versoes")`,
    `PropertyAccessMode.Field`);
  - propriedades derivadas (`TextoFinal`, `ProntaParaExportar`, atalhos da Release)
    com `Ignore`;
  - `text[]` com converter + `ArrayConversionHelper.ListaStringComparer`; índice GIN em
    `itens_comunicado.origens`.
- **Repositories**:

| Classe | Destaque |
|---|---|
| `ReleaseRepository` | `Include` de histórias, versões → itens e execuções (sem lazy loading: faltar Include = lista vazia silenciosa). `SalvarAsync` só faz `Add` se a entidade estiver `Detached`; senão deixa o ChangeTracker gerar UPDATE |
| `HistoriaJiraRepository`, `ExecucaoPipelineRepository` | leitura `AsNoTracking`, sem escrita |
| `ComunicadoExportadoRepository` | append-only: `AdicionarAsync` + consultas |
| `UsuarioRepository` | busca por id/e-mail + `SalvarAsync`, sem delete |
| `DestaqueHeroRepository` | ativos ordenados por `Ordem` |

- **Migrations** (`Database/Migrations/`):

| Migration | Conteúdo |
|---|---|
| `InitialCreate` | 8 tabelas, FKs, índices, CHECKs |
| `TriggersAtualizadoEm` | função `set_atualizado_em()` + trigger em `releases`, `versoes_comunicado`, `itens_comunicado` |
| `ChavesGeradasNoDominio` | só snapshot (`ValueGeneratedNever`), sem SQL |

Modelo completo, tabela por tabela: [docs/modelagem-de-dominio.md](../../docs/modelagem-de-dominio.md).

---

## Por que é assim

**Configuração e transporte**
- `JiraOptions` e `OpenRouterOptions` são a única entrada dessa configuração: nenhuma
  camada lê variável de ambiente direto.
- O Jira Cloud exige Basic Auth com **API token**, não senha.
- `OpenRouter:ModelosFallback` vira o parâmetro `models` da requisição: em 5xx/429 a
  própria OpenRouter tenta o próximo modelo, sem round-trip nosso.
  `OpenRouter:Endpoint` é configurável para apontar para gateway compatível ou servidor
  de teste.
- Retry só em 429, 5xx e falha de transporte. 4xx de cliente (401, 404) sobe na
  primeira tentativa, porque repetir não muda o resultado. O timeout do LLM é bem maior
  que o do Jira porque geração de texto é lenta.
- Sem credencial, o composition root entrega o adapter `*Pendente`: sem `BaseUrl`, o
  `HttpClient` estouraria lá dentro e o usuário receberia um 500 opaco, em vez de um 501
  dizendo o que configurar ([ADR-014](../../docs/decisoes-arquiteturais.md#adr-014--adapters-pendente-quando-falta-credencial-501-honesto)).

**Jira**
- `nextPageToken` expira em 7 dias, então não é guardado entre execuções.
- `fields.subtasks` traz só id, chave, título e tipo. O corpo da Release Note exige uma
  segunda chamada por subtarefa, custo aceitável no volume de uma Release (dezenas de
  issues).

**OpenRouter**
- `temperature = 0.2`: no pipeline queremos consistência, não criatividade.
- A categorização não usa JSON mode (a saída é uma palavra), e alguns modelos devolvem
  a palavra entre aspas; por isso o `Trim('"')`.
- Alguns modelos atrás do gateway ignoram `response_format` e cercam o JSON com
  ```` ```json ````; por isso o `ParseJson` remove a cerca antes de parsear.
- `ValidarGrupos` valida a forma completa: um `[["INV-1", 42]]` passaria numa checagem
  rasa de "é array?" e sujaria `Origens`.

**Banco**
- `snake_case` é o idiomático no PostgreSQL. O C# continua em PascalCase; a convenção só
  afeta o SQL.
- `itens_comunicado.origens` é `text[]` com as chaves do Jira, e não FK ou tabela de
  junção: é o que o domínio expõe, a chave já é validada na origem, e normalizar exigiria
  mudar a assinatura do domínio sem ganho real. O índice GIN cobre "quais itens vieram da
  história X".
- Unique `(release_id, publico)` em `versoes_comunicado`: uma versão viva por público.
  Reprocessar substitui o conteúdo; o histórico do que foi publicado fica em
  `comunicados_exportados`.
- Unique `(release_id, chave)` em `historias_jira`: a mesma issue não se repete dentro
  de uma Release, mas pode aparecer em outra.
- CHECK de motivo obrigatório quando a versão está reprovada: o domínio já bloqueia, e o
  banco garante mesmo para INSERT via SQL direto.
- FK `RESTRICT` de `comunicados_exportados` para a versão: apagar uma versão que já foi
  publicada reescreveria a auditoria. Exportação não tem unique por combinação, porque
  reexportar é evento novo.
- `revisado_por` é texto livre até existir autenticação; depois vira `revisado_por_id`.
- `ArrayConversionHelper.ListaStringComparer`: sem ele o EF compara `text[]` por
  referência e marca a lista como alterada em todo `SaveChanges`.
- A FK de `HistoriaJira` para a Release é shadow property, então o filtro por Release no
  `HistoriaJiraRepository` usa `EF.Property`.

**Repositories**
- Portas de leitura usam `AsNoTracking`: rastrear só gastaria memória e abriria a chance
  de salvar uma filha por fora do agregado. `ReleaseRepository` e `UsuarioRepository`
  rastreiam de propósito, porque o `SalvarAsync` decide INSERT ou UPDATE pelo estado de
  rastreamento.
- `ComunicadoExportadoRepository.AdicionarAsync` sempre faz INSERT: exportação nunca é
  atualizada.
- `UsuarioRepository` compara o e-mail exatamente como gravado. Normalizar caixa é regra
  da autenticação (e teria que valer também no cadastro).
- Padrão para agregado novo: porta em `Domain.Ports`, implementação aqui, registro no
  composition root.

---

## Regras para quem for mexer aqui

- Adapter novo = implementa uma porta do domínio + é registrado **só** no
  `DependencyInjection.cs`.
- Toda chamada HTTP externa passa por `HttpClient` registrado com policy de resiliência.
- Erro externo vira exceção de `Domain.Ports` (nunca `HttpRequestException` crua), sem
  corpo da resposta na mensagem.
- Mudou entidade? `dotnet ef migrations add <Nome>` e revise o SQL gerado. Nada de
  migration destrutiva sem conversa.
- Testes de referência: `OpenRouterProviderTests`, `JiraRestClientAdfTests` (unit, com
  `FakeHttpMessageHandler`), `*RepositoryTests` e `PersistenciaAgregadoReleaseTests`
  (integração, Postgres real).

## O que ainda falta neste módulo

- **Traduzir falha de transporte** (`HttpRequestException`, timeout e circuit breaker do
  Polly) em `JiraApiException`/`LlmApiException` em `JiraRestClient.GetAsync` e
  `OpenRouterProvider.PostAsync`. Hoje só resposta HTTP de erro é traduzida; DNS ou
  conexão recusada vira 500 cru.
- Validar `JiraRestClient` e `OpenRouterProvider` contra as instâncias reais (Fase 5).
- Adapter de renderização/exportação (Markdown; depois HTML/PDF), conforme
  [P-03](../../docs/decisoes-arquiteturais.md#p-03--onde-mora-o-render-da-exportação).
- Hash de senha e emissão de JWT (Fase 3), provavelmente como porta
  (`IHashSenha`, `IEmissorToken`) + adapter aqui.
