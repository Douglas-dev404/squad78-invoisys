using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure.Auth;

public sealed class AdminInicialSemeador(
    IUsuarioRepository usuarios,
    IHashSenha hashSenha,
    IOptions<AuthOptions> opcoes,
    ILogger<AdminInicialSemeador> logger)
{
    public async Task SemearAsync(CancellationToken cancellationToken = default)
    {
        var admin = opcoes.Value.AdminInicial;

        if (!admin.EstaConfigurado)
        {
            return;
        }

        if (await usuarios.ExisteAlgumAsync(cancellationToken))
        {
            return;
        }

        var usuario = new Usuario(
            admin.Nome.Trim(), admin.Email, hashSenha.Gerar(admin.Senha), Usuario.PapelAdmin);

        try
        {
            await usuarios.SalvarAsync(usuario, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Outra instância criou o admin entre a checagem e o INSERT (índice único em e-mail).
            logger.LogInformation("Admin inicial já criado por outra instância.");
            return;
        }

        logger.LogInformation("Admin inicial criado: {Email}.", usuario.Email);
    }
}
