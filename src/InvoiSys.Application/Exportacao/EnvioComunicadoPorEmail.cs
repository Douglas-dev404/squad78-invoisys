using InvoiSys.Application.Revisao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Exportacao;

public sealed record ResultadoEnvio(
    ComunicadoExportado Exportacao,
    IReadOnlyList<EnvioComunicado> Envios);

public sealed class DestinatarioInvalidoParaEnvioException(string mensagem) : Exception(mensagem);

public sealed class EnvioComunicadoPorEmail(
    IReleaseRepository releases,
    IComunicadoExportadoRepository exportados,
    IDestinatarioRepository destinatarios,
    IEnvioComunicadoRepository envios,
    IUsuarioRepository usuarios,
    IEnvioEmail envioEmail)
{
    public async Task<ResultadoEnvio> EnviarAsync(
        string chaveRelease,
        PublicoAlvo publico,
        IReadOnlyCollection<Guid> destinatarioIds,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var remetente = await UsuarioResponsavel.ObterAtivoAsync(usuarios, usuarioId, cancellationToken);

        var release = await releases.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken)
            ?? throw new ReleaseNaoEncontradaException(chaveRelease);

        var versao = release.VersaoPara(publico)
            ?? throw new ReleaseSemItensProcessadosException(
                $"Release {chaveRelease} não tem comunicado gerado para o público {publico.ParaValor()}.");

        if (!versao.ProntaParaExportar)
        {
            throw new ReleaseNaoAprovadaException(
                $"A versão {publico.ParaValor()} não está aprovada: aprove antes de enviar.");
        }

        var escolhidos = await ValidarDestinatariosAsync(publico, destinatarioIds, cancellationToken);

        var html = RenderizadorHtml.Renderizar(versao);
        var exportacao = new ComunicadoExportado(
            versao,
            FormatoExportacao.Html,
            html,
            null,
            remetente.Nome,
            remetente.Id);
        await exportados.AdicionarAsync(exportacao, cancellationToken);

        var assunto = string.IsNullOrWhiteSpace(versao.TituloExecutivo)
            ? $"Release {release.ChaveJira}"
            : versao.TituloExecutivo;
        var texto = RenderizadorMarkdown.Renderizar(versao);

        var resultados = new List<EnvioComunicado>(escolhidos.Count);
        foreach (var destinatario in escolhidos)
        {
            try
            {
                await envioEmail.EnviarComunicadoAsync(
                    destinatario.Email, destinatario.Nome, assunto, html, texto, cancellationToken);
                resultados.Add(EnvioComunicado.Enviado(exportacao, destinatario));
            }
            catch (EnvioEmailException exc)
            {
                resultados.Add(EnvioComunicado.Falhou(exportacao, destinatario, exc.Message));
            }
        }

        await envios.AdicionarAsync(resultados, cancellationToken);

        if (resultados.All(r => r.Status == StatusEnvio.Falhou))
        {
            throw new EnvioEmailException(
                "Nenhum e-mail foi enviado: o provedor de e-mail falhou para todos os destinatários.");
        }

        return new ResultadoEnvio(exportacao, resultados);
    }

    private async Task<IReadOnlyList<Destinatario>> ValidarDestinatariosAsync(
        PublicoAlvo publico,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            throw new DestinatarioInvalidoParaEnvioException("Escolha pelo menos um destinatário.");
        }

        if (ids.Distinct().Count() != ids.Count)
        {
            throw new DestinatarioInvalidoParaEnvioException("A lista de destinatários tem id repetido.");
        }

        var encontrados = await destinatarios.BuscarPorIdsAsync(ids, cancellationToken);

        var faltando = ids.Except(encontrados.Select(d => d.Id)).ToList();
        if (faltando.Count > 0)
        {
            throw new DestinatarioInvalidoParaEnvioException(
                $"Destinatário não encontrado: {string.Join(", ", faltando)}.");
        }

        if (encontrados.FirstOrDefault(d => !d.Ativo) is { } inativo)
        {
            throw new DestinatarioInvalidoParaEnvioException(
                $"O destinatário {inativo.Email} está desativado.");
        }

        if (encontrados.FirstOrDefault(d => d.Publico != publico) is { } outroPublico)
        {
            throw new DestinatarioInvalidoParaEnvioException(
                $"O destinatário {outroPublico.Email} é do público {outroPublico.Publico.ParaValor()}, "
                + $"não {publico.ParaValor()}.");
        }

        return [.. encontrados.OrderBy(d => d.Nome).ThenBy(d => d.Email)];
    }
}
