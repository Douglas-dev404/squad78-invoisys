using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "destaques_hero",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    icone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_destaques_hero", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "releases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    chave_jira = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_releases", x => x.id);
                    table.CheckConstraint("ck_releases_status", "status IN ('pendente','processando','aguardando_revisao','aprovado','falhou')");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    papel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "release_manager"),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "execucoes_pipeline",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    modelo_llm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    erro = table.Column<string>(type: "text", nullable: true),
                    iniciado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concluido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_execucoes_pipeline", x => x.id);
                    table.CheckConstraint("ck_execucoes_pipeline_status", "status IN ('iniciada','concluida','falhou')");
                    table.ForeignKey(
                        name: "fk_execucoes_pipeline_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historias_jira",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    chave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    titulo = table.Column<string>(type: "text", nullable: false),
                    descricao_tecnica = table.Column<string>(type: "text", nullable: false),
                    tipo_issue = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    texto_release_note = table.Column<string>(type: "text", nullable: true),
                    labels = table.Column<string[]>(type: "text[]", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historias_jira", x => x.id);
                    table.ForeignKey(
                        name: "fk_historias_jira_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "versoes_comunicado",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publico = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    titulo_executivo = table.Column<string>(type: "text", nullable: true),
                    resumo_executivo = table.Column<string>(type: "text", nullable: true),
                    revisado_por = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    revisado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_reprovacao = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_versoes_comunicado", x => x.id);
                    table.CheckConstraint("ck_versoes_comunicado_motivo_reprovacao", "status <> 'reprovado' OR motivo_reprovacao IS NOT NULL");
                    table.CheckConstraint("ck_versoes_comunicado_publico", "publico IN ('cliente','comercial','suporte','interno')");
                    table.CheckConstraint("ck_versoes_comunicado_status", "status IN ('aguardando_revisao','aprovado','reprovado')");
                    table.ForeignKey(
                        name: "fk_versoes_comunicado_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comunicados_exportados",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    versao_comunicado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publico = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    formato = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    conteudo = table.Column<string>(type: "text", nullable: true),
                    caminho_arquivo = table.Column<string>(type: "text", nullable: true),
                    gerado_por = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gerado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comunicados_exportados", x => x.id);
                    table.CheckConstraint("ck_comunicados_exportados_formato", "formato IN ('markdown','html','pdf')");
                    table.CheckConstraint("ck_comunicados_exportados_publico", "publico IN ('cliente','comercial','suporte','interno')");
                    table.ForeignKey(
                        name: "fk_comunicados_exportados_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_comunicados_exportados_versoes_comunicado_versao_comunicado",
                        column: x => x.versao_comunicado_id,
                        principalTable: "versoes_comunicado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "itens_comunicado",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    texto = table.Column<string>(type: "text", nullable: false),
                    origens = table.Column<string[]>(type: "text[]", nullable: false),
                    texto_editado_manualmente = table.Column<string>(type: "text", nullable: true),
                    incluido = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    motivo_exclusao = table.Column<string>(type: "text", nullable: true),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    versao_comunicado_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_comunicado", x => x.id);
                    table.CheckConstraint("ck_itens_comunicado_categoria", "categoria IN ('nova_funcionalidade','melhoria','correcao','outros')");
                    table.ForeignKey(
                        name: "fk_itens_comunicado_versoes_comunicado_versao_comunicado_id",
                        column: x => x.versao_comunicado_id,
                        principalTable: "versoes_comunicado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comunicados_exportados_release_id",
                table: "comunicados_exportados",
                column: "release_id");

            migrationBuilder.CreateIndex(
                name: "ix_comunicados_exportados_versao_comunicado_id",
                table: "comunicados_exportados",
                column: "versao_comunicado_id");

            migrationBuilder.CreateIndex(
                name: "ix_destaques_hero_ativo_ordem",
                table: "destaques_hero",
                columns: new[] { "ativo", "ordem" });

            migrationBuilder.CreateIndex(
                name: "ix_execucoes_pipeline_release_id",
                table: "execucoes_pipeline",
                column: "release_id");

            migrationBuilder.CreateIndex(
                name: "ix_historias_jira_release_id_chave",
                table: "historias_jira",
                columns: new[] { "release_id", "chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_itens_comunicado_origens",
                table: "itens_comunicado",
                column: "origens")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_itens_comunicado_versao_comunicado_id",
                table: "itens_comunicado",
                column: "versao_comunicado_id");

            migrationBuilder.CreateIndex(
                name: "ix_releases_chave_jira",
                table: "releases",
                column: "chave_jira",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_releases_status",
                table: "releases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_versoes_comunicado_release_id_publico",
                table: "versoes_comunicado",
                columns: new[] { "release_id", "publico" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_versoes_comunicado_status",
                table: "versoes_comunicado",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comunicados_exportados");

            migrationBuilder.DropTable(
                name: "destaques_hero");

            migrationBuilder.DropTable(
                name: "execucoes_pipeline");

            migrationBuilder.DropTable(
                name: "historias_jira");

            migrationBuilder.DropTable(
                name: "itens_comunicado");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "versoes_comunicado");

            migrationBuilder.DropTable(
                name: "releases");
        }
    }
}
