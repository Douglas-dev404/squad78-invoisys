using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Database;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class EnvioComunicadoRepositoryTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task Envios_sao_gravados_com_copia_do_destinatario_e_listados_pela_exportacao()
    {
        var revisor = await fixture.SalvarRevisorAsync();
        var release = new Release($"RELEASE-ENVIO-{Guid.NewGuid():N}", []);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto", ["INV-1"])], "Título", "Resumo");
        release.Aprovar(revisor, DateTimeOffset.UtcNow);
        var exportado = new ComunicadoExportado(
            release.VersaoCliente!, FormatoExportacao.Html, "<p>x</p>", null, revisor.Nome, revisor.Id);
        var ana = new Destinatario("Ana", $"ana-{Guid.NewGuid():N}@acme.com", PublicoAlvo.Cliente);
        var bia = new Destinatario("Bia", $"bia-{Guid.NewGuid():N}@acme.com", PublicoAlvo.Cliente);

        await using (var contexto = fixture.CriarContexto())
        {
            await new ReleaseRepository(contexto).SalvarAsync(release);
            await new ComunicadoExportadoRepository(contexto).AdicionarAsync(exportado);
            var destinatarios = new DestinatarioRepository(contexto);
            await destinatarios.SalvarAsync(ana);
            await destinatarios.SalvarAsync(bia);
            await new EnvioComunicadoRepository(contexto).AdicionarAsync(
            [
                EnvioComunicado.Falhou(exportado, bia, "Provedor de e-mail retornou 503."),
                EnvioComunicado.Enviado(exportado, ana),
            ]);
        }

        await using (var alteracao = fixture.CriarContexto())
        {
            var repositorio = new DestinatarioRepository(alteracao);
            var carregada = await repositorio.BuscarPorIdAsync(ana.Id);
            carregada!.Alterar("Ana Souza", carregada.Email, PublicoAlvo.Cliente);
            await repositorio.SalvarAsync(carregada);
        }

        await using var leitura = fixture.CriarContexto();
        var envios = await new EnvioComunicadoRepository(leitura).ListarPorExportacaoAsync(exportado.Id);

        envios.Select(e => e.Nome).Should().Equal("Ana", "Bia");
        envios[0].Status.Should().Be(StatusEnvio.Enviado);
        envios[0].Erro.Should().BeNull();
        envios[1].Status.Should().Be(StatusEnvio.Falhou);
        envios[1].Erro.Should().Be("Provedor de e-mail retornou 503.");
        envios.Should().OnlyContain(e => e.ComunicadoExportadoId == exportado.Id);
    }
}
