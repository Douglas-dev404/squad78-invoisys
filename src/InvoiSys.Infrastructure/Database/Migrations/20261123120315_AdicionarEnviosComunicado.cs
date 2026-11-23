using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarEnviosComunicado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "envios_comunicado",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    comunicado_exportado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destinatario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    erro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    enviado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_envios_comunicado", x => x.id);
                    table.CheckConstraint("ck_envios_comunicado_status", "status IN ('enviado','falhou')");
                    table.ForeignKey(
                        name: "fk_envios_comunicado_comunicados_exportados_comunicado_exporta",
                        column: x => x.comunicado_exportado_id,
                        principalTable: "comunicados_exportados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_envios_comunicado_destinatarios_destinatario_id",
                        column: x => x.destinatario_id,
                        principalTable: "destinatarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_envios_comunicado_comunicado_exportado_id",
                table: "envios_comunicado",
                column: "comunicado_exportado_id");

            migrationBuilder.CreateIndex(
                name: "ix_envios_comunicado_destinatario_id",
                table: "envios_comunicado",
                column: "destinatario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "envios_comunicado");
        }
    }
}
