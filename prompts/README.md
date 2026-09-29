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
| [`03_agrupar_semelhantes.md`](03_agrupar_semelhantes.md) | Agrupamento semântico | `AgruparSemelhantesAsync` | `{{lista_chave_texto}}` (JSON `[[chave, texto], ...]`) | JSON: lista de listas de chaves; cada chave em exatamente um grupo |
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
- **Prompts por público** (Comercial, Suporte, Interno): diferencial da Fase 6.
  Provavelmente variações do `04` e do `05` por público.
