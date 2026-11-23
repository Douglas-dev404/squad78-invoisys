using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDestinatarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "destinatarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    publico = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_destinatarios", x => x.id);
                    table.CheckConstraint("ck_destinatarios_publico", "publico IN ('cliente','comercial','suporte','interno')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_destinatarios_email_publico",
                table: "destinatarios",
                columns: new[] { "email", "publico" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "destinatarios");
        }
    }
}
