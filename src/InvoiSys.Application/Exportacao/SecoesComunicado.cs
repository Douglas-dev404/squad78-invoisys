using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Exportacao;

internal static class SecoesComunicado
{
    private static readonly (CategoriaAlteracao Categoria, string Titulo)[] Ordem =
    [
        (CategoriaAlteracao.NovaFuncionalidade, "Novas Funcionalidades"),
        (CategoriaAlteracao.Melhoria, "Melhorias"),
        (CategoriaAlteracao.Correcao, "Correções"),
        (CategoriaAlteracao.Outros, "Outros"),
    ];

    public static IEnumerable<(string Titulo, IReadOnlyList<ItemComunicado> Itens)> De(VersaoComunicado versao)
    {
        var itens = versao.ItensPublicaveis;

        foreach (var (categoria, titulo) in Ordem)
        {
            var daCategoria = itens.Where(item => item.Categoria == categoria).ToList();

            if (daCategoria.Count > 0)
            {
                yield return (titulo, daCategoria);
            }
        }
    }
}
