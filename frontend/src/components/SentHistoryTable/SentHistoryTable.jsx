import { Link } from 'react-router-dom';
import { formatarData } from '../../utils/releases';
import { PUBLICO_LABEL } from '../../utils/revisao';

const FORMATO_LABEL = { markdown: 'Markdown' };

/**
 * Tabela do histórico de exportações com paginação no servidor.
 * @param {Object} props
 * @param {Array<{ id, chaveRelease, tituloExecutivo, publico, formato, geradoPor, geradoEm }>} props.itens
 * @param {number} props.pagina
 * @param {number} props.totalPaginas
 * @param {number} props.total
 * @param {Function} props.onPaginaChange
 * @param {Function} props.onVerConteudo recebe o item da linha
 */
export function SentHistoryTable({ itens, pagina, totalPaginas, total, onPaginaChange, onVerConteudo }) {
  return (
    <section aria-label="Exportações" className="overflow-hidden rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px] border-collapse text-left text-body-sm">
          <thead className="bg-surface-bright text-label-sm text-on-surface-variant">
            <tr>
              <th scope="col" className="px-sm py-sm font-medium">Release</th>
              <th scope="col" className="px-sm py-sm font-medium">Título</th>
              <th scope="col" className="px-sm py-sm font-medium">Público</th>
              <th scope="col" className="px-sm py-sm font-medium">Formato</th>
              <th scope="col" className="px-sm py-sm font-medium">Gerado por</th>
              <th scope="col" className="px-sm py-sm font-medium">Gerado em</th>
              <th scope="col" className="px-sm py-sm text-right font-medium">Ações</th>
            </tr>
          </thead>
          <tbody>
            {itens.map((item) => (
              <tr key={item.id} className="border-t border-surface-variant/60 transition-colors hover:bg-secondary-container/5">
                <td className="whitespace-nowrap px-sm py-sm font-semibold">
                  <Link to={`/releases/${encodeURIComponent(item.chaveRelease)}`} className="text-primary hover:underline">
                    {item.chaveRelease}
                  </Link>
                </td>
                <td className="min-w-[180px] px-sm py-sm font-medium text-on-surface">{item.tituloExecutivo ?? '—'}</td>
                <td className="px-sm py-sm">
                  <span className="rounded-full bg-surface-container px-2 py-1 text-label-sm text-on-surface-variant">
                    {PUBLICO_LABEL[item.publico] ?? item.publico}
                  </span>
                </td>
                <td className="px-sm py-sm text-on-surface-variant">{FORMATO_LABEL[item.formato] ?? item.formato}</td>
                <td className="px-sm py-sm text-on-surface-variant">{item.geradoPor ?? '—'}</td>
                <td className="whitespace-nowrap px-sm py-sm text-on-surface-variant">{formatarData(item.geradoEm)}</td>
                <td className="px-sm py-sm text-right">
                  <button
                    type="button"
                    onClick={() => onVerConteudo(item)}
                    className="rounded-md px-sm py-xs font-label-md text-label-md text-primary transition-colors hover:bg-primary/5"
                  >
                    Ver conteúdo
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex flex-col gap-sm border-t border-surface-variant/60 px-sm py-sm text-body-sm text-on-surface-variant sm:flex-row sm:items-center sm:justify-between">
        <span aria-live="polite">Página {pagina} de {totalPaginas} · {total} {total === 1 ? 'exportação' : 'exportações'}</span>
        <nav aria-label="Páginas do histórico" className="flex items-center gap-xs">
          <button
            type="button"
            disabled={pagina <= 1}
            onClick={() => onPaginaChange(pagina - 1)}
            className="inline-flex items-center gap-1 rounded-lg px-sm py-xs hover:bg-surface-container disabled:cursor-not-allowed disabled:opacity-40"
          >
            <span aria-hidden="true" className="material-symbols-outlined text-[18px]">chevron_left</span>
            Anterior
          </button>
          <button
            type="button"
            disabled={pagina >= totalPaginas}
            onClick={() => onPaginaChange(pagina + 1)}
            className="inline-flex items-center gap-1 rounded-lg px-sm py-xs hover:bg-surface-container disabled:cursor-not-allowed disabled:opacity-40"
          >
            Próxima
            <span aria-hidden="true" className="material-symbols-outlined text-[18px]">chevron_right</span>
          </button>
        </nav>
      </div>
    </section>
  );
}
