using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RevisorEGeradorComoFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "revisado_por_id",
                table: "versoes_comunicado",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "gerado_por_id",
                table: "comunicados_exportados",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_versoes_comunicado_revisado_por_id",
                table: "versoes_comunicado",
                column: "revisado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_comunicados_exportados_gerado_por_id",
                table: "comunicados_exportados",
                column: "gerado_por_id");

            migrationBuilder.AddForeignKey(
                name: "fk_comunicados_exportados_usuarios_gerado_por_id",
                table: "comunicados_exportados",
                column: "gerado_por_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_versoes_comunicado_usuarios_revisado_por_id",
                table: "versoes_comunicado",
                column: "revisado_por_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_comunicados_exportados_usuarios_gerado_por_id",
                table: "comunicados_exportados");

            migrationBuilder.DropForeignKey(
                name: "fk_versoes_comunicado_usuarios_revisado_por_id",
                table: "versoes_comunicado");

            migrationBuilder.DropIndex(
                name: "ix_versoes_comunicado_revisado_por_id",
                table: "versoes_comunicado");

            migrationBuilder.DropIndex(
                name: "ix_comunicados_exportados_gerado_por_id",
                table: "comunicados_exportados");

            migrationBuilder.DropColumn(
                name: "revisado_por_id",
                table: "versoes_comunicado");

            migrationBuilder.DropColumn(
                name: "gerado_por_id",
                table: "comunicados_exportados");
        }
    }
}
