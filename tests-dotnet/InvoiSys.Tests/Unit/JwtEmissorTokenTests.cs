using System.Text;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InvoiSys.Tests.Unit;

public class JwtEmissorTokenTests
{
    private const string Chave = "chave-de-teste-com-mais-de-32-caracteres!";

    private static readonly DateTimeOffset Agora = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private static AuthOptions Opcoes(string chave = Chave, double horas = 8) => new()
    {
        Jwt = new JwtOptions { Chave = chave, Emissor = "invoisys", Audiencia = "invoisys-api", ExpiracaoHoras = horas },
    };

    private static JwtEmissorToken Emissor(FakeTimeProvider relogio, string chave = Chave, double horas = 8) =>
        new(Options.Create(Opcoes(chave, horas)), relogio);

    private static Usuario UmUsuario() => new("Ana Revisora", "ana@invoisys.com", "hash", "release_manager");

    private static TokenValidationParameters Parametros(string chave, DateTimeOffset agora) => new()
    {
        ValidIssuer = "invoisys",
        ValidAudience = "invoisys-api",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
        ClockSkew = TimeSpan.Zero,
        LifetimeValidator = (antes, expira, _, _) =>
            (antes is null || antes <= agora.UtcDateTime) && (expira is null || expira > agora.UtcDateTime),
    };

    private static Task<TokenValidationResult> Validar(string token, string chave, DateTimeOffset agora) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, Parametros(chave, agora));

    [Fact]
    public async Task Token_valido_carrega_sub_email_nome_e_papel()
    {
        var usuario = UmUsuario();
        var emitido = Emissor(new FakeTimeProvider(Agora)).Emitir(usuario);

        var resultado = await Validar(emitido.Token, Chave, Agora);

        resultado.IsValid.Should().BeTrue();
        var jwt = (JsonWebToken)resultado.SecurityToken;
        jwt.GetClaim("sub").Value.Should().Be(usuario.Id.ToString());
        jwt.GetClaim("email").Value.Should().Be("ana@invoisys.com");
        jwt.GetClaim("name").Value.Should().Be("Ana Revisora");
        jwt.GetClaim("role").Value.Should().Be("release_manager");
    }

    [Fact]
    public void ExpiraEm_segue_a_configuracao_de_horas()
    {
        Emissor(new FakeTimeProvider(Agora)).Emitir(UmUsuario()).ExpiraEm.Should().Be(Agora.AddHours(8));
        Emissor(new FakeTimeProvider(Agora), horas: 2).Emitir(UmUsuario()).ExpiraEm.Should().Be(Agora.AddHours(2));
    }

    [Fact]
    public async Task Token_expirado_e_rejeitado()
    {
        var emitido = Emissor(new FakeTimeProvider(Agora)).Emitir(UmUsuario());

        var resultado = await Validar(emitido.Token, Chave, Agora.AddHours(8).AddSeconds(1));

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Token_com_assinatura_de_outra_chave_e_rejeitado()
    {
        var emitido = Emissor(new FakeTimeProvider(Agora), chave: "outra-chave-bem-diferente-com-32-caracteres").Emitir(UmUsuario());

        var resultado = await Validar(emitido.Token, Chave, Agora);

        resultado.IsValid.Should().BeFalse();
    }
}
