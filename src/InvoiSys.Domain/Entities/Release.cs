using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

/// <summary>Levantado quando algo tenta publicar uma Release sem aprovação humana.</summary>
public sealed class RevisaoHumanaObrigatoriaException(string mensagem) : Exception(mensagem);

/// <summary>Levantado quando se tenta aprovar uma Release cujo pipeline de IA não rodou.</summary>
public sealed class ReleaseSemItensProcessadosException(string mensagem) : Exception(mensagem);

/// <summary>Levantado quando o item pedido não existe em nenhuma versão desta Release.</summary>
public sealed class ItemNaoEncontradoException(string mensagem) : Exception(mensagem);

/// <summary>
/// Release — agregado raiz do domínio.
///
/// Dona do ciclo de vida do comunicado: recebe histórias brutas do Jira, guarda o
/// resultado do pipeline de IA (uma <see cref="VersaoComunicado"/> por público-alvo),
/// e só libera publicação depois de aprovação humana explícita. Essa regra vive aqui e
/// na versão, como método, não como `if` espalhado pela API ou pelo service — é a
/// garantia de que ninguém consegue publicar sem o gate, não importa por qual caminho
/// de código chegue até este objeto.
///
/// Por isso as coleções são expostas como somente-leitura e mutadas apenas pelos
/// métodos abaixo: um `List` público deixaria qualquer camada injetar itens sem
/// passar pelo ciclo de vida.
///
/// <b>Status vs revisão</b>: <see cref="Status"/> descreve a geração (a IA rodou?);
/// quem responde pela revisão é cada <see cref="VersaoComunicado"/>, porque o Cliente
/// pode estar aprovado enquanto o Suporte ainda está em ajuste.
///
/// Ver ADR-007 e ADR-008 em docs/decisoes-arquiteturais.md.
/// </summary>
public sealed class Release
{
    private readonly List<HistoriaJira> _historias;
    private readonly List<VersaoComunicado> _versoes = [];
    private readonly List<ExecucaoPipeline> _execucoes = [];

    /// <summary>Uso exclusivo do EF Core para materializar a entidade vinda do banco.</summary>
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

    /// <summary>Identidade técnica — chave estável para FK, independente da chave de negócio.</summary>
    public Guid Id { get; private init; }

    /// <summary>Ex: "RELEASE-2026-08".</summary>
    public string ChaveJira { get; private init; }

    public IReadOnlyList<HistoriaJira> Historias => _historias;

    /// <summary>Estado da <b>geração</b> por IA — não do gate de revisão humana.</summary>
    public StatusPipeline Status { get; private set; } = StatusPipeline.Pendente;

    /// <summary>Uma por público-alvo que já teve comunicado gerado.</summary>
    public IReadOnlyList<VersaoComunicado> Versoes => _versoes;

    /// <summary>
    /// Histórico de execuções do pipeline sobre esta Release. Vive no agregado para
    /// que a rastreabilidade seja garantida pelo domínio, não por a camada de
    /// aplicação lembrar de gravar o log.
    /// </summary>
    public IReadOnlyList<ExecucaoPipeline> Execucoes => _execucoes;

    /// <summary>
    /// Versão destinada ao Cliente — o público padrão do MVP, e o único obrigatório
    /// pelo enunciado. Conveniência de leitura para quem não precisa lidar com os
    /// demais públicos.
    /// </summary>
    public VersaoComunicado? VersaoCliente => VersaoPara(PublicoAlvo.Cliente);

    /// <summary>Título executivo da versão Cliente — atalho de compatibilidade.</summary>
    public string? TituloExecutivo => VersaoCliente?.TituloExecutivo;

    /// <summary>Resumo executivo da versão Cliente — atalho de compatibilidade.</summary>
    public string? ResumoExecutivo => VersaoCliente?.ResumoExecutivo;

    /// <summary>Itens da versão Cliente — atalho de compatibilidade.</summary>
    public IReadOnlyList<ItemComunicado> Itens => VersaoCliente?.Itens ?? [];

    public string? AprovadoPor => VersaoCliente?.RevisadoPor;

    public DateTimeOffset? AprovadoEm => VersaoCliente?.RevisadoEm;

    /// <summary>
    /// Verdadeiro quando a versão Cliente está aprovada. Para os demais públicos,
    /// consulte <see cref="VersaoComunicado.ProntaParaExportar"/> da versão específica
    /// — cada audiência tem seu próprio gate.
    /// </summary>
    public bool ProntaParaExportar => VersaoCliente?.ProntaParaExportar == true;

    public VersaoComunicado? VersaoPara(PublicoAlvo publico) =>
        _versoes.FirstOrDefault(versao => versao.Publico == publico);

    public void MarcarProcessando() => Status = StatusPipeline.Processando;

    /// <summary>
    /// Barra o reprocessamento de um público cuja versão já foi aprovada. O orquestrador
    /// chama isto <b>antes</b> de qualquer chamada externa: recusar depois de gastar
    /// Jira e tokens de LLM seria desperdício, e a tentativa recusada não deixa rastro
    /// (nenhuma execução chegou a rodar). O caminho para gerar de novo é
    /// <see cref="Reabrir"/>, uma decisão humana explícita.
    /// </summary>
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

    /// <summary>
    /// Reprocessamento: substitui as histórias vindas do Jira nesta rodada, mantendo
    /// Id, Versoes e Execucoes anteriores. Muda a lista no lugar (Clear + AddRange) em
    /// vez de trocar a referência de <c>_historias</c> — o EF Core rastreia a instância
    /// da List, então reatribuir a variável perderia o rastreamento das histórias
    /// antigas e o delete/insert em cascata não seria gerado corretamente no
    /// SaveChanges.
    /// </summary>
    public void AtualizarHistorias(IEnumerable<HistoriaJira> historias)
    {
        _historias.Clear();
        _historias.AddRange(historias);
    }

    /// <summary>
    /// Registra uma nova execução do pipeline sobre esta Release e a devolve para que
    /// o orquestrador marque conclusão ou falha. Uma Release pode ser reprocessada
    /// várias vezes; cada rodada vira uma linha, preservando o rastro das anteriores.
    /// </summary>
    public ExecucaoPipeline RegistrarExecucao(string? modeloLlm)
    {
        var execucao = new ExecucaoPipeline(Id, modeloLlm);
        _execucoes.Add(execucao);
        return execucao;
    }

    /// <summary>
    /// Chamado pelo orquestrador de IA ao fim do estágio 5 do pipeline, uma vez por
    /// público gerado. Deixa a versão pronta para revisão humana — nunca pronta para
    /// publicação direta.
    ///
    /// Reprocessar um público que já existe substitui o conteúdo daquela versão e a
    /// devolve para AguardandoRevisao; as versões dos outros públicos não são tocadas.
    /// Versão aprovada não é substituída (ver <see cref="GarantirQuePodeReprocessar"/>).
    /// </summary>
    public VersaoComunicado ConcluirProcessamento(
        IEnumerable<ItemComunicado> itens,
        string tituloExecutivo,
        string resumoExecutivo,
        PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        GarantirQuePodeReprocessar(publico);

        var versao = VersaoPara(publico);

        if (versao is null)
        {
            versao = new VersaoComunicado(Id, publico);
            _versoes.Add(versao);
        }

        versao.PreencherConteudo(itens, tituloExecutivo, resumoExecutivo);
        Status = StatusPipeline.AguardandoRevisao;

        return versao;
    }

    public void MarcarFalha() => Status = StatusPipeline.Falhou;

    /// <summary>
    /// Aprova a versão de um público. Delega para
    /// <see cref="VersaoComunicado.Aprovar"/> — o invariante de revisão humana mora
    /// lá, porque é por público que se aprova.
    ///
    /// A Release só passa a <see cref="StatusPipeline.Aprovado"/> quando <b>todas</b>
    /// as versões geradas estiverem aprovadas: enquanto qualquer audiência ainda
    /// estiver em revisão, o trabalho não acabou.
    /// </summary>
    public void Aprovar(
        string aprovadoPor,
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
            versao.Aprovar(aprovadoPor, agora);
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
    }

    /// <summary>
    /// O revisor considerou o resultado inaceitável para este público. A Release volta
    /// a AguardandoRevisao (ou permanece) — reprovar não é falha técnica de pipeline,
    /// é decisão humana, e o reprocessamento é o caminho natural a seguir.
    /// </summary>
    public void Reprovar(
        string revisadoPor,
        string motivo,
        DateTimeOffset agora,
        PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        var versao = VersaoPara(publico)
            ?? throw new ReleaseSemItensProcessadosException(
                $"Release {ChaveJira} não tem comunicado gerado para o público "
                + $"{publico.ParaValor()}.");

        versao.Reprovar(revisadoPor, motivo, agora);
        Status = StatusPipeline.AguardandoRevisao;
    }

    /// <summary>
    /// Revisão item a item. O item é procurado em todas as versões (o id é global), e a
    /// regra de quando ele pode mudar fica em <see cref="VersaoComunicado.EditarItem"/>.
    /// Devolve o item alterado para quem chamou montar a resposta.
    /// </summary>
    public ItemComunicado EditarItem(Guid itemId, string texto)
    {
        var (versao, item) = LocalizarItem(itemId);
        versao.EditarItem(item, texto);
        return item;
    }

    /// <inheritdoc cref="EditarItem"/>
    public ItemComunicado ExcluirItem(Guid itemId, string? motivo = null)
    {
        var (versao, item) = LocalizarItem(itemId);
        versao.ExcluirItem(item, motivo);
        return item;
    }

    /// <inheritdoc cref="EditarItem"/>
    public ItemComunicado ReincluirItem(Guid itemId)
    {
        var (versao, item) = LocalizarItem(itemId);
        versao.ReincluirItem(item);
        return item;
    }

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

    /// <summary>
    /// Devolve uma versão já aprovada para revisão — erro descoberto depois da
    /// aprovação, eventualmente depois de já ter exportado. A Release deixa de estar
    /// Aprovada enquanto isso não for resolvido.
    /// </summary>
    public void Reabrir(PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        var versao = VersaoPara(publico)
            ?? throw new ReleaseSemItensProcessadosException(
                $"Release {ChaveJira} não tem comunicado gerado para o público "
                + $"{publico.ParaValor()}.");

        versao.Reabrir();
        Status = StatusPipeline.AguardandoRevisao;
    }
}
