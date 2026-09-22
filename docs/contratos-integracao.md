# InvoiSys — Contratos de Integração Externa

Referência única do que foi verificado sobre as duas APIs externas que o InvoiSys
consome (Jira e OpenRouter). Cada ponto aqui tem sua origem: ou foi conferido contra
a documentação oficial em uma data específica, ou ainda é suposição pendente de
validação — as duas coisas são marcadas explicitamente, para nunca virarem a mesma
categoria de confiança.

O código é a implementação; este documento existe porque o comentário no código
sozinho fica espalhado entre `JiraRestClient.cs`, `OpenRouterProvider.cs` e
`AGENTS.md` — aqui está tudo num lugar só, para quando alguém (interno ou da
InvoiSys) perguntar "qual é o contrato real dessas integrações".

---

## Jira Cloud REST API v3

Adapter: `src/InvoiSys.Infrastructure/Jira/JiraRestClient.cs`. Configuração:
`src/InvoiSys.Infrastructure/Configuration/JiraOptions.cs` (`JIRA_BASE_URL`,
`JIRA_EMAIL`, `JIRA_API_TOKEN`).

### Verificado contra a documentação oficial da Atlassian em 2026-08-24

| Ponto | Detalhe |
|---|---|
| Endpoint de busca | `/rest/api/3/search/jql`. O endpoint legado `/rest/api/3/search` **foi removido** pela Atlassian — não é opção, mesmo que apareça em exemplos antigos. |
| Paginação | `nextPageToken` (não mais `startAt`). Cada página devolve `isLast` e, se houver próxima, `nextPageToken`. **Token expira em 7 dias** — o adapter não cacheia entre execuções, sempre pagina do zero. |
| Autenticação | Basic Auth com **email + API token**, nunca senha — é o que a Jira Cloud exige. Header montado uma vez na composition root (`DependencyInjection.cs`). |
| Query usada | JQL `fixVersion = "<chave>"` — busca todas as issues de uma Release pela fixVersion. |
| Campos pedidos | `summary,description,issuetype,labels,subtasks` — `fields.subtasks` retorna só `id/key/summary/issuetype` por padrão; **não** traz o corpo de texto da subtarefa. |
| Corpo da subtarefa Release Note | Exige uma **segunda chamada** por subtarefa candidata: `GET /rest/api/3/issue/{key}?fields=description`. Custa 1 request extra por issue com subtarefa do tipo certo — aceitável no volume de uma Release (dezenas de issues, não milhares). |
| Formato da descrição | Vem em **ADF** (Atlassian Document Format), uma árvore de nós JSON, não texto puro. Uma instância mal configurada pode devolver string direta — o parser trata os dois casos (`ExtrairTextoDescription`). |
| Retry | 429/5xx e falha de transporte são retentados pela policy de resiliência da composition root (3 tentativas, backoff exponencial) — o adapter em si não tem lógica de retry, só traduz HTTP em domínio. |

Fonte primária: documentação oficial da Atlassian (Jira Cloud REST API v3), consultada
em 2026-08-24. Sem link fixo aqui de propósito — a Atlassian reorganiza a doc com
frequência; se precisar reconferir, buscar "Jira Cloud REST API v3 search/jql" direto.

### Pendências reais — não confundir com o que já foi verificado

1. **Nome exato do subtask type "Release Note" não confirmado contra a instância real
   da InvoiSys.** O código usa a constante `TipoIssueReleaseNote = "Release Note"`
   (`JiraRestClient.cs:32`) como suposição — pode ser diferente no Jira real deles
   (ex: "Nota de Release", "Release Notes", etc). **Bloqueador**: sem confirmar isso,
   o pipeline nunca vai encontrar a subtarefa certa, mesmo funcionando tecnicamente.
2. **Nunca testado contra uma instância real do Jira.** Toda a cobertura de teste
   (`JiraRestClientAdfTests.cs`) usa fixtures/fakes. O parsing de ADF só cobre nós de
   texto simples — tabelas, listas e formatação rica em ADF real podem quebrar ou
   perder conteúdo, e isso só aparece testando contra dado real.
3. **Sem fallback de input.** Decisão de projeto (não limitação técnica): se a API do
   Jira estiver fora do ar ou mal configurada, não existe caminho alternativo
   (JSON/CSV colado) — é bloqueador de desenvolvimento, não só de produção.

---

## OpenRouter (LLM provider)

Adapter: `src/InvoiSys.Infrastructure/Llm/OpenRouterProvider.cs`. Configuração:
`src/InvoiSys.Infrastructure/Configuration/OpenRouterOptions.cs`
(`OPENROUTER_API_KEY`, `OPENROUTER_MODEL`, `OPENROUTER_FALLBACK_MODELS` opcional).

### Verificado contra a documentação oficial em 2026-08-24

| Ponto | Detalhe |
|---|---|
| Endpoint | `POST https://openrouter.ai/api/v1/chat/completions` — único endpoint usado. |
| Schema | Compatível com **OpenAI Chat Completions** — por isso não há SDK dedicado, `HttpClient` direto com o mesmo formato de request/response da OpenAI resolve. |
| Saída estruturada | `response_format: {"type": "json_object"}` para os estágios que esperam JSON (agrupamento semântico). O estágio de categorização não usa — a saída esperada é só a palavra da categoria. |
| Identificação de modelo | Formato `"provedor/modelo"`, ex. `openai/gpt-4o-mini`, `anthropic/claude-3.5-sonnet`. |
| Fallback entre modelos | Parâmetro `models` (lista) — se o modelo principal falhar com 5xx/429, a **própria OpenRouter** tenta o próximo da lista, sem round-trip nosso. Configurado via `OPENROUTER_FALLBACK_MODELS` (JSON de lista). Isso é diferente de "múltiplos providers em paralelo", que é decisão explicitamente descartada para o MVP. |
| Erro 429 | Vem com header `Retry-After` e corpo `{"error": {"code", "message", "type"}}`. |

Fonte primária: `openrouter.ai/docs/api_reference/overview`, consultada em 2026-08-24.

### Pendências reais

1. **Nunca validado contra chamada real de LLM em produção.** `OpenRouterProviderTests.cs`
   cobre parsing de resposta com fixtures — o comportamento real do modelo (qualidade
   da categorização, do agrupamento semântico, alucinação de chaves inexistentes) só
   se confirma rodando de verdade. O pipeline já trata alucinação de chave defensivamente
   (`PipelineGeracaoReleaseNote.ProcessarGruposAsync` filtra chaves desconhecidas em vez
   de derrubar a Release), mas isso não substitui validação real.
2. **Parsing de JSON da resposta é defensivo por necessidade**: mesmo pedindo
   `response_format: json_object`, o modelo pode devolver JSON malformado — o adapter
   trata isso (`ParseJson`), mas é sintoma de que o contrato de saída não é 100%
   garantido pelo provider, só fortemente sugerido.

---

## Onde a configuração vive

Nenhuma camada lê variável de ambiente direto — sempre via `IOptions<T>`
(`JiraOptions`, `OpenRouterOptions`), configurado na composition root
(`InvoiSys.Infrastructure/DependencyInjection.cs`). Ver `.env.example` na raiz do
repositório para a lista completa de variáveis e seus nomes exatos.

Sem credencial configurada, os adapters caem para uma implementação "pendente"
(`JiraClientPendente`, `ProviderPendente`) que devolve 501 em vez de estourar exceção
opaca — comportamento esperado em ambiente sem chave, não bug a corrigir trocando o
fallback (ver `AGENTS.md`).
