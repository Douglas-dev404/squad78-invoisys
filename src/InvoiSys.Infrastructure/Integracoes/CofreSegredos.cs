using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace InvoiSys.Infrastructure.Integracoes;

/// <summary>
/// Cifra os segredos (token do Jira, chave da IA) antes de irem ao banco: AES-256-GCM, chave derivada por HKDF.
/// A chave vem de <c>Configuracoes:ChaveCriptografia</c> ou, na falta dela, de <c>Auth:Jwt:Chave</c>.
/// </summary>
public sealed class CofreSegredos
{
    private const string Prefixo = "v1:";
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;

    private readonly byte[] _chave;

    public CofreSegredos(IConfiguration configuracao)
    {
        var material = configuracao["Configuracoes:ChaveCriptografia"];
        if (string.IsNullOrWhiteSpace(material))
        {
            material = configuracao["Auth:Jwt:Chave"];
        }

        if (string.IsNullOrWhiteSpace(material))
        {
            throw new InvalidOperationException(
                "Sem chave para cifrar as integrações: defina Configuracoes__ChaveCriptografia ou Auth__Jwt__Chave.");
        }

        _chave = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            Encoding.UTF8.GetBytes(material),
            32,
            salt: null,
            info: Encoding.UTF8.GetBytes("invoisys-integracoes-v1"));
    }

    public string Cifrar(string textoPuro)
    {
        var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
        var dados = Encoding.UTF8.GetBytes(textoPuro);
        var cifrado = new byte[dados.Length];
        var tag = new byte[TamanhoTag];

        using var aes = new AesGcm(_chave, TamanhoTag);
        aes.Encrypt(nonce, dados, cifrado, tag);

        return Prefixo + Convert.ToBase64String([.. nonce, .. tag, .. cifrado]);
    }

    /// <summary>Devolve null quando o valor não decifra (chave trocada ou dado corrompido).</summary>
    public string? Decifrar(string valor)
    {
        if (!valor.StartsWith(Prefixo, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(valor[Prefixo.Length..]);
            if (bytes.Length < TamanhoNonce + TamanhoTag)
            {
                return null;
            }

            var nonce = bytes.AsSpan(0, TamanhoNonce);
            var tag = bytes.AsSpan(TamanhoNonce, TamanhoTag);
            var cifrado = bytes.AsSpan(TamanhoNonce + TamanhoTag);
            var dados = new byte[cifrado.Length];

            using var aes = new AesGcm(_chave, TamanhoTag);
            aes.Decrypt(nonce, cifrado, tag, dados);

            return Encoding.UTF8.GetString(dados);
        }
        catch (Exception exc) when (exc is FormatException or CryptographicException)
        {
            return null;
        }
    }
}
