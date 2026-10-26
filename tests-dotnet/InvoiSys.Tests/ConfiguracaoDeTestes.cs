using System.Runtime.CompilerServices;

namespace InvoiSys.Tests;

internal static class ConfiguracaoDeTestes
{
    internal const string ChaveJwt = "chave-de-teste-com-mais-de-32-caracteres!";

    internal const string UrlRedefinicao = "http://localhost:5173/redefinir-senha";

    [ModuleInitializer]
    internal static void Inicializar()
    {
        Environment.SetEnvironmentVariable("Auth__Jwt__Chave", ChaveJwt);
        Environment.SetEnvironmentVariable("Auth__RecuperacaoSenha__UrlBase", UrlRedefinicao);
    }
}
