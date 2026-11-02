using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InvoiSys.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class EventosRevisao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_revisao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sequencia = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    versao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ocorrido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    detalhe = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_revisao", x => x.id);
                    table.CheckConstraint("ck_eventos_revisao_acao", "acao IN ('processar','aprovar','reprovar','reabrir','editar_item','excluir_item','reincluir_item')");
                    table.ForeignKey(
                        name: "fk_eventos_revisao_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_eventos_revisao_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_eventos_revisao_versoes_comunicado_versao_id",
                        column: x => x.versao_id,
                        principalTable: "versoes_comunicado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_eventos_revisao_release_id_sequencia",
                table: "eventos_revisao",
                columns: new[] { "release_id", "sequencia" });

            migrationBuilder.CreateIndex(
                name: "ix_eventos_revisao_usuario_id",
                table: "eventos_revisao",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_revisao_versao_id",
                table: "eventos_revisao",
                column: "versao_id");

            migrationBuilder.Sql("""
                CREATE FUNCTION bloquear_alteracao_eventos_revisao()
                RETURNS TRIGGER AS $$
                BEGIN
                    RAISE EXCEPTION 'eventos_revisao é uma trilha de auditoria só de inclusão: % não é permitido.', TG_OP
                        USING ERRCODE = 'integrity_constraint_violation';
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_eventos_revisao_somente_inclusao
                BEFORE UPDATE OR DELETE ON eventos_revisao
                FOR EACH ROW
                EXECUTE FUNCTION bloquear_alteracao_eventos_revisao();
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_eventos_revisao_sem_truncate
                BEFORE TRUNCATE ON eventos_revisao
                FOR EACH STATEMENT
                EXECUTE FUNCTION bloquear_alteracao_eventos_revisao();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER trg_eventos_revisao_sem_truncate ON eventos_revisao;");
            migrationBuilder.Sql("DROP TRIGGER trg_eventos_revisao_somente_inclusao ON eventos_revisao;");
            migrationBuilder.Sql("DROP FUNCTION bloquear_alteracao_eventos_revisao();");

            migrationBuilder.DropTable(
                name: "eventos_revisao");
        }
    }
}
