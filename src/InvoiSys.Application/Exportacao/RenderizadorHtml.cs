using System.Net;
using System.Text;
using InvoiSys.Domain.Entities;

namespace InvoiSys.Application.Exportacao;

public static class RenderizadorHtml
{
    private const string TituloPadrao = "Release Notes";

    private const string Estilo =
        "body{font-family:system-ui,-apple-system,'Segoe UI',Roboto,Arial,sans-serif;"
        + "max-width:760px;margin:40px auto;padding:0 16px;color:#1b1c1a;line-height:1.6;background:#fff}"
        + "h1{font-size:1.8rem;margin:0 0 .5rem;color:#1b4332}"
        + "h2{font-size:1.25rem;margin:2rem 0 .5rem;padding-bottom:.25rem;border-bottom:1px solid #c1c8c2;color:#1b4332}"
        + "p.resumo{font-size:1.05rem;color:#414844}"
        + "ul{padding-left:1.25rem}li{margin:.35rem 0}";

    public static string Renderizar(VersaoComunicado versao)
    {
        var titulo = string.IsNullOrWhiteSpace(versao.TituloExecutivo)
            ? null
            : versao.TituloExecutivo.Trim();

        var html = new StringBuilder();
        html.Append("<!DOCTYPE html>\n")
            .Append("<html lang=\"pt-BR\">\n<head>\n")
            .Append("<meta charset=\"utf-8\">\n")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n")
            .Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'\">\n")
            .Append("<title>").Append(Escapar(titulo ?? TituloPadrao)).Append("</title>\n")
            .Append("<style>").Append(Estilo).Append("</style>\n")
            .Append("</head>\n<body>\n");

        if (titulo is not null)
        {
            html.Append("<h1>").Append(Escapar(titulo)).Append("</h1>\n");
        }

        if (!string.IsNullOrWhiteSpace(versao.ResumoExecutivo))
        {
            html.Append("<p class=\"resumo\">").Append(Escapar(versao.ResumoExecutivo.Trim())).Append("</p>\n");
        }

        foreach (var (tituloSecao, itens) in SecoesComunicado.De(versao))
        {
            html.Append("<h2>").Append(Escapar(tituloSecao)).Append("</h2>\n<ul>\n");

            foreach (var item in itens)
            {
                html.Append("<li>").Append(Escapar(item.TextoFinal.Trim())).Append("</li>\n");
            }

            html.Append("</ul>\n");
        }

        html.Append("</body>\n</html>\n");
        return html.ToString();
    }

    private static string Escapar(string texto) => WebUtility.HtmlEncode(texto);
}
