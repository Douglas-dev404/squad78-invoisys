using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace InvoiSys.Tests.Integration;

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

        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

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

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;
