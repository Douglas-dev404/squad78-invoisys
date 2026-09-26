using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

/// <summary>
/// Porta de persistência de <see cref="ComunicadoExportado"/> — o histórico de
/// publicação. Agregado próprio: não vive dentro de <see cref="Release"/>, é registro de
/// auditoria que referencia a versão aprovada que o originou.
///
/// <b>Só inclui e consulta, nunca altera nem apaga</b>: o que já foi publicado
/// aconteceu, e reescrever esse histórico é exatamente o que a FK <c>RESTRICT</c> para
/// <c>versoes_comunicado</c> protege no banco.
///
/// O gate de revisão humana não é responsabilidade desta porta — ele já foi aplicado
/// antes: o construtor de <see cref="ComunicadoExportado"/> rejeita versão não aprovada,
/// então não existe instância inválida para chegar até aqui.
/// </summary>
public interface IComunicadoExportadoRepository
{
    /// <summary>Registra uma exportação nova. Reexportar é um evento novo, nunca update.</summary>
    Task AdicionarAsync(ComunicadoExportado exportado, CancellationToken cancellationToken = default);

    /// <summary>Busca uma exportação pela chave técnica. Devolve null se não existir.</summary>
    Task<ComunicadoExportado?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Histórico de exportações de uma Release, da mais recente para a mais antiga,
    /// opcionalmente só de um público. Devolve lista vazia se não houver nenhuma.
    /// </summary>
    Task<IReadOnlyList<ComunicadoExportado>> ListarPorReleaseAsync(
        Guid releaseId,
        PublicoAlvo? publico = null,
        CancellationToken cancellationToken = default);
}
