import { useState } from 'react';
import { Layout } from '../../components/Layout/Layout';
import { Button } from '../../components/Button/Button';
import { ExportacaoModal } from '../../components/ExportacaoModal/ExportacaoModal';
import { SentHistoryFilters } from '../../components/SentHistoryFilters/SentHistoryFilters';
import { SentHistoryTable } from '../../components/SentHistoryTable/SentHistoryTable';
import { useExportacoes } from '../../hooks/useExportacoes';
import { buscarExportacao } from '../../services/exportacoes.service';
import { UI_STRINGS } from '../../utils/constants';

const CARD_CLASS =
  'rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]';

function Carregando() {
  return (
    <section role="status" aria-live="polite" className={`${CARD_CLASS} space-y-sm p-md`}>
      {[...Array(5)].map((_, i) => (
        <div key={i} className="h-10 animate-pulse rounded-lg bg-surface-container" />
      ))}
      <span className="sr-only">Carregando exportações…</span>
    </section>
  );
}

function Vazio({ comFiltro, onLimpar }) {
  return (
    <section role="status" className={`${CARD_CLASS} p-xl text-center`}>
      <p className="font-body-md text-body-md text-on-surface-variant">
        {comFiltro ? 'Nada encontrado para essa busca.' : 'Nenhuma exportação ainda.'}
      </p>
      {comFiltro && (
        <button
          type="button"
          onClick={onLimpar}
          className="mt-md rounded-lg border border-primary px-md py-sm font-label-md text-label-md text-primary hover:bg-primary/5"
        >
          Limpar filtros
        </button>
      )}
    </section>
  );
}

export function SentHistoryPage() {
  const historico = useExportacoes();
  const [previa, setPrevia] = useState(null);

  const verConteudo = async (item) => {
    setPrevia({ item, exportacao: null, carregando: true, erro: null });
    try {
      const detalhe = await buscarExportacao(item.id);
      setPrevia((atual) => (atual?.item.id === item.id ? { item, exportacao: detalhe, carregando: false, erro: null } : atual));
    } catch (err) {
      setPrevia((atual) =>
        atual?.item.id === item.id
          ? { item, exportacao: null, carregando: false, erro: err?.detail || err?.message || 'Não foi possível abrir o conteúdo.' }
          : atual
      );
    }
  };

  let conteudo;
  if (historico.carregando) {
    conteudo = <Carregando />;
  } else if (historico.erro) {
    conteudo = (
      <div
        role="alert"
        className="flex flex-col items-start gap-sm rounded-xl border border-error bg-error-container p-md sm:flex-row sm:items-center sm:justify-between"
      >
        <p className="font-body-md text-body-md text-on-error-container">{historico.erro}</p>
        <Button onClick={historico.recarregar}>{UI_STRINGS.RELEASES_RETRY}</Button>
      </div>
    );
  } else if (historico.total === 0) {
    conteudo = <Vazio comFiltro={historico.temFiltro} onLimpar={historico.limparFiltros} />;
  } else {
    conteudo = (
      <SentHistoryTable
        itens={historico.itens}
        total={historico.total}
        pagina={historico.pagina}
        totalPaginas={historico.totalPaginas}
        onPageChange={historico.irParaPagina}
        onVerConteudo={verConteudo}
      />
    );
  }

  return (
    <Layout>
      <div className="mx-auto w-full max-w-[1280px]">
        <header className="mb-lg">
          <h2 className="mb-xs font-headline-lg text-headline-lg text-on-surface">Histórico de exportações</h2>
          <p className="max-w-2xl text-body-sm text-on-surface-variant">
            Todos os comunicados aprovados que já foram exportados, do mais recente para o mais antigo.
          </p>
        </header>

        <SentHistoryFilters
          busca={historico.busca}
          publico={historico.publico}
          onBuscaChange={historico.setBusca}
          onPublicoChange={historico.setPublico}
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
          textoCarregando="Carregando o conteúdo…"
          onClose={() => setPrevia(null)}
        />
      )}
    </Layout>
  );
}
