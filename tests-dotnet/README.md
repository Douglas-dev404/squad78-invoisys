# Testes — InvoiSys.Tests

xUnit + FluentAssertions. Duas camadas de teste, com filosofias diferentes:

| Pasta | O que testa | Dependências externas |
|---|---|---|
| [`Unit/`](InvoiSys.Tests/Unit/) | Domínio, pipeline e adapters HTTP | **nenhuma**: fakes das portas e `FakeHttpMessageHandler` |
| [`Integration/`](InvoiSys.Tests/Integration/) | Repositories, persistência do agregado, API ponta a ponta, OpenAPI | **Docker** (Postgres real via Testcontainers) para os testes de banco |
| [`Fakes/`](InvoiSys.Tests/Fakes/) | Implementações falsas das portas | — |

```bash
dotnet test InvoiSys.slnx                                   # tudo (Docker precisa estar rodando)
dotnet test InvoiSys.slnx --filter "FullyQualifiedName~Unit" # só unitários, sem Docker
```

### Vendo o EF Core persistir

`PipelineGeracaoReleaseNotePersistenciaTests` imprime cada comando SQL que o EF Core
manda ao Postgres (com os valores), separado por etapas. Aparece com verbosidade
detalhada:

```bash
dotnet test InvoiSys.slnx --filter "FullyQualifiedName~PipelineGeracaoReleaseNotePersistencia" --logger "console;verbosity=detailed"
```

O teste `Versao_aprovada_so_volta_a_ser_gerada_depois_de_reaberta` mostra o ciclo
completo: INSERT no processamento → UPDATE na aprovação → nenhuma escrita no
reprocessamento recusado → UPDATE ao reabrir → DELETE + INSERT ao reprocessar.

---

## Princípios

1. **Fakes das portas, não mocks de biblioteca.** `FakeJiraClient` e `FakeLlmProvider`
   implementam as interfaces do domínio com comportamento configurável. É a prova prática
   de que o pipeline depende só das portas.
2. **Banco real, não fake** ([ADR-012](../docs/decisoes-arquiteturais.md#adr-012--testes-de-banco-com-testcontainers-e-postgres-real)).
   `text[]`, CHECKs, triggers e o bug de `ValueGeneratedNever` só aparecem num Postgres de
   verdade.
3. **Releitura sempre por contexto novo.** `PostgresContainerFixture.CriarContexto()` dá um
   `DbContext` limpo, para a asserção ir ao banco e não ao change tracker.
4. **Um container por execução.** `[Collection("Postgres")]` compartilha o mesmo Postgres
   entre todas as classes de banco; as migrations reais são aplicadas uma vez (e não
   `EnsureCreated`, que ignoraria migrations e triggers).
5. **Cada teste só olha os dados que ele mesmo criou.** Como o banco é compartilhado e não
   é resetado, chaves com índice único global (`ChaveJira`, e-mail) levam sufixo aleatório
   (`ChaveJiraUnica()`), e as asserções filtram pelo que o teste semeou.
6. **Dados vão pelo caminho de produção.** Os testes das portas de leitura semeiam via
   `ReleaseRepository` / métodos do agregado, nunca por INSERT direto. A exceção é o teste
   de cascade em `ReleaseRepositoryTests`, que usa `Remove` no `DbContext` de propósito: o
   alvo ali é o mapeamento, e o repository não tem delete.

---

## Mapa dos testes

### Unit

| Arquivo | Cobre |
|---|---|
| `RevisaoDeItensTests` | Editar/excluir/reincluir pela Release, item de versão aprovada bloqueado ([ADR-021](../docs/decisoes-arquiteturais.md#adr-021--item-de-versão-aprovada-não-é-editado-sem-reabrir)) |
| `ReleaseInvariantesTests` | Gate de aprovação da Release, múltiplos públicos, status agregado |
| `VersaoComunicadoTests` | Transições `Aprovar`/`Reprovar`/`Reabrir`, itens excluídos, `ProntaParaExportar`, bloqueio de `ComunicadoExportado` |
| `HistoriaJiraTests` | Prioridade da Release Note sobre a descrição técnica (`TextoFonte`) |
| `PipelineGeracaoReleaseNoteTests` | Orquestração dos 5 estágios, limpeza, chave alucinada, falha marca Release/execução |
| `OpenRouterProviderTests` | Foco no parsing defensivo (o que quebra em produção é resposta fora do formato). Corpo da request, fallback de modelos, JSON em code fence, validação de grupos, categoria inválida, erros HTTP → exceção de domínio |
| `JiraRestClientAdfTests` | Achatamento de ADF em texto. Isolado porque, se falhar, o pipeline recebe texto vazio e o comunicado sai oco sem erro nenhum |

### Integration

| Arquivo | Cobre |
|---|---|
| `ReleaseRepositoryTests` | Round-trip do agregado, busca por id/chave, chave de história única por Release (mas repetível entre Releases), cascade delete, salvar de novo não duplica histórias |
| `PipelineGeracaoReleaseNotePersistenciaTests` | Pipeline gravando de verdade: sobrevive a restart, reprocessar não duplica, falha persiste a execução, versão aprovada só é reprocessada depois de reaberta |
| `PersistenciaAgregadoReleaseTests` | Ciclo processar → revisar → aprovar → reprocessar, adicionando filhos a Release já persistida (regressão do [ADR-010](../docs/decisoes-arquiteturais.md#adr-010--ids-gerados-no-domínio-com-valuegeneratednever)) |
| `HistoriaJiraRepositoryTests`, `ExecucaoPipelineRepositoryTests` | Portas somente leitura: consulta, ordenação, sem tracking |
| `ComunicadoExportadoRepositoryTests` | Cada reexportação vira linha nova, filtro por público, apagar versão já exportada é barrado pelo banco (`RESTRICT`) |
| `UsuarioRepositoryTests` | Busca por id/e-mail, salvar de novo atualiza sem duplicar, e-mail repetido viola o unique |
| `DestaqueHeroRepositoryTests` | Só ativos, ordenados |
| `ApiReleasesTests` | Endpoints via `WebApplicationFactory<Program>` com as portas trocadas por fakes; 200/501/502 |
| `ApiRevisaoTests` | Contrato HTTP de aprovar/reprovar/reabrir: 204/404/409/422, ProblemDetails, efeito na Release (as transições em si são cobertas no domínio) |
| `ApiItensTests` | Contrato HTTP de editar/excluir/reincluir item (a persistência real fica em `PersistenciaAgregadoReleaseTests`) |
| `OpenApiTests` | O spec publica as rotas, os schemas dos DTOs e as respostas de erro. A documentação é entregável, e a falha aqui é silenciosa: sem `.Produces<T>()` o handler funciona, mas o spec sai com "200" e nenhum schema |

---

## Escrevendo um teste novo

- **Regra de domínio?** Teste unitário direto na entidade, sem fake nenhum.
- **Orquestração?** `PipelineGeracaoReleaseNoteTests` é o modelo: configure o
  `FakeLlmProvider` (`GruposFixos`, `CategoriaFixa`, `FalhaAoChamar`) e verifique o
  agregado resultante.
- **Adapter HTTP?** `FakeHttpMessageHandler` + `HttpClient`, verificando o request
  enviado e a tradução da resposta.
- **Persistência?** Classe com `[Collection("Postgres")]`, receba `PostgresContainerFixture`
  no construtor, salve com um contexto e releia com **outro**.
- **Endpoint?** Siga `ApiReleasesTests`: `WebApplicationFactory` + `ConfigureTestServices`
  substituindo as portas.

## O que ainda falta cobrir

- Endpoints de consulta e exportação (#21, #24), quando entrarem.
- Frontend: não há testes automatizados ainda. O CI roda só lint + build. Candidatos:
  Vitest + Testing Library para hooks/serviços e Playwright para o fluxo de revisão.
