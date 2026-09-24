# Checklist: camada de acesso a dados para `historias_jira`

Plano completo em [plano-historias-jira-data-access.md](plano-historias-jira-data-access.md).

> ⚠️ Requer .NET SDK 10 e Docker instalados (Testcontainers precisa de um daemon
> Docker acessível). Não executável no ambiente onde este checklist foi gerado.

## 0. Decisão de arquitetura (já confirmada, não reabrir)

- [x] Confirmado: **não** criar `IHistoriaJiraRepository`/`HistoriaJiraRepository` —
      `HistoriaJira` é filha do agregado `Release`, já coberta por
      `ReleaseRepository.ConsultaComAgregadoCompleto()`.
- [x] Confirmado: cobrir a lacuna real (zero testes em `ReleaseRepository`) com
      Testcontainers + Postgres real, não EF InMemory/SQLite.

## 1. Dependência de teste

- [ ] Rodar `dotnet add tests-dotnet/InvoiSys.Tests/InvoiSys.Tests.csproj package Testcontainers.PostgreSql`
- [ ] Confirmar que `tests-dotnet/InvoiSys.Tests/InvoiSys.Tests.csproj` ganhou a nova
      `<PackageReference>` (versão decidida pelo próprio comando, não fixar de memória)

## 2. Fixture do container Postgres

- [ ] Criar `tests-dotnet/InvoiSys.Tests/Integration/PostgresContainerFixture.cs`
  - [ ] `PostgreSqlBuilder().WithImage("postgres:16-alpine")` (mesma imagem do
        `docker-compose.yml`)
  - [ ] `IAsyncLifetime.InitializeAsync()` sobe o container e aplica migrations reais
        via `context.Database.MigrateAsync()` (não `EnsureCreatedAsync()`)
  - [ ] `IAsyncLifetime.DisposeAsync()` derruba o container
  - [ ] Método `CriarContexto()` devolve um `InvoiSysDbContext` **novo** por chamada,
        replicando a config de produção: `UseNpgsql(...).UseSnakeCaseNamingConvention()`
  - [ ] `[CollectionDefinition("Postgres")]` + `ICollectionFixture<PostgresContainerFixture>`
        (não `IClassFixture` direto — permite compartilhar o container se surgir outra
        classe de teste de banco no futuro)

## 3. Testes de `ReleaseRepository`

- [ ] Criar `tests-dotnet/InvoiSys.Tests/Integration/ReleaseRepositoryTests.cs`
  - [ ] `[Collection("Postgres")]`, construtor recebe `PostgresContainerFixture`
  - [ ] Helper privado `UmaHistoria(...)` (mesmo estilo de `ReleaseInvariantesTests.UmaHistoria`)
  - [ ] Helper privado `ChaveJiraUnica()` (`$"RELEASE-TESTE-{Guid.NewGuid():N}"`) para
        evitar colisão com o índice único global em `Release.ChaveJira`

- [ ] **Teste 1** — `SalvarAsync_persiste_release_com_historias_e_releituraPorId_traz_tudo_de_volta`
  - Salvar Release com 2 histórias (uma com `Labels = ["backend","urgente"]` +
    `TextoReleaseNote` preenchido; outra com `Labels = []` + `TextoReleaseNote = null`)
  - Reler com `BuscarPorIdAsync` num contexto **novo**
  - Assert todos os campos, incluindo `Labels.Should().BeEquivalentTo(...)`
  - *(implementar este primeiro — valida a fixture antes dos demais)*

- [ ] **Teste 2** — `BuscarPorChaveJiraAsync_encontra_pela_chave_de_negocio_e_carrega_historias`

- [ ] **Teste 3** — `BuscarPorIdAsync_e_BuscarPorChaveJiraAsync_devolvem_null_quando_nao_existe`

- [ ] **Teste 4** — `SalvarAsync_com_chave_de_historia_duplicada_na_mesma_release_lanca_DbUpdateException`
  - Duas `HistoriaJira` com `Chave = "INV-1"` na mesma Release
  - `await acao.Should().ThrowAsync<DbUpdateException>()`

- [ ] **Teste 5** — `SalvarAsync_permite_a_mesma_chave_de_historia_em_releases_diferentes`
  - Prova que o índice único é composto `(ReleaseId, Chave)`, não global em `Chave`

- [ ] **Teste 6** — `Remover_release_apaga_historias_filhas_em_cascata`
  - Capturar `Id`s das histórias antes do delete
  - `contexto.Releases.Remove(release)` + `SaveChangesAsync()`
  - Assert que nenhum desses `Id` existe mais em `contexto.HistoriasJira`
  - Comentar no teste: `Remove` direto é intencional (porta não expõe delete); o alvo
    é validar `OnDelete(DeleteBehavior.Cascade)`

- [ ] **Teste 7** — `SalvarAsync_sobre_release_existente_nao_duplica_historias_ja_persistidas`
  - Salvar → reler → `release.MarcarProcessando()` → salvar de novo (branch update)
  - Reler de novo: contagem de `Historias` igual, `Status` persistido

- [ ] Rodar isolado durante o desenvolvimento:
      `dotnet test InvoiSys.slnx --filter "FullyQualifiedName~ReleaseRepositoryTests"`

## 4. Documentar a decisão em `AGENTS.md`

- [ ] Adicionar bullet "Sem `IHistoriaJiraRepository`/`HistoriaJiraRepository` dedicado"
      na seção "Decisões já tomadas — não reabrir sem motivo novo"
- [ ] Adicionar bullet "Testes de banco usam Testcontainers com Postgres real"
      na mesma seção
- [ ] Texto exato de ambos os bullets está no plano (seção 4)

## 5. Verificação final

- [ ] `dotnet test InvoiSys.slnx --filter "FullyQualifiedName~ReleaseRepositoryTests"` — 7/7 passando
- [ ] `dotnet format InvoiSys.slnx` — sem alterações pendentes
- [ ] `dotnet test InvoiSys.slnx` — suíte completa (unit + integration) passando, sem regressão
- [ ] Revisar `AGENTS.md` — bullets coerentes em estilo/tom com o resto da seção

## 6. Antes de abrir o PR

- [ ] Confirmar que nenhuma porta/implementação/registro de `HistoriaJira` foi criada
      (`IReleaseRepository.cs`, `ReleaseRepository.cs`, `DependencyInjection.cs`
      permanecem intocados)
- [ ] Descrição do PR explica a decisão "não criar repository" (linkar
      `docs/plano-historias-jira-data-access.md` ou copiar o resumo do Contexto)
- [ ] Mencionar no PR: primeira execução em CI baixa a imagem `postgres:16-alpine`
      (tempo de job aumenta um pouco, é esperado)
