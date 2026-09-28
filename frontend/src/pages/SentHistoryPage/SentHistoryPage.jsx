import { useMemo, useState } from 'react';
import { Layout } from '../../components/Layout/Layout';
import { SentHistoryFilters } from '../../components/SentHistoryFilters/SentHistoryFilters';
import { SentHistoryTable } from '../../components/SentHistoryTable/SentHistoryTable';
import { SENT_HISTORY_MOCK, SENT_HISTORY_REFERENCE_TIME } from '../../data/sentHistory.mock';

const PAGE_SIZE = 4;
const INITIAL_FILTERS = { query: '', period: '7', tone: 'all', status: 'all' };

function normalize(value) {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('pt-BR');
}

export function SentHistoryPage({ onLogout, onProfileClick, onNavigate }) {
  const [filters, setFilters] = useState(INITIAL_FILTERS);
  const [page, setPage] = useState(1);

  const filteredItems = useMemo(() => {
    const query = normalize(filters.query.trim());
    const cutoff = filters.period === 'all'
      ? null
      : SENT_HISTORY_REFERENCE_TIME - Number(filters.period) * 86_400_000;

    return SENT_HISTORY_MOCK.filter((item) =>
      (!query || normalize(`${item.id} ${item.title}`).includes(query)) &&
      (!cutoff || new Date(item.sentAt).getTime() >= cutoff) &&
      (filters.tone === 'all' || item.tone === filters.tone) &&
      (filters.status === 'all' || item.status === filters.status)
    );
  }, [filters]);

  const onFilterChange = (name, value) => {
    setFilters((current) => ({ ...current, [name]: value }));
    setPage(1);
  };

  return (
    <Layout onLogout={onLogout} onProfileClick={onProfileClick} activeScreen="sent-history" onNavigate={onNavigate}>
      <div className="mx-auto w-full max-w-[1280px]">
        <header className="mb-lg flex flex-col gap-sm lg:flex-row lg:items-end lg:justify-between">
          <div>
            <div className="mb-xs flex items-center gap-xs">
              <h2 className="font-headline-lg text-headline-lg text-on-surface">Histórico de Envios</h2>
              <span className="rounded-full bg-secondary-container px-2 py-1 text-label-sm text-on-secondary-container">Prévia</span>
            </div>
            <p className="max-w-2xl text-body-sm text-on-surface-variant">
              Consulte os comunicados formalizados e acompanhe os registros de envio.
            </p>
            <p className="mt-1 text-label-sm text-on-surface-variant">Dados ilustrativos desta entrega visual.</p>
          </div>
          <button type="button" disabled title="Disponível após integração com o histórico real" className="inline-flex h-10 items-center justify-center gap-xs self-start rounded-lg border border-outline-variant px-sm text-body-sm font-medium text-on-surface-variant opacity-60 lg:self-auto">
            <span aria-hidden="true" className="material-symbols-outlined text-[18px]">download</span>
            Exportar log
          </button>
        </header>

        <SentHistoryFilters
          filters={filters}
          onChange={onFilterChange}
          onClear={() => { setFilters(INITIAL_FILTERS); setPage(1); }}
        />
        <SentHistoryTable
          items={filteredItems.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)}
          total={filteredItems.length}
          page={page}
          pageSize={PAGE_SIZE}
          onPageChange={setPage}
        />
      </div>
    </Layout>
  );
}
