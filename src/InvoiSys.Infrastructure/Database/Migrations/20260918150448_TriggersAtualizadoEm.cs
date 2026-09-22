using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Triggers que mantêm <c>atualizado_em</c> correto nas tabelas mutáveis.
    ///
    /// O <c>DEFAULT now()</c> da coluna só age no INSERT — sem trigger, o valor
    /// congela na criação e um UPDATE nunca o move, o que torna o campo uma mentira
    /// silenciosa. Fica no banco, e não em código de aplicação, justamente para valer
    /// também para UPDATE via SQL direto, script de correção ou outro serviço que
    /// venha a escrever na base.
    ///
    /// Só as três tabelas que sofrem UPDATE de verdade têm trigger:
    /// <c>releases</c> (status muda ao longo do pipeline), <c>versoes_comunicado</c>
    /// (ciclo de revisão) e <c>itens_comunicado</c> (edição humana, exclusão).
    /// <c>execucoes_pipeline</c>, <c>comunicados_exportados</c>, <c>usuarios</c> e
    /// <c>destaques_hero</c> não têm a coluna: são registros de evento ou de cadastro,
    /// cujo histórico não se reescreve.
    /// </summary>
    public partial class TriggersAtualizadoEm : Migration
    {
        private static readonly string[] TabelasComAtualizadoEm =
        [
            "releases",
            "versoes_comunicado",
            "itens_comunicado",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION set_atualizado_em()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.atualizado_em = now();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            foreach (var tabela in TabelasComAtualizadoEm)
            {
                migrationBuilder.Sql($"""
                    CREATE TRIGGER trg_{tabela}_atualizado_em
                    BEFORE UPDATE ON {tabela}
                    FOR EACH ROW
                    EXECUTE FUNCTION set_atualizado_em();
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var tabela in TabelasComAtualizadoEm)
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS trg_{tabela}_atualizado_em ON {tabela};");
            }

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS set_atualizado_em();");
        }
    }
}
