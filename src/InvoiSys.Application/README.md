# InvoiSys.Application — casos de uso

Orquestra o domínio e as portas para executar os casos de uso. **Depende só de
`InvoiSys.Domain`**: não conhece EF Core, `HttpClient` nem ASP.NET. Os adapters
concretos chegam por injeção de dependência.

```
InvoiSys.Application/
├── Consulta/
│   └── ConsultaReleases.cs             # listagem (resumo) e detalhe (agregado completo)
├── Pipeline/
│   └── PipelineGeracaoReleaseNote.cs   # o caso de uso central
├── Revisao/
│   └── RevisaoComunicado.cs            # aprovar/reprovar/reabrir + editar/excluir/reincluir item
└── DependencyInjection.cs              # AddApplication(): registra os casos de uso
```

---

## `PipelineGeracaoReleaseNote`

O caso de uso central. Recebe a chave de uma Release e devolve o agregado `Release` com
a versão Cliente gerada e pronta para **revisão** (nunca publicada direto).

**Dependências (construtor):** `IJiraClient`, `ILlmProvider`, `IReleaseRepository` e
`string? modeloLlm` (rótulo do modelo para o log de execução).

### `ExecutarAsync(chaveRelease)`: o que acontece, em ordem

```
1. release = IReleaseRepository.BuscarPorChaveJiraAsync(chave)   ← já existe?
2. release?.GarantirQuePodeReprocessar()        ← versão aprovada? recusa aqui (409),
                                                  sem chamar Jira/LLM (ADR-020)
3. historias = IJiraClient.BuscarHistoriasDaReleaseAsync(chave)
4. release nova → new Release(chave, historias)
   release existente → release.AtualizarHistorias(historias)  (reprocessar não duplica)
5. release.MarcarProcessando()
6. execucao = release.RegistrarExecucao(modeloLlm)        ← rastro, mesmo se falhar
7. try
     a. historiasLimpas = historias.Select(ExtrairELimpar)          [estágio 1, sem LLM]
     b. grupos = AgruparSemelhantesAsync((chave, TextoFonte)[])     [estágio 3]
     c. itens  = ProcessarGruposAsync(...)                           [estágios 2 e 4]
          para cada grupo:
            - descarta chaves que não vieram do Jira (alucinação)
            - pula grupo que ficou vazio
            - categoria = CategorizarAsync(TextoFonte da 1ª história)
            - texto     = ReescreverLinguagemNegocioAsync(textos do grupo, categoria)
            - new ItemComunicado(categoria, texto, chavesConhecidas)
     d. (titulo, resumo) = GerarTituloEResumoAsync(itens.Texto)     [estágio 5]
     e. release.ConcluirProcessamento(itens, titulo, resumo)  → status aguardando_revisao
     f. execucao.MarcarConcluida()
   catch
     release.MarcarFalha(); execucao.MarcarFalha(mensagem)
     IReleaseRepository.SalvarAsync(release)   ← a execução com erro fica no banco
     relança
8. IReleaseRepository.SalvarAsync(release)
9. return release
```

Por que cada passo é assim:

- **1 e 4:** a Release já existente é **reusada**, nunca recriada com `new`. O
  `SalvarAsync` decide INSERT ou UPDATE pelo rastreamento do EF, então reusar a instância
  carregada é o que faz reprocessar virar UPDATE em vez de linha duplicada.
- **2:** a recusa vem antes do Jira e do LLM: não gasta token nem toca no agregado.
- **6:** a execução é registrada antes do `try`, para a rodada deixar rastro mesmo se
  falhar. Esse rastro é o log que o requisito pede.
- **7c:** a categoria vem da 1ª história do grupo, porque as histórias foram agrupadas
  por tratarem do mesmo assunto. Grupo só com chaves inexistentes é omitido: gerar item
  do nada seria pior que não gerar. O caso inverso (chave real que o modelo esqueceu) é
  tratado no adapter, que a devolve como grupo próprio, então nenhuma história se perde.

### Métodos auxiliares

| Método | O que faz | Por que é assim |
|---|---|---|
| `ExtrairELimpar(historia)` (`internal static`) | Trim do título e colapso de espaços/quebras **no campo que `TextoFonte` vai usar** (Release Note, se houver; senão descrição técnica) | Limpar sempre a descrição técnica seria bug: com Release Note, `TextoFonte` devolveria o texto sujo |
| `Normalizar(texto)` | `Split` por qualquer whitespace + `Join(' ')` | Determinístico e grátis. Não tem por que gastar token nisso |
| `ProcessarGruposAsync(...)` | Indexa histórias por chave (`TryAdd`: duplicata de paginação não derruba) e gera um item por grupo | Protege contra chave alucinada e chave duplicada ([ADR-017](../../docs/decisoes-arquiteturais.md#adr-017--desconfiança-estruturada-da-saída-do-llm)) |

### Registro na DI

`AddApplication(configuration)` registra o pipeline como **Scoped** (as portas que ele
usa são scoped; singleton capturaria um escopo encerrado) e lê `OpenRouter:Modelo` da
configuração em vez de `IOptions<OpenRouterOptions>`, porque essa classe vive na
infraestrutura, que a Application não referencia. O modelo é só um rótulo para o log de
execução, mas sem ele a coluna `modelo_llm` ficaria vazia, e é o dado mais útil para
investigar um comunicado ruim.

`AddApplication` fica na Application, e não no composition root da Infrastructure,
porque a Infrastructure não referencia a Application: quem conhece as duas é a API.

---

## `ConsultaReleases`

Só leitura. `ListarAsync(status?)` devolve `ReleaseResumo` (chave, status, criada em) via
`IReleaseRepository.ListarResumosAsync`, projeção leve sem carregar o agregado.
`DetalharAsync(chave)` devolve a `Release` completa ou lança `ReleaseNaoEncontradaException`
(404 na API). Registrado como **Scoped**.

---

## `RevisaoComunicado`

Revisão humana: `AprovarAsync`, `ReprovarAsync`, `ReabrirAsync` (por público) e
`EditarItemAsync`, `ExcluirItemAsync`, `ReincluirItemAsync` (por item). Todas seguem o
mesmo caminho:

```
1. release = IReleaseRepository.BuscarPorChaveJiraAsync(chave)
             ?? throw ReleaseNaoEncontradaException          → 404 na API
2. release.Aprovar / Reprovar / Reabrir(..., publico)        ← regra mora no domínio
   release.EditarItem / ExcluirItem / ReincluirItem(itemId, ...)
3. IReleaseRepository.SalvarAsync(release)
```

As operações de item devolvem `ItemRevisado` (o item + o público da versão dona dele,
via `Release.PublicoDoItem`), o bastante para a API montar a resposta sem refazer a busca.

Nenhuma decisão de negócio aqui: o público chega já convertido e obrigatório (sem
default para Cliente); revisor, motivo e texto de edição são validados pelo domínio.
Registrado como **Scoped** (depende do repository, que é scoped).

---

## Como testar

`tests-dotnet/InvoiSys.Tests/Unit/PipelineGeracaoReleaseNoteTests.cs` é a referência:
`FakeJiraClient` + `FakeLlmProvider` configuráveis (grupos fixos, categoria fixa, falha
forçada). Sem rede, sem token, determinístico.

## O que ainda falta neste módulo

| Caso de uso | Issue | Observação |
|---|---|---|
| ~~Exportar Markdown~~ feito em `Exportacao/` (`ExportacaoComunicado` + `RenderizadorMarkdown`) | #24 | ver [P-03](../../docs/decisoes-arquiteturais.md#p-03--onde-mora-o-render-da-exportação) |
| Gerar versões Comercial/Suporte/Interno | Fase 6 | chamar `ConcluirProcessamento(..., publico)` por público, com prompt por público |
| Paralelizar o loop por grupo | opcional | só se a latência de Releases grandes incomodar |
