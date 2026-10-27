const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
});

const statusClassName = {
  Enviado: 'bg-secondary-container text-on-secondary-container',
  Pendente: 'bg-surface-container text-on-surface-variant',
  Falhou: 'bg-error-container text-error',
};

export function SentHistoryTable({ items, total, page, pageSize, onPageChange }) {
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, total);

  return (
    <section aria-label="Registros do histórico" className="overflow-hidden rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[780px] border-collapse text-left text-body-sm">
          <thead className="bg-surface-bright text-label-sm text-on-surface-variant">
            <tr>
              <th scope="col" className="px-md py-sm font-medium">ID da tarefa</th>
              <th scope="col" className="px-md py-sm font-medium">Título da tarefa</th>
              <th scope="col" className="px-md py-sm font-medium">Data e hora</th>
              <th scope="col" className="px-md py-sm font-medium">Tom da IA</th>
              <th scope="col" className="px-md py-sm font-medium">Status</th>
              <th scope="col" className="px-md py-sm text-right font-medium">Ações</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.id} className="border-t border-surface-variant/60 transition-colors hover:bg-secondary-container/5">
                <td className="whitespace-nowrap px-md py-sm font-semibold text-primary">{item.id}</td>
                <td className="min-w-[220px] px-md py-sm">
                  <span className="block font-medium text-on-surface">{item.title}</span>
                  <span className="block text-label-sm text-on-surface-variant">{item.detail}</span>
                </td>
                <td className="whitespace-nowrap px-md py-sm text-on-surface-variant">
                  {dateFormatter.format(new Date(item.sentAt))}
                </td>
                <td className="px-md py-sm">
                  <span className="rounded-full bg-surface-container px-2 py-1 text-label-sm text-on-surface-variant">{item.tone}</span>
                </td>
                <td className="px-md py-sm">
                  <span className={`inline-flex rounded-full px-2 py-1 text-label-sm font-semibold ${statusClassName[item.status]}`}>
                    {item.status}
                  </span>
                </td>
                <td className="px-md py-sm text-right">
                  <button type="button" disabled title="Disponível após integração com o histórico real" aria-label={`Ações de ${item.id} indisponíveis nesta prévia`} className="rounded-lg p-2 text-on-surface-variant opacity-50">
                    <span aria-hidden="true" className="material-symbols-outlined text-[20px]">more_horiz</span>
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {items.length === 0 && (
          <div className="px-md py-xl text-center text-body-sm text-on-surface-variant" role="status">
            Nenhum registro encontrado para esses filtros.
          </div>
        )}
      </div>

      <div className="flex flex-col gap-sm border-t border-surface-variant/60 px-md py-sm text-body-sm text-on-surface-variant sm:flex-row sm:items-center sm:justify-between">
        <span aria-live="polite">Mostrando {start} a {end} de {total} resultados</span>
        <nav aria-label="Páginas do histórico" className="flex items-center gap-1">
          <button type="button" aria-label="Página anterior" disabled={page === 1} onClick={() => onPageChange(page - 1)} className="rounded-lg p-2 hover:bg-surface-container disabled:cursor-not-allowed disabled:opacity-40">
            <span aria-hidden="true" className="material-symbols-outlined text-[18px]">chevron_left</span>
          </button>
          {Array.from({ length: pageCount }, (_, index) => index + 1).map((pageNumber) => (
            <button
              key={pageNumber}
              type="button"
              aria-label={`Página ${pageNumber}`}
              aria-current={page === pageNumber ? 'page' : undefined}
              onClick={() => onPageChange(pageNumber)}
              className={`h-9 min-w-9 rounded-lg px-2 ${page === pageNumber ? 'bg-primary text-on-primary' : 'hover:bg-surface-container'}`}
            >
              {pageNumber}
            </button>
          ))}
          <button type="button" aria-label="Próxima página" disabled={page === pageCount} onClick={() => onPageChange(page + 1)} className="rounded-lg p-2 hover:bg-surface-container disabled:cursor-not-allowed disabled:opacity-40">
            <span aria-hidden="true" className="material-symbols-outlined text-[18px]">chevron_right</span>
          </button>
        </nav>
      </div>
    </section>
  );
}
