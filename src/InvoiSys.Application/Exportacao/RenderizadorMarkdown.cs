using System.Text;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Exportacao;

public static class RenderizadorMarkdown
{
    private static readonly (CategoriaAlteracao Categoria, string Titulo)[] Secoes =
    [
        (CategoriaAlteracao.NovaFuncionalidade, "Novas Funcionalidades"),
        (CategoriaAlteracao.Melhoria, "Melhorias"),
        (CategoriaAlteracao.Correcao, "Correções"),
        (CategoriaAlteracao.Outros, "Outros"),
    ];

    public static string Renderizar(VersaoComunicado versao)
    {
        var itens = versao.ItensPublicaveis;
        var markdown = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(versao.TituloExecutivo))
        {
            markdown.Append("# ").Append(versao.TituloExecutivo.Trim()).Append("\n\n");
        }

        if (!string.IsNullOrWhiteSpace(versao.ResumoExecutivo))
        {
            markdown.Append(versao.ResumoExecutivo.Trim()).Append("\n\n");
        }

        foreach (var (categoria, titulo) in Secoes)
        {
            var daCategoria = itens.Where(item => item.Categoria == categoria).ToList();

            if (daCategoria.Count == 0)
            {
                continue;
            }

            markdown.Append("## ").Append(titulo).Append("\n\n");

            foreach (var item in daCategoria)
            {
                markdown.Append("- ").Append(item.TextoFinal.Trim()).Append('\n');
            }

            markdown.Append('\n');
        }

        return markdown.ToString().TrimEnd() + "\n";
    }
}
