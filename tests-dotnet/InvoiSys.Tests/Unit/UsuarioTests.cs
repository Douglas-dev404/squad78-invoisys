using FluentAssertions;
using InvoiSys.Domain.Entities;

namespace InvoiSys.Tests.Unit;

public class UsuarioTests
{
    [Fact]
    public void Construtor_normaliza_o_email_com_trim_e_minusculas()
    {
        var usuario = new Usuario("Ana", "  Ana.Silva@InvoiSys.COM ", "hash", Usuario.PapelAdmin);

        usuario.Email.Should().Be("ana.silva@invoisys.com");
    }

    [Fact]
    public void NormalizarEmail_e_o_mesmo_criterio_do_cadastro()
    {
        Usuario.NormalizarEmail("  ANA@x.com\t").Should().Be("ana@x.com");
    }
}
