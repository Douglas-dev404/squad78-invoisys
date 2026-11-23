using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class DestinatarioRepositoryTests(PostgresContainerFixture fixture)
{
    private static string EmailUnico(string prefixo = "dest") => $"{prefixo}-{Guid.NewGuid():N}@acme.com";

    private async Task Salvar(Destinatario destinatario)
    {
        await using var contexto = fixture.CriarContexto();
        await new DestinatarioRepository(contexto).SalvarAsync(destinatario);
    }

    [Fact]
    public async Task SalvarAsync_persiste_e_BuscarPorId_traz_todos_os_campos()
    {
        var destinatario = new Destinatario("Financeiro ACME", EmailUnico(), PublicoAlvo.Comercial);
        await Salvar(destinatario);

        await using var contexto = fixture.CriarContexto();
        var carregado = await new DestinatarioRepository(contexto).BuscarPorIdAsync(destinatario.Id);

        carregado.Should().NotBeNull();
        carregado!.Nome.Should().Be("Financeiro ACME");
        carregado.Email.Should().Be(destinatario.Email);
        carregado.Publico.Should().Be(PublicoAlvo.Comercial);
        carregado.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Mesmo_email_em_publicos_diferentes_e_aceito_e_no_mesmo_publico_viola_o_indice_unico()
    {
        var email = EmailUnico();
        await Salvar(new Destinatario("Cliente", email, PublicoAlvo.Cliente));
        await Salvar(new Destinatario("Suporte", email, PublicoAlvo.Suporte));

        var duplicar = () => Salvar(new Destinatario("De novo", email, PublicoAlvo.Cliente));

        await duplicar.Should().ThrowAsync<DbUpdateException>();

        await using var contexto = fixture.CriarContexto();
        var repositorio = new DestinatarioRepository(contexto);
        (await repositorio.ExisteEmailNoPublicoAsync(email.ToUpperInvariant(), PublicoAlvo.Cliente)).Should().BeTrue();
        (await repositorio.ExisteEmailNoPublicoAsync(email, PublicoAlvo.Interno)).Should().BeFalse();
    }

    [Fact]
    public async Task ExisteEmailNoPublico_ignora_o_proprio_id()
    {
        var destinatario = new Destinatario("Próprio", EmailUnico(), PublicoAlvo.Interno);
        await Salvar(destinatario);

        await using var contexto = fixture.CriarContexto();
        var existe = await new DestinatarioRepository(contexto)
            .ExisteEmailNoPublicoAsync(destinatario.Email, PublicoAlvo.Interno, destinatario.Id);

        existe.Should().BeFalse();
    }

    [Fact]
    public async Task Listar_filtra_por_publico_e_esconde_inativos_por_padrao()
    {
        var prefixo = $"lista-{Guid.NewGuid():N}";
        var ativo = new Destinatario($"{prefixo} A", EmailUnico(prefixo), PublicoAlvo.Suporte);
        var inativo = new Destinatario($"{prefixo} B", EmailUnico(prefixo), PublicoAlvo.Suporte);
        inativo.Desativar();
        var outroPublico = new Destinatario($"{prefixo} C", EmailUnico(prefixo), PublicoAlvo.Interno);
        await Salvar(ativo);
        await Salvar(inativo);
        await Salvar(outroPublico);

        await using var contexto = fixture.CriarContexto();
        var repositorio = new DestinatarioRepository(contexto);

        var soAtivos = (await repositorio.ListarAsync(PublicoAlvo.Suporte, incluirInativos: false))
            .Where(d => d.Nome.StartsWith(prefixo, StringComparison.Ordinal));
        var todos = (await repositorio.ListarAsync(PublicoAlvo.Suporte, incluirInativos: true))
            .Where(d => d.Nome.StartsWith(prefixo, StringComparison.Ordinal));

        soAtivos.Select(d => d.Id).Should().Equal(ativo.Id);
        todos.Select(d => d.Id).Should().Equal(ativo.Id, inativo.Id);
    }

    [Fact]
    public async Task SalvarAsync_sobre_destinatario_carregado_atualiza_sem_duplicar()
    {
        var destinatario = new Destinatario("Antes", EmailUnico(), PublicoAlvo.Cliente);
        await Salvar(destinatario);

        await using (var atualizacao = fixture.CriarContexto())
        {
            var repositorio = new DestinatarioRepository(atualizacao);
            var carregado = await repositorio.BuscarPorIdAsync(destinatario.Id);
            carregado!.Alterar("Depois", carregado.Email, PublicoAlvo.Comercial);
            carregado.Desativar();
            await repositorio.SalvarAsync(carregado);
        }

        await using var leitura = fixture.CriarContexto();
        var lidos = await new DestinatarioRepository(leitura).BuscarPorIdsAsync([destinatario.Id]);

        lidos.Should().ContainSingle();
        lidos[0].Nome.Should().Be("Depois");
        lidos[0].Publico.Should().Be(PublicoAlvo.Comercial);
        lidos[0].Ativo.Should().BeFalse();
    }
}
