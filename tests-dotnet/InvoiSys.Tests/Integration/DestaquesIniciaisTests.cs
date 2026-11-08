using FluentAssertions;
using InvoiSys.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class DestaquesIniciaisTests(PostgresContainerFixture fixture)
{
    private static readonly Guid[] Ids =
    [
        Guid.Parse("286921eb-54b7-47a1-a9db-a56e2cc84001"),
        Guid.Parse("286921eb-54b7-47a1-a9db-a56e2cc84002"),
        Guid.Parse("286921eb-54b7-47a1-a9db-a56e2cc84003"),
    ];

    [Fact]
    public async Task Migration_insere_os_tres_destaques_com_os_textos_e_ordem_do_frontend()
    {
        await using var contexto = fixture.CriarContexto();
        var destaques = await contexto.DestaquesHero.AsNoTracking()
            .Where(d => Ids.Contains(d.Id)).OrderBy(d => d.Ordem).ToListAsync();

        destaques.Select(d => (d.Titulo, d.Descricao, d.Icone, d.Ordem, d.Ativo)).Should().Equal(
            ("Gestão automatizada de tasks", "Simplifique sua produtividade.", "task_alt", 1, true),
            ("Release Notes com Inteligência Artificial",
                "Converta histórias do Jira em comunicados claros em poucos segundos.", "auto_awesome", 2, true),
            ("Governança e Aprovação Humana",
                "Revisão obrigatória e controle granular antes de cada publicação oficial.", "verified_user", 3, true));
    }

    [Fact]
    public async Task Reaplicar_SQL_nao_duplica_nem_sobrescreve_destaques_editados()
    {
        await using var contexto = fixture.CriarContexto();
        await using var transacao = await contexto.Database.BeginTransactionAsync();
        var totalAntes = await contexto.DestaquesHero.CountAsync();
        var primeiroId = Ids[0];
        var terceiroId = Ids[2];
        var idPreexistente = Guid.NewGuid();
        await contexto.DestaquesHero.Where(d => d.Id == primeiroId)
            .ExecuteUpdateAsync(campos => campos
                .SetProperty(d => d.Titulo, "Título personalizado")
                .SetProperty(d => d.Ativo, false));
        await contexto.DestaquesHero.Where(d => d.Id == terceiroId)
            .ExecuteUpdateAsync(campos => campos.SetProperty(d => d.Id, idPreexistente));

        var migration = new DestaquesIniciais();
        for (var repeticao = 0; repeticao < 2; repeticao++)
        {
            foreach (var operacao in migration.UpOperations.Cast<SqlOperation>())
            {
                await contexto.Database.ExecuteSqlRawAsync(operacao.Sql);
            }
        }

        (await contexto.DestaquesHero.CountAsync()).Should().Be(totalAntes);
        var primeiro = await contexto.DestaquesHero.AsNoTracking().SingleAsync(d => d.Id == primeiroId);
        primeiro.Titulo.Should().Be("Título personalizado");
        primeiro.Ativo.Should().BeFalse();
        (await contexto.DestaquesHero.AnyAsync(d => d.Id == terceiroId)).Should().BeFalse();
        (await contexto.DestaquesHero.AnyAsync(d => d.Id == idPreexistente)).Should().BeTrue();
        migration.DownOperations.Should().BeEmpty();
        await transacao.RollbackAsync();
    }
}
