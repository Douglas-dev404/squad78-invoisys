using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

public sealed class ItemComunicado
{
    private ItemComunicado()
    {
        Texto = string.Empty;
        Origens = [];
    }

    public ItemComunicado(CategoriaAlteracao categoria, string texto, IReadOnlyList<string> origens)
    {
        Id = Guid.NewGuid();
        Categoria = categoria;
        Texto = texto;
        Origens = origens;
        Incluido = true;
    }

    public Guid Id { get; private init; }

    public CategoriaAlteracao Categoria { get; private init; }

    public string Texto { get; private init; }

    public IReadOnlyList<string> Origens { get; private init; }

    public string? TextoEditadoManualmente { get; private set; }

    public bool Incluido { get; private set; } = true;

    public string? MotivoExclusao { get; private set; }

    public string TextoFinal =>
        string.IsNullOrWhiteSpace(TextoEditadoManualmente) ? Texto : TextoEditadoManualmente;

    // internal: de fora do domínio, só via Release, que recusa alterar versão aprovada (ADR-021).
    internal void EditarManualmente(string texto) => TextoEditadoManualmente = texto;

    internal void Excluir(string? motivo = null)
    {
        Incluido = false;
        MotivoExclusao = motivo;
    }

    internal void Reincluir()
    {
        Incluido = true;
        MotivoExclusao = null;
    }
}
