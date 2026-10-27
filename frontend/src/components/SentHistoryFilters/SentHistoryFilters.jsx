const selectClassName =
  'h-10 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 text-body-sm text-on-surface focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/15';

export function SentHistoryFilters({ filters, onChange, onClear }) {
  return (
    <section
      aria-label="Filtros do histórico"
      className="mb-md rounded-xl border border-surface-variant/50 bg-surface-container-lowest p-md shadow-[0px_4px_12px_rgba(27,67,50,0.08)]"
    >
      <div className="grid grid-cols-1 gap-sm sm:grid-cols-2 xl:grid-cols-[minmax(180px,1.5fr)_repeat(3,minmax(140px,1fr))_auto] xl:items-end">
        <label className="block min-w-0">
          <span className="mb-2 block text-label-sm text-on-surface-variant">Buscar identificador</span>
          <span className="flex h-10 items-center gap-xs rounded-lg border border-outline-variant bg-surface-container-lowest px-3 focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/15">
            <span aria-hidden="true" className="material-symbols-outlined text-[18px] text-on-surface-variant">search</span>
            <input
              className="min-w-0 w-full border-0 bg-transparent p-0 text-body-sm text-on-surface placeholder:text-on-surface-variant focus:ring-0"
              type="search"
              placeholder="ID ou título da tarefa"
              value={filters.query}
              onChange={(event) => onChange('query', event.target.value)}
            />
          </span>
        </label>

        <label className="block min-w-0">
          <span className="mb-2 block text-label-sm text-on-surface-variant">Período</span>
          <select className={selectClassName} value={filters.period} onChange={(event) => onChange('period', event.target.value)}>
            <option value="7">Últimos 7 dias</option>
            <option value="30">Últimos 30 dias</option>
            <option value="90">Últimos 90 dias</option>
            <option value="all">Todo o período</option>
          </select>
        </label>

        <label className="block min-w-0">
          <span className="mb-2 block text-label-sm text-on-surface-variant">Tom da IA</span>
          <select className={selectClassName} value={filters.tone} onChange={(event) => onChange('tone', event.target.value)}>
            <option value="all">Todos os tons</option>
            <option value="Técnico">Técnico</option>
            <option value="Formal">Formal</option>
            <option value="Simplificado">Simplificado</option>
          </select>
        </label>

        <label className="block min-w-0">
          <span className="mb-2 block text-label-sm text-on-surface-variant">Status</span>
          <select className={selectClassName} value={filters.status} onChange={(event) => onChange('status', event.target.value)}>
            <option value="all">Todos os status</option>
            <option value="Enviado">Enviado</option>
            <option value="Pendente">Pendente</option>
            <option value="Falhou">Falhou</option>
          </select>
        </label>

        <button
          className="h-10 rounded-lg bg-surface-container px-sm text-body-sm font-medium text-on-surface transition-colors hover:bg-surface-container-high sm:self-end"
          type="button"
          onClick={onClear}
        >
          Limpar
        </button>
      </div>
    </section>
  );
}
