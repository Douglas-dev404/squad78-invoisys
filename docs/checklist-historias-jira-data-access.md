# Checklist: camada de acesso a dados para `historias_jira`

Plano completo em [plano-historias-jira-data-access.md](plano-historias-jira-data-access.md).

> ⚠️ Requer .NET SDK 10 e Docker instalados (Testcontainers precisa de um daemon
> Docker acessível). Não executável no ambiente onde este checklist foi gerado.

## 0. Decisão de arquitetura (já confirmada, não reabrir)

- [x] ~~Confirmado: **não** criar `IHistoriaJiraRepository`/`HistoriaJiraRepository`~~ —
      **revisto**: a porta foi criada como leitura isolada (ver seção 7).
      `HistoriaJira` segue sendo gravada só via `IReleaseRepository.SalvarAsync`.
- [x] Confirmado: cobrir a lacuna real (zero testes em `ReleaseRepository`) com
      Testcontainers + Postgres real, não EF InMemory/SQLite.

## 1. Dependência de teste

- [x] Rodar `dotnet add tests-dotnet/InvoiSys.Tests/InvoiSys.Tests.csproj package Testcontainers.PostgreSql`
      *(sem `dotnet` CLI na máquina: edição equivalente, com a última versão estável
      resolvida na API do NuGet — 4.15.0)*
- [x] Confirmar que `tests-dotnet/InvoiSys.Tests/InvoiSys.Tests.csproj` ganhou a nova
      `<PackageReference>` (versão decidida pelo próprio comando, não fixar de memória)

- [x] *(descoberto na verificação)* Referenciar `Microsoft.EntityFrameworkCore.Relational`
      10.0.12 no projeto de testes: sem isso, tocar em `DbContext` falha com `CS1705`
      (a Infrastructure compila contra EF Core 10.0.12; o teste só recebia o 10.0.4
      transitivo do Npgsql)

## 2. Fixture do container Postgres

- [x] Criar `tests-dotnet/InvoiSys.Tests/Integration/PostgresContainerFixture.cs`
  - [x] `new PostgreSqlBuilder("postgres:16-alpine")` (mesma imagem do
        `docker-compose.yml`; o construtor sem argumentos é obsoleto na 4.15.0)
  - [x] `IAsyncLifetime.InitializeAsync()` sobe o container e aplica migrations reais
        via `context.Database.MigrateAsync()` (não `EnsureCreatedAsync()`)
  - [x] `IAsyncLifetime.DisposeAsync()` derruba o container
  - [x] Método `CriarContexto()` devolve um `InvoiSysDbContext` **novo** por chamada,
        replicando a config de produção: `UseNpgsql(...).UseSnakeCaseNamingConvention()`
  - [x] `[CollectionDefinition("Postgres")]` + `ICollectionFixture<PostgresContainerFixture>`
        (não `IClassFixture` direto — permite compartilhar o container se surgir outra
        classe de teste de banco no futuro)

## 3. Testes de `ReleaseRepository`

- [x] Criar `tests-dotnet/InvoiSys.Tests/Integration/ReleaseRepositoryTests.cs`
  - [x] `[Collection("Postgres")]`, construtor recebe `PostgresContainerFixture`
  - [x] Helper privado `UmaHistoria(...)` (mesmo estilo de `ReleaseInvariantesTests.UmaHistoria`)
  - [x] Helper privado `ChaveJiraUnica()` (`$"RELEASE-TESTE-{Guid.NewGuid():N}"`) para
        evitar colisão com o índice único global em `Release.ChaveJira`

- [x] **Teste 1** — `SalvarAsync_persiste_release_com_historias_e_a_releitura_traz_tudo_de_volta`
  - Salvar Release com 2 histórias (uma com `Labels = ["backend","urgente"]` +
    `TextoReleaseNote` preenchido; outra com `Labels = []` + `TextoReleaseNote = null`)
  - Reler com `BuscarPorIdAsync` num contexto **novo**
  - Assert todos os campos, incluindo `Labels.Should().BeEquivalentTo(...)`
  - *(implementar este primeiro — valida a fixture antes dos demais)*

- [x] **Teste 2** — `BuscarPorChaveJiraAsync_encontra_pela_chave_de_negocio_e_carrega_as_historias`

- [x] **Teste 3** — `BuscarPorIdAsync_e_BuscarPorChaveJiraAsync_devolvem_null_quando_nao_existe`

- [x] **Teste 4** — `SalvarAsync_com_chave_de_historia_duplicada_na_mesma_release_lanca_DbUpdateException`
  - Duas `HistoriaJira` com `Chave = "INV-1"` na mesma Release
  - `await acao.Should().ThrowAsync<DbUpdateException>()`

- [x] **Teste 5** — `SalvarAsync_permite_a_mesma_chave_de_historia_em_releases_diferentes`
  - Prova que o índice único é composto `(ReleaseId, Chave)`, não global em `Chave`

- [x] **Teste 6** — `Remover_release_apaga_historias_filhas_em_cascata`
  - Capturar `Id`s das histórias antes do delete
  - `contexto.Releases.Remove(release)` + `SaveChangesAsync()`
  - Assert que nenhum desses `Id` existe mais em `contexto.HistoriasJira`
  - Comentar no teste: `Remove` direto é intencional (porta não expõe delete); o alvo
    é validar `OnDelete(DeleteBehavior.Cascade)`

- [x] **Teste 7** — `SalvarAsync_sobre_release_existente_nao_duplica_historias_ja_persistidas`
  - Salvar → reler → `release.MarcarProcessando()` → salvar de novo (branch update)
  - Reler de novo: contagem de `Historias` igual, `Status` persistido

- [x] Rodar isolado durante o desenvolvimento:
      `dotnet test InvoiSys.slnx --filter "FullyQualifiedName~ReleaseRepositoryTests"`

## 4. Documentar a decisão em `AGENTS.md`

- [x] Adicionar bullet "Sem `IHistoriaJiraRepository`/`HistoriaJiraRepository` dedicado"
      na seção "Decisões já tomadas — não reabrir sem motivo novo"
- [x] Adicionar bullet "Testes de banco usam Testcontainers com Postgres real"
      na mesma seção
- [x] Texto exato de ambos os bullets está no plano (seção 4)

## 5. Verificação final

- [x] `dotnet test InvoiSys.slnx --filter "FullyQualifiedName~ReleaseRepositoryTests"` — 7/7 passando
- [x] `dotnet format InvoiSys.slnx` — sem alterações pendentes
      *(`--verify-no-changes`, o mesmo do CI: exit 0)*
- [x] `dotnet test InvoiSys.slnx` — suíte completa (unit + integration) passando, sem regressão
      *(71/71: 64 anteriores + 7 novos)*
- [x] Revisar `AGENTS.md` — bullets coerentes em estilo/tom com o resto da seção

> Verificação rodada com o SDK .NET 10.0.401 em container Docker (a máquina não tem
> `dotnet` instalado), com o socket do Docker montado para o Testcontainers.

## 6. Antes de abrir o PR

- [x] ~~Confirmar que nenhuma porta/implementação/registro de `HistoriaJira` foi criada~~
      — superado pela seção 7. `IReleaseRepository.cs` e `ReleaseRepository.cs`
      permanecem intocados; `DependencyInjection.cs` ganhou uma linha.
- [ ] **Atualizar a descrição do PR**: ela ainda diz que não há repository novo. Deve
      passar a descrever a porta de leitura isolada e a mudança de decisão
- [ ] Mencionar no PR: primeira execução em CI baixa a imagem `postgres:16-alpine`
      (tempo de job aumenta um pouco, é esperado)

## 7. Revisão da decisão: repository de leitura isolada de `HistoriaJira`

- [x] Porta `src/InvoiSys.Domain/Ports/IHistoriaJiraRepository.cs` — somente leitura:
      `BuscarPorIdAsync`, `BuscarPorChaveAsync(releaseId, chave)`, `ListarPorReleaseAsync`
- [x] Implementação `src/InvoiSys.Infrastructure/Database/HistoriaJiraRepository.cs`
      (`AsNoTracking`; filtro por Release via `EF.Property` na shadow FK `ReleaseId`)
- [x] Registro em `DependencyInjection.cs`:
      `services.AddScoped<IHistoriaJiraRepository, HistoriaJiraRepository>();`
- [x] Testes `tests-dotnet/InvoiSys.Tests/Integration/HistoriaJiraRepositoryTests.cs` (7 casos)
- [x] `dotnet format --verify-no-changes` limpo e suíte completa 78/78
      *(71 anteriores + 7 novos; rodada em container Docker)*
- [x] `AGENTS.md` reescrito para refletir a decisão revista
- [ ] Descrição do PR atualizada (ver seção 6)

Decisão de desenho: a porta **não tem escrita**. `HistoriaJira` só entra no agregado pelo
construtor de `Release` e a FK é shadow property; gravar por aqui permitiria persistir
histórias por fora do agregado.

Pendência fora do escopo: os docstrings de `IReleaseRepository`/`ReleaseRepository`
dizem que as filhas do agregado "não ganham repository próprio". Continua valendo para
`VersaoComunicado`/`ExecucaoPipeline`, mas agora há uma exceção de leitura para
`HistoriaJira` — vale ajustar esses textos numa revisão.
