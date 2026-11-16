# Prompts do pipeline de IA

Cada estágio do pipeline que chama o LLM tem um prompt aqui, **versionado como arquivo**,
nunca como string no C# ([ADR-005](../docs/decisoes-arquiteturais.md#adr-005--prompts-versionados-como-arquivo-interpolação-chave)).
Isso permite:

- ajustar tom de voz e exemplos sem tocar em código;
- revisar mudança de prompt como diff de texto, separado de mudança de lógica;
- trocar de provider de LLM sem reescrever prompt (o adapter carrega o arquivo, não o
  contrário).

## Estágios

Contrato de cada método em [`ILlmProvider`](../src/InvoiSys.Domain/Ports/ILlmProvider.cs);
chamadas em [`OpenRouterProvider`](../src/InvoiSys.Infrastructure/Llm/OpenRouterProvider.cs).

| Arquivo | Estágio | Método | Placeholders | Saída esperada |
|---|---|---|---|---|
| [`02_categorizar.md`](02_categorizar.md) | Categorização | `CategorizarAsync` | `{{texto_fonte}}` | só o valor: `nova_funcionalidade` \| `melhoria` \| `correcao` \| `outros` |
| [`03_agrupar_semelhantes.md`](03_agrupar_semelhantes.md) | Agrupamento semântico | `AgruparSemelhantesAsync` | `{{lista_chave_texto}}` (JSON `[[chave, texto], ...]`) | JSON: objeto `{"grupos": [[chave, ...], ...]}` (o JSON mode exige objeto na raiz); cada chave em exatamente um grupo |
| [`04_reescrever_linguagem_negocio.md`](04_reescrever_linguagem_negocio.md) | Reescrita | `ReescreverLinguagemNegocioAsync` | `{{categoria}}`, `{{textos_fonte}}` | um parágrafo em linguagem de negócio |
| [`05_gerar_titulo_resumo.md`](05_gerar_titulo_resumo.md) | Título e resumo | `GerarTituloEResumoAsync` | `{{itens_texto}}` | JSON: `{"titulo": "...", "resumo": "..."}` |

O estágio 1 (extração e limpeza) é normalização determinística em C#
(`PipelineGeracaoReleaseNote.ExtrairELimpar`). Não chama LLM e não tem prompt.

## Convenções

- **Estrutura de cada arquivo:** persona → regras → entrada → tarefa → formato de saída →
  few-shot.
- **Placeholders `{{chave}}`**, substituídos por `string.Replace` literal em
  [`PromptLoader.Montar`](../src/InvoiSys.Infrastructure/Llm/PromptLoader.cs). **Nunca**
  `{chave}` com chave simples: colide com o JSON literal dos exemplos de saída. Esse bug
  já aconteceu uma vez.
- **Saída estruturada** pede JSON estrito. O adapter liga `response_format: json_object`
  nesses estágios e mesmo assim extrai o JSON de dentro de ```` ```json ```` se o modelo
  ignorar.
- **Categorias** listadas no prompt 02 são as mesmas do enum `CategoriaAlteracao`. Mudar
  uma sem mudar a outra quebra o parser ([ADR-018](../docs/decisoes-arquiteturais.md#adr-018--categorias-e-públicos-fixos)).
- Renomear arquivo ou placeholder exige mudar o `OpenRouterProvider` no mesmo PR.

## Como testar uma mudança de prompt

1. Configure `OpenRouter__ApiKey` e o Jira (ver [README principal](../README.md#configuração)).
2. `POST /api/v1/releases/{chave}/processar` numa Release conhecida.
3. Compare com a saída anterior. Hoje a comparação é manual; um dataset de regressão com
   Releases reais é trabalho da Fase 5.

## Pendências

- **Few-shot reais da InvoiSys:** os exemplos atuais são genéricos de DF-e/fiscal.
  Substituir por histórias e comunicados reais assim que houver acesso ao Jira e a
  Releases publicadas.
## Prompts por público (#88)

O `04` e o `05` são a versão **Cliente**. Os outros públicos têm prompt próprio só nos
estágios que mudam de texto conforme o leitor:

| Público | Reescrita | Título e resumo | Tom |
|---|---|---|---|
| `comercial` | [`reescrever.comercial.md`](reescrever.comercial.md) | [`titulo_resumo.comercial.md`](titulo_resumo.comercial.md) | benefícios, linguagem de venda, sem jargão |
| `suporte` | [`reescrever.suporte.md`](reescrever.suporte.md) | [`titulo_resumo.suporte.md`](titulo_resumo.suporte.md) | o que mudou no comportamento, como orientar o cliente |
| `interno` | [`reescrever.interno.md`](reescrever.interno.md) | [`titulo_resumo.interno.md`](titulo_resumo.interno.md) | detalhe técnico e pontos de atenção |

- O adapter escolhe o arquivo por convenção: `reescrever.<publico>.md` e
  `titulo_resumo.<publico>.md` (`ChatCompletionsLlmProvider.ArquivoDoPublico`). Placeholders
  e formato de saída são **os mesmos** do `04` e do `05`.
- **Categorização e agrupamento rodam uma vez** e valem para todos os públicos: não há
  prompt por público para `02` e `03`.
- Público novo exige mudar o enum `PublicoAlvo` (ADR-018) e criar os dois arquivos.
- Quem pede os públicos é `POST /api/v1/releases/{chave}/processar?publicos=cliente,comercial`.
  Sem a query, só `cliente`.
