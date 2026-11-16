using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

public sealed record CartaoRelease(
    string Chave,
    string? Descricao,
    bool NoJira,
    bool LancadaNoJira,
    DateOnly? DataLancamento,
    StatusPipeline? Status,
    DateTimeOffset? ProcessadaEm);

public sealed record Quadro(IReadOnlyList<CartaoRelease> Cartoes, string? AvisoJira);

public sealed class QuadroReleases(IJiraClient jira, IReleaseRepository releases)
{
    public async Task<Quadro> MontarAsync(CancellationToken cancellationToken = default)
    {
        var processadas = await releases.ListarResumosAsync(null, cancellationToken);
        var porChave = processadas.ToDictionary(r => r.ChaveJira, StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<VersaoJira> versoes;
        string? aviso = null;

        try
        {
            versoes = await jira.ListarVersoesAsync(cancellationToken);
        }
        catch (Exception exc) when (exc is ProviderNaoConfiguradoException or IntegracaoExternaException)
        {
            versoes = [];
            aviso = exc.Message;
        }

        var cartoes = new List<CartaoRelease>();
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var versao in versoes.Where(v => !v.Arquivada))
        {
            if (!vistas.Add(versao.Nome))
            {
                continue;
            }

            porChave.TryGetValue(versao.Nome, out var resumo);
            cartoes.Add(new CartaoRelease(
                resumo?.ChaveJira ?? versao.Nome,
                versao.Descricao,
                NoJira: true,
                versao.Lancada,
                versao.DataLancamento,
                resumo?.Status,
                resumo?.CriadoEm));
        }

        cartoes.AddRange(processadas
            .Where(r => !vistas.Contains(r.ChaveJira))
            .Select(r => new CartaoRelease(r.ChaveJira, null, NoJira: false, false, null, r.Status, r.CriadoEm)));

        return new Quadro(cartoes, aviso);
    }
}
