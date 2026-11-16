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

    public int ChamadasAgrupar { get; private set; }

    /// <summary>Se definido, segura o agrupamento até ser liberado (simula a IA demorando).</summary>
    public TaskCompletionSource? Portao { get; set; }

    public TaskCompletionSource IniciouAgrupar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<IReadOnlyList<IReadOnlyList<string>>> AgruparSemelhantesAsync(
        IReadOnlyList<(string Chave, string Texto)> textos,
        CancellationToken cancellationToken = default)
    {
        LancarSeConfigurado();

        ChamadasAgrupar++;
        IniciouAgrupar.TrySetResult();

        if (Portao is not null)
        {
            await Portao.Task;
        }

        IReadOnlyList<IReadOnlyList<string>> grupos = GruposFixos
            ?? [.. textos.Select(t => (IReadOnlyList<string>)new[] { t.Chave })];

        return grupos;
    }

    public List<PublicoAlvo> PublicosReescritos { get; } = [];

    public List<PublicoAlvo> PublicosTituloEResumo { get; } = [];

    public Task<string> ReescreverLinguagemNegocioAsync(
        IReadOnlyList<string> textosFonte,
        CategoriaAlteracao categoria,
        PublicoAlvo publico = PublicoAlvo.Cliente,
        CancellationToken cancellationToken = default)
    {
        LancarSeConfigurado();
        GruposReescritos.Add(textosFonte);
        PublicosReescritos.Add(publico);
        return Task.FromResult(publico == PublicoAlvo.Cliente ? TextoReescrito : $"{TextoReescrito} ({publico.ParaValor()})");
    }

    public Task<(string Titulo, string Resumo)> GerarTituloEResumoAsync(
        IReadOnlyList<string> itensTexto,
        PublicoAlvo publico = PublicoAlvo.Cliente,
        CancellationToken cancellationToken = default)
    {
        LancarSeConfigurado();
        PublicosTituloEResumo.Add(publico);
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
