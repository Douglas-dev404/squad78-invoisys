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
   entre todas as classes de banco; as migrations reais são aplicadas uma vez.

---

## Mapa dos testes

### Unit

| Arquivo | Cobre |
|---|---|
| `ReleaseInvariantesTests` | Gate de aprovação da Release, múltiplos públicos, status agregado |
| `VersaoComunicadoTests` | Transições `Aprovar`/`Reprovar`/`Reabrir`, itens excluídos, `ProntaParaExportar`, bloqueio de `ComunicadoExportado` |
| `HistoriaJiraTests` | Prioridade da Release Note sobre a descrição técnica (`TextoFonte`) |
| `PipelineGeracaoReleaseNoteTests` | Orquestração dos 5 estágios, limpeza, chave alucinada, falha marca Release/execução |
| `OpenRouterProviderTests` | Corpo da request, fallback de modelos, JSON em code fence, validação de grupos, categoria inválida, erros HTTP → exceção de domínio |
| `JiraRestClientAdfTests` | Achatamento de ADF em texto |

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
| `OpenApiTests` | O spec publica as rotas, os schemas dos DTOs e as respostas de erro |

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

- Endpoints de revisão e exportação (#21–#24), quando existirem.
- Frontend: não há testes automatizados ainda. O CI roda só lint + build. Candidatos:
  Vitest + Testing Library para hooks/serviços e Playwright para o fluxo de revisão.
