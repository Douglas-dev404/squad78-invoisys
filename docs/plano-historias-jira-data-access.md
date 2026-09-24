# Plano: camada de acesso a dados para `historias_jira`

## Contexto

A task original pedia um `IHistoriaJiraRepository`/`HistoriaJiraRepository`, seguindo o
padrão de referência de `IReleaseRepository`/`ReleaseRepository`, mas com uma ressalva
explícita: confirmar antes com o time se esse repository é realmente necessário, dado
que `HistoriaJira` já é carregada/salva como filha do agregado `Release`.

Investigação confirmou que **não é necessário** — e mais que isso, criá-lo
contrariaria uma regra já documentada no próprio código de referência:

- O docstring de `IReleaseRepository`/`ReleaseRepository`
  (`src/InvoiSys.Domain/Ports/IReleaseRepository.cs`,
  `src/InvoiSys.Infrastructure/Database/ReleaseRepository.cs`) diz explicitamente:
  repository é **por agregado, não por entidade filha**. `VersaoComunicado` e
  `ExecucaoPipeline` (filhas do agregado `Release`) já seguem essa regra e não têm
  repository próprio; `HistoriaJira` é a mesma situação.
- `HistoriaJira` é filha 1:N de `Release` (FK shadow `ReleaseId`, cascade delete —
  `ReleaseConfiguration.cs:47-54`, `HistoriaJiraConfiguration.cs:37`) e já é
  carregada integralmente por `ReleaseRepository.ConsultaComAgregadoCompleto()` via
  `.Include(r => r.Historias)`.
- Nenhum código atual (pipeline, endpoints, testes) consulta `HistoriaJira` isolada do
  seu agregado — confirmado por busca em `src/InvoiSys.Application/`,
  `src/InvoiSys.Api/` e `tests-dotnet/`.

**Decisão confirmada: não criar o repository.**

A lacuna real que a investigação revelou, e que se torna a entrega desta task: o
`ReleaseRepository` existente — que já é responsável por toda a persistência/leitura
de `HistoriaJira` como parte do agregado — **nunca teve nenhum teste**
(`grep -rn "ReleaseRepository" tests-dotnet/` não retorna nada). O repositório também
não tem hoje nenhuma convenção de teste de banco (sem EF InMemory, sem SQLite, sem
Testcontainers). Decisão confirmada: cobrir isso com **Testcontainers + Postgres real**
(`postgres:16-alpine`, mesma imagem do `docker-compose.yml`), porque
`HistoriaJira.Labels` é mapeado para `text[]` nativo do Postgres via value converter
customizado (`ArrayConversionHelper`) — um provider fake (InMemory/SQLite) não validaria
esse mapeamento nem as constraints reais (índice único composto, cascade delete).

## Escopo revisado

1. Documentar a decisão de não criar o repository em `AGENTS.md` (seção "Decisões já
   tomadas"), para que a pergunta não seja reaberta numa task futura similar (ex. para
   `ItemComunicado`, outra filha de agregado).
2. Adicionar testes de integração para `ReleaseRepository`, usando Testcontainers,
   cobrindo especificamente o comportamento relacionado a `HistoriaJira`: persistência
   do array `Labels`, índice único composto `(ReleaseId, Chave)`, cascade delete, e
   não-duplicação em updates.
3. `dotnet format` e `dotnet test` passando ao final.

Não há porta nova, não há implementação nova em `src/InvoiSys.Infrastructure/Database/`
além do que já existe, e não há mudança no composition root — os itens do critério de
aceite original que dependiam da criação do repository não se aplicam.

## Implementação

### 1. Dependência de teste

```bash
dotnet add tests-dotnet/InvoiSys.Tests/InvoiSys.Tests.csproj package Testcontainers.PostgreSql
```

Adiciona `Testcontainers.PostgreSql` ao `ItemGroup` de pacotes de
`tests-dotnet/InvoiSys.Tests/InvoiSys.Tests.csproj` (versão resolvida pelo próprio
`dotnet add` como a última estável — não fixar de memória). `Npgsql` já vem transitivo
via `InvoiSys.Infrastructure`.

### 2. `tests-dotnet/InvoiSys.Tests/Integration/PostgresContainerFixture.cs` (novo)

Fixture de collection (`ICollectionFixture` + `[CollectionDefinition("Postgres")]`, não
`IClassFixture`) — evita subir um container novo por classe de teste se outra classe de
teste de banco aparecer no futuro. Sobe `PostgreSqlBuilder().WithImage("postgres:16-alpine")`
(mesma imagem do `docker-compose.yml`), aplica as migrations reais uma única vez em
`InitializeAsync` via `context.Database.MigrateAsync()` (não `EnsureCreatedAsync()` —
senão os triggers de `TriggersAtualizadoEm` não seriam exercitados). Expõe
`CriarContexto()`, que devolve um `InvoiSysDbContext` **novo** a cada chamada,
replicando a config de produção de `DependencyInjection.cs`:
`UseNpgsql(connectionString).UseSnakeCaseNamingConvention()`.

```csharp
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("invoisys_test")
        .WithUsername("invoisys")
        .WithPassword("invoisys")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public InvoiSysDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<InvoiSysDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options);
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;
```

### 3. `tests-dotnet/InvoiSys.Tests/Integration/ReleaseRepositoryTests.cs` (novo)

`[Collection("Postgres")]`, construtor recebe `PostgresContainerFixture`. Cada teste usa
um `fixture.CriarContexto()` para escrever e **outro novo** para reler — força ida real
ao banco em vez de servir do change tracker em memória. Helper privado de dados, no
mesmo estilo de `ReleaseInvariantesTests.UmaHistoria`:

```csharp
private static HistoriaJira UmaHistoria(
    string chave = "INV-1",
    string? textoReleaseNote = null,
    IReadOnlyList<string>? labels = null) => new()
{
    Chave = chave,
    Titulo = "Título",
    DescricaoTecnica = "Descrição técnica",
    TipoIssue = "Story",
    TextoReleaseNote = textoReleaseNote,
    Labels = labels ?? [],
};

private static string ChaveJiraUnica() => $"RELEASE-TESTE-{Guid.NewGuid():N}";
```

`ChaveJiraUnica()` evita colisão com o índice único global em `Release.ChaveJira`
(`ReleaseConfiguration.cs:34`) entre execuções de teste — não é necessário
Respawn/reset de banco, cada teste opera sobre dados que ele mesmo cria e filtra pelo
próprio `Id`/`ChaveJira`.

Casos de teste (nomes em português, padrão do repo):

1. **`SalvarAsync_persiste_release_com_historias_e_releituraPorId_traz_tudo_de_volta`**
   — salva `Release` com 2 histórias (uma com `Labels = ["backend", "urgente"]` e
   `TextoReleaseNote` preenchido, outra com `Labels = []` e `TextoReleaseNote = null`);
   relê via `BuscarPorIdAsync` num contexto novo; assert de todos os campos, com
   destaque para `Labels.Should().BeEquivalentTo(...)` — é o assert que de fato valida
   o mapeamento `text[]`.
2. **`BuscarPorChaveJiraAsync_encontra_pela_chave_de_negocio_e_carrega_historias`**.
3. **`BuscarPorIdAsync_e_BuscarPorChaveJiraAsync_devolvem_null_quando_nao_existe`**.
4. **`SalvarAsync_com_chave_de_historia_duplicada_na_mesma_release_lanca_DbUpdateException`**
   — duas `HistoriaJira` com `Chave = "INV-1"` na mesma `Release`; assert
   `await acao.Should().ThrowAsync<DbUpdateException>()` — valida o índice único
   composto `(ReleaseId, Chave)`.
5. **`SalvarAsync_permite_a_mesma_chave_de_historia_em_releases_diferentes`** — prova
   que o índice é composto, não global em `Chave`.
6. **`Remover_release_apaga_historias_filhas_em_cascata`** — carrega a Release, captura
   os `Id` das histórias, `contexto.Releases.Remove(release)` +
   `SaveChangesAsync()`; num contexto novo, assert que nenhum desses `Id` existe mais em
   `contexto.HistoriasJira`. Comentário no teste explicando que o `Remove` direto no
   `DbContext` é intencional (a porta não expõe delete) — o alvo do teste é a
   configuração `OnDelete(DeleteBehavior.Cascade)`, não o repository.
7. **`SalvarAsync_sobre_release_existente_nao_duplica_historias_ja_persistidas`** —
   salva, relê, chama `release.MarcarProcessando()`, salva de novo (branch de update);
   relê de novo e assert que a contagem de `Historias` não mudou e `Status` foi
   persistido.

### 4. `AGENTS.md` — dois bullets novos em "Decisões já tomadas"

```markdown
- **Sem `IHistoriaJiraRepository`/`HistoriaJiraRepository` dedicado.** Avaliado e
  descartado: `HistoriaJira` é filha do agregado `Release` (1:N, FK shadow
  `ReleaseId`, cascade delete — ver `HistoriaJiraConfiguration.cs`/
  `ReleaseConfiguration.cs`) e já é persistida/carregada integralmente por
  `ReleaseRepository.ConsultaComAgregadoCompleto()` via `.Include(r => r.Historias)`.
  Repository é por agregado, não por entidade filha — regra documentada no
  docstring de `src/InvoiSys.Domain/Ports/IReleaseRepository.cs`. Se algum código
  precisar de fato consultar `HistoriaJira` isolada da sua `Release`, isso é motivo
  novo para reabrir esta decisão.
- **Testes de banco usam Testcontainers com Postgres real** (`postgres:16-alpine`,
  mesma imagem do `docker-compose.yml`), não EF InMemory nem SQLite — o mapeamento de
  `text[]` de `HistoriaJira.Labels` via value converter customizado
  (`ArrayConversionHelper`) não seria validado fielmente por um provider fake. Ver
  `tests-dotnet/InvoiSys.Tests/Integration/PostgresContainerFixture.cs`.
```

## Arquivos

- `tests-dotnet/InvoiSys.Tests/InvoiSys.Tests.csproj` — editar (novo `PackageReference`)
- `tests-dotnet/InvoiSys.Tests/Integration/PostgresContainerFixture.cs` — novo
- `tests-dotnet/InvoiSys.Tests/Integration/ReleaseRepositoryTests.cs` — novo
- `AGENTS.md` — editar (dois bullets em "Decisões já tomadas")
- Não editar: `IReleaseRepository.cs`, `ReleaseRepository.cs`, `DependencyInjection.cs`
  (nenhuma porta/implementação/registro novo)

## Verificação

1. `dotnet test InvoiSys.slnx --filter "FullyQualifiedName~ReleaseRepositoryTests"` —
   todos os 7 casos passando (exige Docker disponível localmente; primeira execução
   baixa a imagem `postgres:16-alpine`).
2. `dotnet format InvoiSys.slnx` — sem alterações pendentes.
3. `dotnet test InvoiSys.slnx` — suíte completa (unit + integration existentes + os
   novos) passando, sem regressão nos testes já existentes.
4. Revisar que `AGENTS.md` ficou coerente com o restante da seção (mesmo estilo de
   bullet, referências a arquivo corretas).

## Riscos conhecidos (comunicar no PR)

- CI (`ubuntu-latest`) já tem Docker disponível — Testcontainers funciona sem setup
  extra, mas a primeira execução baixa a imagem, aumentando o tempo do job.
- Ambiente sem socket Docker acessível fará esses testes falharem por erro de conexão
  Docker — limitação inerente a Testcontainers, não bug da implementação.

## Nota de ambiente

O `dotnet` CLI não está disponível no ambiente onde este plano foi gerado — a
implementação e a verificação (`dotnet add`, `dotnet format`, `dotnet test`) precisam
rodar numa máquina/ambiente com .NET SDK 10 e Docker instalados.
