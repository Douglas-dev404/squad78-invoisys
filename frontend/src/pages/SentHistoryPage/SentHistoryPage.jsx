import { useCallback, useRef, useState } from 'react';
import { AlertError } from '../../components/AlertError/AlertError';
import { ExportacaoModal } from '../../components/ExportacaoModal/ExportacaoModal';
import { Layout } from '../../components/Layout/Layout';
import { SentHistoryFilters } from '../../components/SentHistoryFilters/SentHistoryFilters';
import { SentHistoryTable } from '../../components/SentHistoryTable/SentHistoryTable';
import { useExportacoes } from '../../hooks/useExportacoes';
import { buscarExportacao } from '../../services/exportacoes.service';
import { mensagemDeErro } from '../../utils/revisao';

const CARD_CLASS =
  'rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]';

function EstadoVazio({ comFiltro, onLimpar }) {
  return (
    <section className={`${CARD_CLASS} p-xl text-center`} role="status">
      <p className="text-body-md text-on-surface-variant">
        {comFiltro ? 'Nada encontrado para essa busca.' : 'Nenhuma exportação ainda.'}
      </p>
      {comFiltro && (
        <button
          type="button"
          onClick={onLimpar}
          className="mt-sm rounded-lg bg-surface-container px-sm py-xs text-body-sm font-medium text-on-surface hover:bg-surface-container-high"
        >
          Limpar filtros
        </button>
      )}
    </section>
  );
}

function Carregando() {
  return (
    <section role="status" aria-live="polite" className={`${CARD_CLASS} space-y-sm p-md`}>
      <span className="sr-only">Carregando exportações…</span>
      {[0, 1, 2].map((linha) => (
        <div key={linha} className="h-10 animate-pulse rounded-lg bg-surface-container" />
      ))}
    </section>
  );
}

export function SentHistoryPage() {
  const historico = useExportacoes();
  const [previa, setPrevia] = useState(null);
  const pedidoAtual = useRef(0);

  const verConteudo = useCallback(async (item) => {
    const pedido = ++pedidoAtual.current;
    setPrevia({ item, exportacao: null, carregando: true, erro: null });
    try {
      const detalhe = await buscarExportacao(item.id);
      if (pedido === pedidoAtual.current) setPrevia({ item, exportacao: detalhe, carregando: false, erro: null });
    } catch (err) {
      if (pedido === pedidoAtual.current) {
        setPrevia({ item, exportacao: null, carregando: false, erro: mensagemDeErro(err, 'Não foi possível abrir o conteúdo.') });
      }
    }
  }, []);

  const fecharPrevia = useCallback(() => {
    pedidoAtual.current += 1;
    setPrevia(null);
  }, []);

  let conteudo;
  if (historico.carregando) {
    conteudo = <Carregando />;
  } else if (historico.erro) {
    conteudo = <AlertError message={historico.erro} onRetry={historico.recarregar} />;
  } else if (historico.itens.length === 0) {
    conteudo = <EstadoVazio comFiltro={historico.temFiltro} onLimpar={historico.limparFiltros} />;
  } else {
    conteudo = (
      <SentHistoryTable
        itens={historico.itens}
        pagina={historico.pagina}
        totalPaginas={historico.totalPaginas}
        total={historico.total}
        onPaginaChange={historico.irParaPagina}
        onVerConteudo={verConteudo}
      />
    );
  }

  return (
    <Layout>
      <div className="mx-auto w-full max-w-[1280px]">
        <header className="mb-lg">
          <h2 className="mb-xs font-headline-lg text-headline-lg text-on-surface">Histórico de envios</h2>
          <p className="max-w-2xl text-body-sm text-on-surface-variant">
            Comunicados aprovados que já foram exportados, do mais recente para o mais antigo.
          </p>
        </header>

        <SentHistoryFilters
          filtros={historico.filtros}
          onBuscaChange={historico.mudarBusca}
          onPublicoChange={historico.mudarPublico}
          onClear={historico.limparFiltros}
        />
        {conteudo}
      </div>

      {previa && (
        <ExportacaoModal
          chave={previa.item.chaveRelease}
          publico={previa.item.publico}
          exportacao={previa.exportacao}
          exportando={previa.carregando}
          erro={previa.erro}
          onClose={fecharPrevia}
          mensagemCarregando="Carregando o conteúdo…"
        />
      )}
    </Layout>
  );
}
