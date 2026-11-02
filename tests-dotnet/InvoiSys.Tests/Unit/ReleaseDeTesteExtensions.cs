using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Database;
using InvoiSys.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace InvoiSys.Tests.Unit;

public static class ReleaseDeTesteExtensions
{
    public static VersaoComunicado ConcluirProcessamento(
        this Release release,
        IEnumerable<ItemComunicado> itens,
        string titulo,
        string resumo,
        PublicoAlvo publico = PublicoAlvo.Cliente) =>
        release.ConcluirProcessamento(
            itens, titulo, resumo, AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow, publico);

    public static void Reabrir(this Release release, PublicoAlvo publico = PublicoAlvo.Cliente) =>
        release.Reabrir(AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow, publico);

    public static ItemComunicado EditarItem(this Release release, Guid itemId, string texto) =>
        release.EditarItem(itemId, texto, AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow);

    public static ItemComunicado ExcluirItem(this Release release, Guid itemId, string? motivo = null) =>
        release.ExcluirItem(itemId, AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow, motivo);

    public static ItemComunicado ReincluirItem(this Release release, Guid itemId) =>
        release.ReincluirItem(itemId, AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow);

    public static Task<Release> ExecutarAsync(
        this PipelineGeracaoReleaseNote pipeline,
        string chaveRelease) =>
        pipeline.ExecutarAsync(chaveRelease, AutenticacaoDeTeste.Revisor.Id);
}

public static class PipelineDeTeste
{
    public static PipelineGeracaoReleaseNote Criar(
        IJiraClient jira,
        ILlmProvider llm,
        IReleaseRepository releases,
        string? modeloLlm = null)
    {
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.Add(AutenticacaoDeTeste.Revisor);
        return new PipelineGeracaoReleaseNote(jira, llm, releases, usuarios, new FakeTimeProvider(DateTimeOffset.UtcNow), modeloLlm);
    }

    public static PipelineGeracaoReleaseNote Criar(
        IJiraClient jira,
        ILlmProvider llm,
        InvoiSysDbContext contexto) =>
        new(jira, llm, new ReleaseRepository(contexto), new UsuarioRepository(contexto), TimeProvider.System);
}
