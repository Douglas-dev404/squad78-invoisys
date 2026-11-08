using FluentAssertions;
using InvoiSys.Domain.Entities;

namespace InvoiSys.Tests.Unit;

public class TokenRedefinicaoSenhaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Emitir_expira_em_30_minutos_e_nasce_sem_uso()
    {
        var token = TokenRedefinicaoSenha.Emitir(Guid.NewGuid(), "hash", Agora);

        token.ExpiraEm.Should().Be(Agora.AddMinutes(30));
        token.UsadoEm.Should().BeNull();
        token.EstaValido(Agora).Should().BeTrue();
        token.EstaValido(Agora.AddMinutes(30)).Should().BeFalse();
    }

    [Fact]
    public void MarcarComoUsado_so_funciona_uma_vez()
    {
        var token = TokenRedefinicaoSenha.Emitir(Guid.NewGuid(), "hash", Agora);
        token.MarcarComoUsado(Agora);

        var acao = () => token.MarcarComoUsado(Agora);

        acao.Should().Throw<TokenRedefinicaoInvalidoException>();
    }

    [Fact]
    public void Emitir_sem_hash_e_rejeitado()
    {
        var acao = () => TokenRedefinicaoSenha.Emitir(Guid.NewGuid(), " ", Agora);

        acao.Should().Throw<ArgumentException>();
    }
}
