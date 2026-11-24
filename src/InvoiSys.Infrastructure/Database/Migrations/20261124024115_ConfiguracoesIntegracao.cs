using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracoesIntegracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracoes_integracao",
                columns: table => new
                {
                    chave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valor = table.Column<string>(type: "text", nullable: false),
                    secreto = table.Column<bool>(type: "boolean", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracoes_integracao", x => x.chave);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracoes_integracao");
        }
    }
}
