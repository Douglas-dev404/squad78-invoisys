using FluentAssertions;
using InvoiSys.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace InvoiSys.Tests.Unit;

public class MigracaoSemDropTests
{
    [Fact]
    public void RevisorEGeradorComoFk_so_adiciona_colunas_anulaveis_indices_e_FKs_no_Up()
    {
        var operacoes = new RevisorEGeradorComoFk().UpOperations;

        var proibidas = operacoes.Where(o =>
            o is DropColumnOperation or DropTableOperation or DropIndexOperation
                or DropForeignKeyOperation or DropPrimaryKeyOperation or DropUniqueConstraintOperation
                or DropCheckConstraintOperation or SqlOperation or AlterColumnOperation);
        proibidas.Should().BeEmpty();
        operacoes.OfType<AddColumnOperation>().Should().HaveCount(2).And.OnlyContain(c => c.IsNullable);
        operacoes.OfType<AddForeignKeyOperation>().Should().HaveCount(2).And.OnlyContain(
            f => f.OnDelete == Microsoft.EntityFrameworkCore.Migrations.ReferentialAction.Restrict
                && f.PrincipalTable == "usuarios");
    }
}
