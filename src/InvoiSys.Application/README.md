# InvoiSys.Application — casos de uso

Orquestra o domínio e as portas para executar os casos de uso. **Depende só de
`InvoiSys.Domain`**: não conhece EF Core, `HttpClient` nem ASP.NET. Os adapters
concretos chegam por injeção de dependência.

```
InvoiSys.Application/
├── Pipeline/
│   └── PipelineGeracaoReleaseNote.cs   # o caso de uso central
└── DependencyInjection.cs              # AddApplication(): registra os casos de uso
```

---

## `PipelineGeracaoReleaseNote`

Único caso de uso hoje. Recebe a chave de uma Release e devolve o agregado `Release` com
a versão Cliente gerada e pronta para **revisão** (nunca publicada direto).

**Dependências (construtor):** `IJiraClient`, `ILlmProvider`, `IReleaseRepository` e
`string? modeloLlm` (rótulo do modelo para o log de execução).

### `ExecutarAsync(chaveRelease)`: o que acontece, em ordem

```
1. historias = IJiraClient.BuscarH```
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
``` que faz | Por que é assim |
|---|---|---|
| `ExtrairELimpar(historia)` (`internal static`) | Trim do título e colapso de espaços/quebras **no campo que `TextoFonte` vai usar** (Release Note, se houver; senão descrição técnica) | Limpar sempre a descrição técnica seria bug: com Release Note, `TextoFonte` devolveria o texto sujo |
| `Normalizar(texto)` | `Split` por qualquer whitespace + `Join(' ')` | Determinístico e grátis. Não tem por que gastar token nisso |
| `ProcessarGruposAsync(...)` | Indexa histórias por chave (`TryAdd`: duplicata de paginação não derruba) e gera um item por grupo | Protege contra chave alucinada e chave duplicada ([ADR-017](../../docs/decisoes-arquiteturais.md#adr-017--desconfiança-estruturada-da-saída-do-llm)) |

### Registro na DI

`AddApplication(configuration)` registra o pipeline como **Scoped** (as portas que ele
usa são scoped; singleton capturaria um escopo encerrado) e lê `OpenRouter:Modelo` da
configuração em vez de `IOptions<OpenRouterOptions>`, porque essa classe vive na
infraestrutura, que a Application não referencia.

---

## Como testar

`tests-dotnet/InvoiSys.Tests/Unit/PipelineGeracaoReleaseNoteTests.cs` é a referência:
`FakeJiraClient` + `FakeLlmProvider` configuráveis (grupos fixos, categoria fixa, falha
forçada). Sem rede, sem token, determinístico.

## O que ainda falta neste módulo

| Caso de uso | Issue | Observação |
|---|---|---|
| Consultar e listar Releases | #21 | pode ser direto do repository no endpoint, ou um serviço de consulta |
| Revisar (aprovar/reprovar/reabrir por público) | #22 | carrega Release → método do domínio → `SalvarAsync` |
| Exportar Markdown | #24 | ver [P-03](../../docs/decisoes-arquiteturais.md#p-03--onde-mora-o-render-da-exportação) |
| Gerar versões Comercial/Suporte/Interno | Fase 6 | chamar `ConcluirProcessamento(..., publico)` por público, com prompt por público |
| Paralelizar o loop por grupo | opcional | só se a latência de Releases grandes incomodar |
