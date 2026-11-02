using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace InvoiSys.Application.Autenticacao;

internal static class GeradorTokenRedefinicao
{
    private const int TamanhoBytes = 32;

    public static (string Token, string Hash) Gerar()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TamanhoBytes));
        return (token, CalcularHash(token));
    }

    public static string CalcularHash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
