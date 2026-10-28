using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Database.Migrations
{
    public partial class DestaquesIniciais : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO destaques_hero (id, titulo, descricao, icone, ordem, ativo)
                SELECT inicial.id, inicial.titulo, inicial.descricao, inicial.icone, inicial.ordem, true
                FROM (VALUES
                    ('286921eb-54b7-47a1-a9db-a56e2cc84001'::uuid,
                     'Gestão automatizada de tasks', 'Simplifique sua produtividade.', 'task_alt', 1),
                    ('286921eb-54b7-47a1-a9db-a56e2cc84002'::uuid,
                     'Release Notes com Inteligência Artificial',
                     'Converta histórias do Jira em comunicados claros em poucos segundos.', 'auto_awesome', 2),
                    ('286921eb-54b7-47a1-a9db-a56e2cc84003'::uuid,
                     'Governança e Aprovação Humana',
                     'Revisão obrigatória e controle granular antes de cada publicação oficial.', 'verified_user', 3)
                ) AS inicial(id, titulo, descricao, icone, ordem)
                WHERE NOT EXISTS (
                    SELECT 1 FROM destaques_hero existente
                    WHERE existente.id = inicial.id OR existente.titulo = inicial.titulo
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
