using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeLlmProvider : ILlmProvider
{
    public IReadOnlyList<IReadOnlyList<string>>? GruposFixos { get; set; }

    public CategoriaAlteracao CategoriaFixa { get; set; } = CategoriaAlteracao.Melhoria;

    public string TextoReescrito { get; set; } = "Texto em linguagem de negócio.";

    public (string Titulo, string Resumo) TituloEResumo { get; set; } =
        ("Título executivo", "Resumo executivo");

    public Exception? FalhaAoChamar { get; set; }

    public List<string> TextosCategorizados { get; } = [];

    public List<IReadOnlyList<string>> GruposReescritos { get; } = [];

    public Task<CategoriaAlteracao> CategorizarAsync(
        string textoFonte,
        CancellationToken cancellationToken = default)
    {
        LancarSeConfigurado();
        TextosCategorizados.Add(textoFonte);
        return Task.FromResult(CategoriaFixa);
    }

    public Task<IReadOnlyList<IReadOnlyList<string>>> AgruparSemelhantesAsync(
        IReadOnlyList<(string Chave, string Texto)> textos,
        CancellationToken cancellationToken = default)
    {
        LancarSeConfigurado();

        IReadOnlyList<IReadOnlyList<string>> grupos = GruposFixos
            ?? [.. textos.Select(t => (IReadOnlyList<string>)new[] { t.Chave })];

        return Task.FromResult(grupos);
    }

    public Task<string> ReescreverLinguagemNegocioAsync(
        IReadOnlyList<string> textosFonte,
        CategoriaAlteracao categoria,
        CancellationToken cancellationToken = default)
    {
        LancarSeConfigurado();
        GruposReescritos.Add(textosFonte);
        return Task.FromResult(TextoReescrito);
    }

    public Task<(string Titulo, string Resumo)> GerarTituloEResumoAsync(
        IReadOnlyList<string> itensTexto,
        CancellationToken cancellationToken = default)
    {
        LancarSeConfigurado();
        return Task.FromResult(TituloEResumo);
    }

    private void LancarSeConfigurado()
    {
        if (FalhaAoChamar is not null)
        {
            throw FalhaAoChamar;
        }
    }
}
