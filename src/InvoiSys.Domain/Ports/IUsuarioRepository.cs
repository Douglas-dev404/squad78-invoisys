using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

/// <summary>
/// Porta de persistência do agregado <see cref="Usuario"/> — quem faz login, revisa e
/// aprova. Agregado independente de <see cref="Release"/> (o vínculo por
/// <c>revisado_por</c>/<c>gerado_por</c> ainda é texto livre), então segue o mesmo
/// padrão de <see cref="IReleaseRepository"/>: busca e um único <c>SalvarAsync</c>.
///
/// Sem remoção de propósito: desligar alguém é <see cref="Usuario.Desativar"/>, não
/// DELETE — o nome dele continua nas revisões que já assinou.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Busca um usuário pela chave técnica. Devolve null se não existir.</summary>
    Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca pelo e-mail de login, inclusive usuário desativado — decidir se desativado
    /// pode entrar é regra da autenticação, não da porta. Devolve null se não existir.
    /// </summary>
    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste um usuário novo ou existente — mesma decisão de
    /// <see cref="IReleaseRepository.SalvarAsync"/>: quem distingue Add/Update é o
    /// rastreamento do EF, não a porta. E-mail repetido viola o índice único.
    /// </summary>
    Task SalvarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
