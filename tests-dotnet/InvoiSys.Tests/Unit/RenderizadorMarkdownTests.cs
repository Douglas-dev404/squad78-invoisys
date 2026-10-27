using FluentAssertions;
using InvoiSys.Application.Exportacao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Tests.Unit;

public class RenderizadorMarkdownTests
{
    private static VersaoComunicado VersaoCom(params ItemComunicado[] itens)
    {
        var release = new Release("RELEASE-2026-08", []);
        return release.ConcluirProcessamento(itens, "Release de agosto", "Resumo da release.");
    }

    private static ItemComunicado Item(CategoriaAlteracao categoria, string texto) =>
        new(categoria, texto, ["INV-1"]);

    [Fact]
    public void Agrupa_por_categoria_na_ordem_fixa_do_enunciado()
    {
        var versao = VersaoCom(
            Item(CategoriaAlteracao.Outros, "Item outros."),
            Item(CategoriaAlteracao.Correcao, "Item correção."),
            Item(CategoriaAlteracao.Melhoria, "Item melhoria."),
            Item(CategoriaAlteracao.NovaFuncionalidade, "Item novo."));

        var markdown = RenderizadorMarkdown.Renderizar(versao);

        markdown.Should().Be(
            "# Release de agosto\n\n"
            + "Resumo da release.\n\n"
            + "## Novas Funcionalidades\n\n- Item novo.\n\n"
            + "## Melhorias\n\n- Item melhoria.\n\n"
            + "## Correções\n\n- Item correção.\n\n"
            + "## Outros\n\n- Item outros.\n");
    }

    [Fact]
    public void Omite_categoria_sem_itens()
    {
        var versao = VersaoCom(Item(CategoriaAlteracao.Melhoria, "Só melhoria."));

        var markdown = RenderizadorMarkdown.Renderizar(versao);

        markdown.Should().Contain("## Melhorias");
        markdown.Should().NotContain("## Novas Funcionalidades");
        markdown.Should().NotContain("## Correções");
        markdown.Should().NotContain("## Outros");
    }

    [Fact]
    public void Item_excluido_nao_aparece_no_markdown()
    {
        var excluido = Item(CategoriaAlteracao.Melhoria, "Detalhe interno.");
        var versao = VersaoCom(Item(CategoriaAlteracao.Melhoria, "Item publicado."), excluido);
        excluido.Excluir("interno");

        var markdown = RenderizadorMarkdown.Renderizar(versao);

        markdown.Should().Contain("Item publicado.");
        markdown.Should().NotContain("Detalhe interno.");
    }

    [Fact]
    public void Edicao_humana_tem_precedencia_sobre_o_texto_da_IA()
    {
        var item = Item(CategoriaAlteracao.Correcao, "Texto da IA.");
        var versao = VersaoCom(item);
        item.EditarManualmente("Texto do revisor.");

        var markdown = RenderizadorMarkdown.Renderizar(versao);

        markdown.Should().Contain("- Texto do revisor.");
        markdown.Should().NotContain("Texto da IA.");
    }
}
