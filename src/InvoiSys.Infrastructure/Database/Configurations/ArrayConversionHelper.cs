using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace InvoiSys.Infrastructure.Database.Configurations;

/// <summary>
/// Comparador de valor compartilhado para colunas <c>text[]</c> mapeadas a partir de
/// <see cref="IReadOnlyList{T}"/> de string (<c>HistoriaJira.Labels</c>,
/// <c>ItemComunicado.Origens</c>). Sem isso o EF Core compara por referência e marca a
/// coleção como alterada em todo SaveChanges, mesmo sem mudança real de conteúdo.
/// </summary>
internal static class ArrayConversionHelper
{
    public static readonly ValueComparer<IReadOnlyList<string>> ListaStringComparer = new(
        (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
        v => v.ToList());
}
