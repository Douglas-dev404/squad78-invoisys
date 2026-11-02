using System.Security.Claims;

namespace InvoiSys.Api.Autenticacao;

public static class UsuarioAutenticado
{
    public static bool TentarObterId(ClaimsPrincipal usuario, out Guid id) =>
        Guid.TryParse(usuario.FindFirstValue("sub"), out id);
}
