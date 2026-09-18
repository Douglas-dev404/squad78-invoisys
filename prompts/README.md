# Prompts do pipeline de IA

Cada estágio do pipeline (ver documentação interna de Regras de Negócio,
projeto `InvoiSys`) tem um arquivo de prompt versionado aqui — nunca hardcoded como
string dentro do código C#. Isso permite:

- Revisar/ajustar tom de voz sem tocar em código.
- Versionar mudança de prompt separada de mudança de lógica (diff limpo).
- Trocar de LLM provider sem reescrever o prompt em si (o adapter em
  `InvoiSys.Infrastructure/Llm/` carrega o arquivo, não o contrário).

## Estágios (ver `src/InvoiSys.Domain/Ports/ILlmProvider.cs` para o contrato de cada um)

| Arquivo | Estágio | Chamado por |
|---|---|---|
| `02_categorizar.md` | Categorização | `LLMProvider.categorizar()` |
| `03_agrupar_semelhantes.md` | Agrupamento semântico | `LLMProvider.agrupar_semelhantes()` |
| `04_reescrever_linguagem_negocio.md` | Reescrita em linguagem de negócio | `LLMProvider.reescrever_linguagem_negocio()` |
| `05_gerar_titulo_resumo.md` | Título + resumo executivo | `LLMProvider.gerar_titulo_e_resumo()` |

Estágio 1 (Extração e Limpeza) é normalização determinística de texto — não chama LLM,
não tem prompt (ver `PipelineGeracaoReleaseNote._extrair_e_limpar`).

## Convenção de formato

Cada arquivo tem: contexto/persona, a tarefa, o formato de saída esperado (sempre
JSON estrito quando a saída é estruturada), e few-shot examples reais da InvoiSys
quando disponíveis. Placeholders de interpolação usam `{{chaves_assim}}` (estilo
Mustache/Jinja) — **nunca** `{chave_assim}` de chave única, porque colide com chaves
JSON literais nos exemplos de formato de saída (`str.format()` interpretaria
`{"titulo": ...}` como um placeholder). A interpolação é feita via `str.replace()`
simples em `src/InvoiSys.Infrastructure/Llm/PromptLoader.cs`, não interpolação nativa.

## Pendência

Few-shot examples reais da InvoiSys ainda não existem — os arquivos abaixo têm
exemplos genéricos de DF-e/fiscal como placeholder. Substituir por exemplos reais
assim que tivermos acesso ao Jira e a Releases publicadas anteriormente.
