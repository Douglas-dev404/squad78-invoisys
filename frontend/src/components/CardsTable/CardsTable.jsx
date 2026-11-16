import { RELEASE_STATUS, formatarData } from '../../utils/releases';

export function CardsTable({ cards, isLoading, onRowClick }) {
  if (isLoading) {
    return (
      <section className="bg-surface-container-lowest rounded-xl shadow-[0px_4px_12px_rgba(27,67,50,0.08)] border border-surface-variant/50 overflow-hidden">
        <div className="px-md py-sm border-b border-surface-variant/30 flex justify-between items-center bg-surface-container-lowest">
          <h3 className="font-headline-sm text-headline-sm text-primary">
            Releases recentes
          </h3>
        </div>
        <div className="p-lg space-y-4">
          {[...Array(4)].map((_, i) => (
            <div
              key={i}
              className="h-12 bg-surface-container-lowest animate-pulse rounded-lg"
            ></div>
          ))}
        </div>
      </section>
    );
  }

  if (!cards || cards.data.length === 0) {
    return (
      <section className="bg-surface-container-lowest rounded-xl shadow-[0px_4px_12px_rgba(27,67,50,0.08)] border border-surface-variant/50 overflow-hidden">
        <div className="px-md py-sm border-b border-surface-variant/30 flex justify-between items-center bg-surface-container-lowest">
          <h3 className="font-headline-sm text-headline-sm text-primary">
            Releases recentes
          </h3>
        </div>
        <div className="p-lg text-center text-on-surface-variant font-body-md">
          Nenhuma Release encontrada
        </div>
      </section>
    );
  }

  return (
    <section className="bg-surface-container-lowest rounded-xl shadow-[0px_4px_12px_rgba(27,67,50,0.08)] border border-surface-variant/50 overflow-hidden">
      <div className="px-md py-sm border-b border-surface-variant/30 flex justify-between items-center bg-surface-container-lowest">
        <h3 className="font-headline-sm text-headline-sm text-primary">
          Releases recentes
        </h3>
        <button className="text-primary font-label-md text-label-md hover:underline flex items-center gap-xs">
          Ver todos{' '}
          <span className="material-symbols-outlined text-[16px]">
            arrow_forward
          </span>
        </button>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-outline-variant/20 bg-surface-bright">
              <th className="px-md py-sm font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider font-medium">
                Chave
              </th>
              <th className="px-md py-sm font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider font-medium">
                Título
              </th>
              <th className="px-md py-sm font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider font-medium">
                Status
              </th>
              <th className="px-md py-sm font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider font-medium text-right">
                Criada em
              </th>
            </tr>
          </thead>
          <tbody className="font-body-sm text-body-sm">
            {cards.data.map((card) => (
              <tr
                key={card.id}
                onClick={() => onRowClick?.(card)}
                onKeyDown={(e) => e.key === 'Enter' && onRowClick?.(card)}
                tabIndex={onRowClick ? 0 : undefined}
                className="border-b border-outline-variant/10 hover:bg-secondary-container/5 transition-colors cursor-pointer group focus-visible:outline-none focus-visible:bg-secondary-container/10 focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary"
              >
                <td className="px-md py-sm text-primary font-medium group-hover:underline">
                  {card.chave}
                </td>
                <td className="px-md py-sm text-on-surface">{card.titulo}</td>
                <td className="px-md py-sm">
                  <span
                    className={`inline-flex items-center px-2 py-1 rounded-full font-label-sm text-[11px] leading-tight font-semibold ${
                      RELEASE_STATUS[card.status]?.className ||
                      'bg-surface-container text-on-surface-variant'
                    }`}
                  >
                    {RELEASE_STATUS[card.status]?.label ?? card.status}
                  </span>
                </td>
                <td className="px-md py-sm text-on-surface-variant text-right">
                  {formatarData(card.criadoEm)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
