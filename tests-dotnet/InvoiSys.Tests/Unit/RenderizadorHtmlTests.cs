using System.Net;
using FluentAssertions;
using InvoiSys.Application.Exportacao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Tests.Unit;

public class RenderizadorHtmlTests
{
    private static VersaoComunicado VersaoCom(string titulo, string resumo, params ItemComunicado[] itens)
    {
        var release = new Release("RELEASE-2026-08", []);
        return release.ConcluirProcessamento(itens, titulo, resumo);
    }

    private static ItemComunicado Item(CategoriaAlteracao categoria, string texto) =>
        new(categoria, texto, ["INV-1"]);

    [Fact]
    public void Gera_documento_autocontido_sem_recurso_externo()
    {
        var versao = VersaoCom("Release de agosto", "Resumo.", Item(CategoriaAlteracao.Melhoria, "Item."));

        var html = RenderizadorHtml.Renderizar(versao);

        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<meta charset=\"utf-8\">");
        html.Should().Contain("<style>");
        html.Should().NotContain("<link");
        html.Should().NotContain("<script");
        html.Should().NotContain("src=");
        html.Should().NotContain("http://");
        html.Should().NotContain("https://");
    }

    [Fact]
    public void Titulo_e_resumo_no_topo_e_categorias_na_ordem_fixa()
    {
        var versao = VersaoCom(
            "Release de agosto",
            "Resumo da release.",
            Item(CategoriaAlteracao.Outros, "Item outros."),
            Item(CategoriaAlteracao.Correcao, "Item correção."),
            Item(CategoriaAlteracao.Melhoria, "Item melhoria."),
            Item(CategoriaAlteracao.NovaFuncionalidade, "Item novo."));

        var html = RenderizadorHtml.Renderizar(versao);

        var posicoes = new[]
        {
            html.IndexOf("<h1>Release de agosto</h1>", StringComparison.Ordinal),
            html.IndexOf("<p class=\"resumo\">Resumo da release.</p>", StringComparison.Ordinal),
            html.IndexOf("<h2>Novas Funcionalidades</h2>", StringComparison.Ordinal),
            html.IndexOf("<h2>Melhorias</h2>", StringComparison.Ordinal),
            html.IndexOf($"<h2>{WebUtility.HtmlEncode("Correções")}</h2>", StringComparison.Ordinal),
            html.IndexOf("<h2>Outros</h2>", StringComparison.Ordinal),
        };

        posicoes.Should().OnlyContain(posicao => posicao >= 0);
        posicoes.Should().BeInAscendingOrder();
        html.Should().Contain("<title>Release de agosto</title>");
    }

    [Fact]
    public void Texto_com_script_ou_e_comercial_na_edicao_humana_sai_escapado()
    {
        var item = Item(CategoriaAlteracao.Correcao, "Texto da IA.");
        var versao = VersaoCom("Título <b>&</b>", "Resumo & cia.", item);
        item.EditarManualmente("<script>alert('x')</script> NF-e & NFS-e");

        var html = RenderizadorHtml.Renderizar(versao);

        html.Should().NotContain("<script>");
        html.Should().NotContain("<b>");
        html.Should().Contain("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt; NF-e &amp; NFS-e");
        html.Should().Contain($"<h1>{WebUtility.HtmlEncode("Título <b>&</b>")}</h1>");
        html.Should().Contain("Resumo &amp; cia.");
        html.Should().NotContain("Texto da IA.");
    }

    [Fact]
    public void Quebra_de_linha_no_resumo_e_nos_itens_e_mantida_pelo_estilo_e_o_texto_segue_escapado()
    {
        var versao = VersaoCom(
            "Release",
            "Primeira linha.\nSegunda <b>linha</b>.",
            Item(CategoriaAlteracao.Melhoria, "Passo 1.\nPasso <2>."));

        var html = RenderizadorHtml.Renderizar(versao);

        html.Should().Contain("p.resumo{font-size:1.05rem;color:#414844;white-space:pre-line}");
        html.Should().Contain("li{margin:.35rem 0;white-space:pre-line}");
        html.Should().Contain("<p class=\"resumo\">Primeira linha.\nSegunda &lt;b&gt;linha&lt;/b&gt;.</p>");
        html.Should().Contain("<li>Passo 1.\nPasso &lt;2&gt;.</li>");
        html.Should().NotContain("<b>");
    }

    [Fact]
    public void Item_excluido_e_categoria_vazia_nao_aparecem()
    {
        var excluido = Item(CategoriaAlteracao.Correcao, "Detalhe interno.");
        var versao = VersaoCom("", "", Item(CategoriaAlteracao.Melhoria, "Item publicado."), excluido);
        excluido.Excluir("interno");

        var html = RenderizadorHtml.Renderizar(versao);

        html.Should().Contain("<li>Item publicado.</li>");
        html.Should().NotContain("Detalhe interno.");
        html.Should().NotContain($"<h2>{WebUtility.HtmlEncode("Correções")}</h2>");
        html.Should().NotContain("<h1>");
        html.Should().Contain("<title>Release Notes</title>");
    }
}
