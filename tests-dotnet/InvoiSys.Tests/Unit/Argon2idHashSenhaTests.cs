using FluentAssertions;
using InvoiSys.Infrastructure.Auth;

namespace InvoiSys.Tests.Unit;

public class Argon2idHashSenhaTests
{
    private readonly Argon2idHashSenha _hash = new();

    [Fact]
    public void Gerar_produz_string_PHC_com_os_parametros_documentados()
    {
        var hash = _hash.Gerar("senha-forte-123");

        hash.Should().StartWith("$argon2id$v=19$m=19456,t=2,p=1$");
        hash.Should().NotContain("senha-forte-123");
        hash.Length.Should().BeLessThan(255);
    }

    [Fact]
    public void Verificar_aceita_a_senha_certa()
    {
        var hash = _hash.Gerar("senha-forte-123");

        _hash.Verificar("senha-forte-123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verificar_rejeita_a_senha_errada()
    {
        var hash = _hash.Gerar("senha-forte-123");

        _hash.Verificar("senha-forte-124", hash).Should().BeFalse();
    }

    [Fact]
    public void Mesma_senha_gera_hashes_diferentes_por_causa_do_salt()
    {
        _hash.Gerar("igual").Should().NotBe(_hash.Gerar("igual"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-qualquer")]
    [InlineData("$argon2id$v=19$m=19456,t=2,p=1$@@@$@@@")]
    [InlineData("$bcrypt$v=19$m=19456,t=2,p=1$YWJj$YWJj")]
    public void Verificar_com_hash_malformado_devolve_false(string hashInvalido)
    {
        _hash.Verificar("qualquer", hashInvalido).Should().BeFalse();
    }
}
