import { PUBLICO_LABEL } from '../../utils/revisao';

const selectClassName =
  'h-10 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 text-body-sm text-on-surface focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/15';

export function SentHistoryFilters({ busca, publico, onBuscaChange, onPublicoChange, onClear }) {
  return (
    <section
      aria-label="Filtros do histórico"
      className="mb-md rounded-xl border border-surface-variant/50 bg-surface-container-lowest p-md shadow-[0px_4px_12px_rgba(27,67,50,0.08)]"
    >
      <div className="grid grid-cols-1 gap-sm sm:grid-cols-[minmax(220px,2fr)_minmax(160px,1fr)_auto] sm:items-end">
        <label className="block min-w-0">
          <span className="mb-2 block text-label-sm text-on-surface-variant">Buscar</span>
          <span className="flex h-10 items-center gap-xs rounded-lg border border-outline-variant bg-surface-container-lowest px-3 focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/15">
            <span aria-hidden="true" className="material-symbols-outlined text-[18px] text-on-surface-variant">search</span>
            <input
              className="min-w-0 w-full border-0 bg-transparent p-0 text-body-sm text-on-surface placeholder:text-on-surface-variant focus:ring-0"
              type="search"
              placeholder="Chave ou título da Release"
              maxLength={200}
              value={busca}
              onChange={(event) => onBuscaChange(event.target.value)}
            />
          </span>
        </label>

        <label className="block min-w-0">
          <span className="mb-2 block text-label-sm text-on-surface-variant">Público</span>
          <select className={selectClassName} value={publico} onChange={(event) => onPublicoChange(event.target.value)}>
            <option value="">Todos os públicos</option>
            {Object.entries(PUBLICO_LABEL).map(([valor, rotulo]) => (
              <option key={valor} value={valor}>{rotulo}</option>
            ))}
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
