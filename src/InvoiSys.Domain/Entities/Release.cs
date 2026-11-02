using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

public sealed class RevisaoHumanaObrigatoriaException(string mensagem) : Exception(mensagem);

public sealed class ReleaseSemItensProcessadosException(string mensagem) : Exception(mensagem);

public sealed class ItemNaoEncontradoException(string mensagem) : Exception(mensagem);

public sealed class ReleaseNaoEncontradaException(string chaveJira)
    : Exception($"Release {chaveJira} não encontrada.");

public sealed class Release
{
    private readonly List<HistoriaJira> _historias;
    private readonly List<VersaoComunicado> _versoes = [];
    private readonly List<ExecucaoPipeline> _execucoes = [];
    private readonly List<EventoRevisao> _eventosPendentes = [];

    private Release()
    {
        ChaveJira = string.Empty;
        _historias = [];
    }

    public Release(string chaveJira, IEnumerable<HistoriaJira> historias)
    {
        Id = Guid.NewGuid();
        ChaveJira = chaveJira;
        _historias = [.. historias];
    }

    public Guid Id { get; private init; }

    public string ChaveJira { get; private init; }

    public IReadOnlyList<HistoriaJira> Historias => _historias;

    public StatusPipeline Status { get; private set; } = StatusPipeline.Pendente;

    public IReadOnlyList<VersaoComunicado> Versoes => _versoes;

    public IReadOnlyList<ExecucaoPipeline> Execucoes => _execucoes;

    public IReadOnlyList<EventoRevisao> EventosPendentes => _eventosPendentes;

    public VersaoComunicado? VersaoCliente => VersaoPara(PublicoAlvo.Cliente);

    public string? TituloExecutivo => VersaoCliente?.TituloExecutivo;

    public string? ResumoExecutivo => VersaoCliente?.ResumoExecutivo;

    public IReadOnlyList<ItemComunicado> Itens => VersaoCliente?.Itens ?? [];

    public string? AprovadoPor => VersaoCliente?.RevisadoPor;

    public DateTimeOffset? AprovadoEm => VersaoCliente?.RevisadoEm;

    public bool ProntaParaExportar => VersaoCliente?.ProntaParaExportar == true;

    public VersaoComunicado? VersaoPara(PublicoAlvo publico) =>
        _versoes.FirstOrDefault(versao => versao.Publico == publico);

    public void LimparEventosPendentes() => _eventosPendentes.Clear();

    public void MarcarProcessando() => Status = StatusPipeline.Processando;

    public void GarantirQuePodeReprocessar(PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        if (VersaoPara(publico)?.Status == StatusRevisao.Aprovado)
        {
            throw new RevisaoHumanaObrigatoriaException(
                $"A versão {publico.ParaValor()} da Release {ChaveJira} já foi aprovada. "
                + "Reabra a revisão antes de reprocessar — a IA não sobrescreve conteúdo "
                + "aprovado por um humano.");
        }
    }

    public void AtualizarHistorias(IEnumerable<HistoriaJira> historias)
    {
        // Clear + AddRange, nunca reatribuir _historias: o EF rastreia esta instância de List.
        _historias.Clear();
        _historias.AddRange(historias);
    }

    public ExecucaoPipeline RegistrarExecucao(string? modeloLlm)
    {
        var execucao = new ExecucaoPipeline(Id, modeloLlm);
        _execucoes.Add(execucao);
        return execucao;
    }

    public VersaoComunicado ConcluirProcessamento(
        IEnumerable<ItemComunicado> itens,
        string tituloExecutivo,
        string resumoExecutivo,
        Usuario usuario,
        DateTimeOffset agora,
        PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        ExigirResponsavel(usuario);
        GarantirQuePodeReprocessar(publico);

        var versao = VersaoPara(publico);

        if (versao is null)
        {
            versao = new VersaoComunicado(Id, publico);
            _versoes.Add(versao);
        }

        versao.PreencherConteudo(itens, tituloExecutivo, resumoExecutivo);
        Status = StatusPipeline.AguardandoRevisao;

        RegistrarEvento(
            AcaoRevisao.Processar,
            versao,
            null,
            usuario,
            agora,
            new Dictionary<string, object?> { ["totalItens"] = versao.Itens.Count });

        return versao;
    }

    public void MarcarFalha() => Status = StatusPipeline.Falhou;

    public void Aprovar(
        Usuario revisor,
        DateTimeOffset agora,
        PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        var versao = VersaoPara(publico)
            ?? throw new ReleaseSemItensProcessadosException(
                $"Release {ChaveJira} não tem comunicado gerado para o público "
                + $"{publico.ParaValor()}.");

        if (Status == StatusPipeline.Falhou)
        {
            throw new RevisaoHumanaObrigatoriaException(
                $"Release {ChaveJira} está em status {Status.ParaValor()}, "
                + "não pode ser aprovada sem um pipeline concluído com sucesso.");
        }

        try
        {
            versao.Aprovar(revisor, agora);
        }
        catch (VersaoSemItensException exc)
        {
            throw new ReleaseSemItensProcessadosException(exc.Message);
        }
        catch (TransicaoDeStatusInvalidaException exc)
        {
            throw new RevisaoHumanaObrigatoriaException(exc.Message);
        }

        if (_versoes.All(v => v.Status == StatusRevisao.Aprovado))
        {
            Status = StatusPipeline.Aprovado;
        }

        RegistrarEvento(AcaoRevisao.Aprovar, versao, null, revisor, agora, null);
    }

    public void Reprovar(
        Usuario revisor,
        string motivo,
        DateTimeOffset agora,
        PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        var versao = VersaoPara(publico)
            ?? throw new ReleaseSemItensProcessadosException(
                $"Release {ChaveJira} não tem comunicado gerado para o público "
                + $"{publico.ParaValor()}.");

        versao.Reprovar(revisor, motivo, agora);
        Status = StatusPipeline.AguardandoRevisao;

        RegistrarEvento(
            AcaoRevisao.Reprovar,
            versao,
            null,
            revisor,
            agora,
            new Dictionary<string, object?> { ["motivo"] = motivo });
    }

    public ItemComunicado EditarItem(
        Guid itemId,
        string texto,
        Usuario usuario,
        DateTimeOffset agora)
    {
        ExigirResponsavel(usuario);
        var (versao, item) = LocalizarItem(itemId);
        var textoAnterior = item.TextoFinal;

        versao.EditarItem(item, texto);

        RegistrarEvento(
            AcaoRevisao.EditarItem,
            versao,
            item.Id,
            usuario,
            agora,
            new Dictionary<string, object?>
            {
                ["textoAnterior"] = textoAnterior,
                ["textoNovo"] = item.TextoFinal,
            });
        return item;
    }

    public ItemComunicado ExcluirItem(
        Guid itemId,
        Usuario usuario,
        DateTimeOffset agora,
        string? motivo = null)
    {
        ExigirResponsavel(usuario);
        var (versao, item) = LocalizarItem(itemId);

        versao.ExcluirItem(item, motivo);

        RegistrarEvento(
            AcaoRevisao.ExcluirItem,
            versao,
            item.Id,
            usuario,
            agora,
            new Dictionary<string, object?>
            {
                ["motivo"] = item.MotivoExclusao,
                ["texto"] = item.TextoFinal,
            });
        return item;
    }

    public ItemComunicado ReincluirItem(Guid itemId, Usuario usuario, DateTimeOffset agora)
    {
        ExigirResponsavel(usuario);
        var (versao, item) = LocalizarItem(itemId);

        versao.ReincluirItem(item);

        RegistrarEvento(
            AcaoRevisao.ReincluirItem,
            versao,
            item.Id,
            usuario,
            agora,
            new Dictionary<string, object?> { ["texto"] = item.TextoFinal });
        return item;
    }

    public PublicoAlvo PublicoDoItem(Guid itemId) => LocalizarItem(itemId).Versao.Publico;

    private (VersaoComunicado Versao, ItemComunicado Item) LocalizarItem(Guid itemId)
    {
        foreach (var versao in _versoes)
        {
            var item = versao.Itens.FirstOrDefault(i => i.Id == itemId);
            if (item is not null)
            {
                return (versao, item);
            }
        }

        throw new ItemNaoEncontradoException(
            $"Item {itemId} não encontrado na Release {ChaveJira}.");
    }

    public void Reabrir(
        Usuario usuario,
        DateTimeOffset agora,
        PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        ExigirResponsavel(usuario);

        var versao = VersaoPara(publico)
            ?? throw new ReleaseSemItensProcessadosException(
                $"Release {ChaveJira} não tem comunicado gerado para o público "
                + $"{publico.ParaValor()}.");

        versao.Reabrir();
        Status = StatusPipeline.AguardandoRevisao;

        RegistrarEvento(AcaoRevisao.Reabrir, versao, null, usuario, agora, null);
    }

    private static void ExigirResponsavel(Usuario? usuario)
    {
        if (usuario is null || !usuario.Ativo)
        {
            throw new ArgumentException(
                "A ação de revisão exige um usuário responsável identificado e ativo — "
                + "é a trilha de auditoria.",
                nameof(usuario));
        }
    }

    private void RegistrarEvento(
        AcaoRevisao acao,
        VersaoComunicado versao,
        Guid? itemId,
        Usuario usuario,
        DateTimeOffset agora,
        IReadOnlyDictionary<string, object?>? detalhe) =>
        _eventosPendentes.Add(new EventoRevisao(Id, versao.Id, itemId, acao, usuario.Id, agora, detalhe));
}
