using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Sobe um Postgres 16 real via Testcontainers (mesma imagem do docker-compose) e aplica
/// as migrations reais uma única vez. Cada teste pede um <see cref="InvoiSysDbContext"/>
/// novo via <see cref="CriarContexto"/> — assim a releitura vai de fato ao banco, em vez
/// de ser servida pelo change tracker de um contexto que acabou de salvar.
///
/// Um Postgres real (e não EF InMemory/SQLite) porque o mapeamento de
/// <c>HistoriaJira.Labels</c> para <c>text[]</c> e as constraints (índice único
/// composto, cascade delete) só são validados de verdade pelo banco de produção.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("invoisys_test")
        .WithUsername("invoisys")
        .WithPassword("invoisys")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Migrations reais, o mesmo caminho de produção (Database__MigrarAoIniciar) —
        // EnsureCreatedAsync ignoraria as migrations e os triggers de atualizado_em.
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Novo contexto, sem tracking herdado de chamadas anteriores. Replica a config de
    /// produção de <c>DependencyInjection.AddDatabase</c>: Npgsql + snake_case.
    ///
    /// Com <paramref name="saida"/>, cada comando SQL que o EF Core manda ao banco
    /// (com os valores dos parâmetros) vai para a saída do teste — visível com
    /// <c>--logger "console;verbosity=detailed"</c>. Serve para enxergar a persistência
    /// acontecendo; os dados são fictícios, então logar valores não expõe nada.
    /// </summary>
    public InvoiSysDbContext CriarContexto(ITestOutputHelper? saida = null)
    {
        var opcoes = new DbContextOptionsBuilder<InvoiSysDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention();

        if (saida is not null)
        {
            opcoes
                .LogTo(saida.WriteLine, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
                .EnableSensitiveDataLogging();
        }

        return new(opcoes.Options);
    }
}

/// <summary>
/// Collection (e não IClassFixture): o container é compartilhado por toda classe de teste
/// de banco marcada com <c>[Collection("Postgres")]</c>, em vez de subir um por classe.
/// </summary>
[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;
