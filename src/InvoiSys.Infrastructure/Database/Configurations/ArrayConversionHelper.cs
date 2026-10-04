using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace InvoiSys.Infrastructure.Database.Configurations;

internal static class ArrayConversionHelper
{
    // Sem este comparer o EF compara text[] por referência e marca a lista como alterada em todo SaveChanges.
    public static readonly ValueComparer<IReadOnlyList<string>> ListaStringComparer = new(
        (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
        v => v.ToList());
}
