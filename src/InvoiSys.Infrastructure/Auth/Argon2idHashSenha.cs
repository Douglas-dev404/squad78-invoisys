using System.Security.Cryptography;
using System.Text;
using InvoiSys.Domain.Ports;
using Konscious.Security.Cryptography;

namespace InvoiSys.Infrastructure.Auth;

public sealed class Argon2idHashSenha : IHashSenha
{
    internal const int MemoriaKib = 19456;
    internal const int Iteracoes = 2;
    internal const int Paralelismo = 1;
    internal const int TamanhoSalt = 16;
    internal const int TamanhoHash = 32;
    private const int VersaoArgon2 = 19;

    public string Gerar(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Calcular(senha, salt, MemoriaKib, Iteracoes, Paralelismo, TamanhoHash);

        return $"$argon2id$v={VersaoArgon2}$m={MemoriaKib},t={Iteracoes},p={Paralelismo}" +
            $"${Base64Sem(salt)}${Base64Sem(hash)}";
    }

    public bool Verificar(string senha, string hash)
    {
        if (!TentarLer(hash, out var memoria, out var iteracoes, out var paralelismo, out var salt, out var esperado))
        {
            return false;
        }

        var calculado = Calcular(senha, salt, memoria, iteracoes, paralelismo, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    private static byte[] Calcular(
        string senha, byte[] salt, int memoriaKib, int iteracoes, int paralelismo, int tamanho)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(senha))
        {
            Salt = salt,
            MemorySize = memoriaKib,
            Iterations = iteracoes,
            DegreeOfParallelism = paralelismo,
        };
        return argon2.GetBytes(tamanho);
    }

    private static bool TentarLer(
        string hash,
        out int memoria,
        out int iteracoes,
        out int paralelismo,
        out byte[] salt,
        out byte[] esperado)
    {
        memoria = iteracoes = paralelismo = 0;
        salt = esperado = [];

        var partes = hash.Split('$');
        if (partes.Length != 6 || partes[0].Length != 0 || partes[1] != "argon2id"
            || partes[2] != $"v={VersaoArgon2}")
        {
            return false;
        }

        var parametros = partes[3].Split(',');
        if (parametros.Length != 3
            || !TentarNumero(parametros[0], "m=", out memoria)
            || !TentarNumero(parametros[1], "t=", out iteracoes)
            || !TentarNumero(parametros[2], "p=", out paralelismo))
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(Preencher(partes[4]));
            esperado = Convert.FromBase64String(Preencher(partes[5]));
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && esperado.Length > 0;
    }

    private static bool TentarNumero(string texto, string prefixo, out int valor)
    {
        valor = 0;
        return texto.StartsWith(prefixo, StringComparison.Ordinal)
            && int.TryParse(texto.AsSpan(prefixo.Length), out valor)
            && valor > 0;
    }

    private static string Base64Sem(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

    private static string Preencher(string base64) =>
        base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
}
