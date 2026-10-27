using System.Runtime.CompilerServices;

namespace InvoiSys.Tests;

internal static class ConfiguracaoDeTestes
{
    internal const string ChaveJwt = "chave-de-teste-com-mais-de-32-caracteres!";

    [ModuleInitializer]
    internal static void Inicializar() =>
        Environment.SetEnvironmentVariable("Auth__Jwt__Chave", ChaveJwt);
}
