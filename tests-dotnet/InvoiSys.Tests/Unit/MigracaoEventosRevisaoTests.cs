using FluentAssertions;
using InvoiSys.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace InvoiSys.Tests.Unit;

public class MigracaoEventosRevisaoTests
{
    [Fact]
    public void Up_cria_a_tabela_e_os_triggers_de_so_inclusao_sem_nenhum_Drop()
    {
        var operacoes = new EventosRevisao().UpOperations;

        var proibidas = operacoes.Where(o =>
            o is DropColumnOperation or DropTableOperation or DropIndexOperation
                or DropForeignKeyOperation or DropPrimaryKeyOperation or DropUniqueConstraintOperation
                or DropCheckConstraintOperation or AlterColumnOperation);
        proibidas.Should().BeEmpty();
        operacoes.OfType<CreateTableOperation>().Should().ContainSingle(t => t.Name == "eventos_revisao");

        var sql = string.Join("\n", operacoes.OfType<SqlOperation>().Select(o => o.Sql));
        sql.Should().Contain("BEFORE UPDATE OR DELETE ON eventos_revisao");
        sql.Should().Contain("BEFORE TRUNCATE ON eventos_revisao");
    }

    [Fact]
    public void Chaves_estrangeiras_sao_RESTRICT_para_a_auditoria_nunca_sumir_por_cascata()
    {
        var tabela = new EventosRevisao().UpOperations.OfType<CreateTableOperation>().Single();

        tabela.ForeignKeys.Should().HaveCount(3).And.OnlyContain(
            f => f.OnDelete == Microsoft.EntityFrameworkCore.Migrations.ReferentialAction.Restrict);
        tabela.Columns.Single(c => c.Name == "item_id").IsNullable.Should().BeTrue();
    }
}
