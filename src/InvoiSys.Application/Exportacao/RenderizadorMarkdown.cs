using System.Text;
using InvoiSys.Domain.Entities;

namespace InvoiSys.Application.Exportacao;

public static class RenderizadorMarkdown
{
    public static string Renderizar(VersaoComunicado versao)
    {
        var markdown = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(versao.TituloExecutivo))
        {
            markdown.Append("# ").Append(versao.TituloExecutivo.Trim()).Append("\n\n");
        }

        if (!string.IsNullOrWhiteSpace(versao.ResumoExecutivo))
        {
            markdown.Append(versao.ResumoExecutivo.Trim()).Append("\n\n");
        }

        foreach (var (titulo, itens) in SecoesComunicado.De(versao))
        {
            markdown.Append("## ").Append(titulo).Append("\n\n");

            foreach (var item in itens)
            {
                markdown.Append("- ").Append(item.TextoFinal.Trim()).Append('\n');
            }

            markdown.Append('\n');
        }

        return markdown.ToString().TrimEnd() + "\n";
    }
}
