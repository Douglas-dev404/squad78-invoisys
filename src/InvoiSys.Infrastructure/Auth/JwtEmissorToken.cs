using System.Text;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InvoiSys.Infrastructure.Auth;

public sealed class JwtEmissorToken(IOptions<AuthOptions> opcoes, TimeProvider relogio) : IEmissorToken
{
    private readonly JwtOptions _jwt = opcoes.Value.Jwt;

    public TokenEmitido Emitir(Usuario usuario)
    {
        var agora = relogio.GetUtcNow();
        var expiraEm = agora.AddHours(_jwt.ExpiracaoHoras);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Emissor,
            Audience = _jwt.Audiencia,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = usuario.Email,
                [JwtRegisteredClaimNames.Name] = usuario.Nome,
                ["role"] = usuario.Papel,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Chave)),
                SecurityAlgorithms.HmacSha256),
        };

        return new TokenEmitido(new JsonWebTokenHandler().CreateToken(descritor), expiraEm);
    }
}
