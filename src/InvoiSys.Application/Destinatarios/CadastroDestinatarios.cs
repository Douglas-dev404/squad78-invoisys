using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Destinatarios;

public sealed class CadastroDestinatarios(IDestinatarioRepository destinatarios)
{
    public Task<IReadOnlyList<Destinatario>> ListarAsync(
        PublicoAlvo? publico,
        bool incluirInativos,
        CancellationToken cancellationToken = default) =>
        destinatarios.ListarAsync(publico, incluirInativos, cancellationToken);

    public async Task<Destinatario> CadastrarAsync(
        string nome,
        string email,
        PublicoAlvo publico,
        CancellationToken cancellationToken = default)
    {
        await GarantirEmailLivreAsync(email, publico, null, cancellationToken);

        var destinatario = new Destinatario(nome, email, publico);
        await destinatarios.SalvarAsync(destinatario, cancellationToken);
        return destinatario;
    }

    public async Task<Destinatario> AlterarAsync(
        Guid id,
        string nome,
        string email,
        PublicoAlvo publico,
        CancellationToken cancellationToken = default)
    {
        var destinatario = await BuscarAsync(id, cancellationToken);
        await GarantirEmailLivreAsync(email, publico, id, cancellationToken);

        destinatario.Alterar(nome, email, publico);
        await destinatarios.SalvarAsync(destinatario, cancellationToken);
        return destinatario;
    }

    public async Task DesativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var destinatario = await BuscarAsync(id, cancellationToken);
        destinatario.Desativar();
        await destinatarios.SalvarAsync(destinatario, cancellationToken);
    }

    public async Task ReativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var destinatario = await BuscarAsync(id, cancellationToken);
        destinatario.Reativar();
        await destinatarios.SalvarAsync(destinatario, cancellationToken);
    }

    private async Task<Destinatario> BuscarAsync(Guid id, CancellationToken cancellationToken) =>
        await destinatarios.BuscarPorIdAsync(id, cancellationToken)
            ?? throw new DestinatarioNaoEncontradoException(id);

    private async Task GarantirEmailLivreAsync(
        string email,
        PublicoAlvo publico,
        Guid? ignorarId,
        CancellationToken cancellationToken)
    {
        if (await destinatarios.ExisteEmailNoPublicoAsync(email, publico, ignorarId, cancellationToken))
        {
            throw new EmailDestinatarioDuplicadoException(email, publico);
        }
    }
}

public sealed class DestinatarioNaoEncontradoException(Guid id)
    : Exception($"Destinatário '{id}' não encontrado.");

public sealed class EmailDestinatarioDuplicadoException(string email, PublicoAlvo publico)
    : Exception($"O e-mail '{Usuario.NormalizarEmail(email)}' já está cadastrado para o público {publico.ParaValor()}.");
