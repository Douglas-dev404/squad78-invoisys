export function ProductivityChart({ data, isLoading }) {
  if (isLoading || !data) {
    return (
      <section className="bg-surface rounded-xl p-lg shadow-[0px_4px_12px_rgba(27,67,50,0.04)] mb-lg border border-outline-variant/30">
        <div className="h-64 bg-surface-container-lowest animate-pulse rounded-lg"></div>
      </section>
    );
  }

  const maxValue = Math.max(
    ...data.data.flatMap((d) => [d.formalized, d.inReview])
  );

  return (
    <section className="bg-surface rounded-xl p-lg shadow-[0px_4px_12px_rgba(27,67,50,0.04)] mb-lg border border-outline-variant/30">
      <div className="flex justify-between items-center mb-xl">
        <h3 className="font-headline-sm text-headline-sm text-primary">
          Produtividade da Semana
        </h3>
        <div className="flex gap-sm">
          <span className="flex items-center gap-xs font-body-sm text-body-sm text-on-surface-variant">
            <div className="w-3 h-3 rounded-full bg-primary-container"></div>
            Formalizados
          </span>
          <span className="flex items-center gap-xs font-body-sm text-body-sm text-on-surface-variant">
            <div className="w-3 h-3 rounded-full bg-secondary-container"></div>
            Em Revisão
          </span>
        </div>
      </div>
      <div className="relative h-64 flex items-end justify-between px-md gap-sm border-b border-outline-variant/20 pb-sm">
        <div className="absolute inset-0 flex flex-col justify-between pointer-events-none z-0 px-md pb-sm pt-4">
          <div className="w-full border-t border-outline-variant/10"></div>
          <div className="w-full border-t border-outline-variant/10"></div>
          <div className="w-full border-t border-outline-variant/10"></div>
          <div className="w-full border-t border-outline-variant/10"></div>
        </div>

        {data.data.map((dayData) => (
          <div
            key={dayData.day}
            className="relative z-10 flex-1 flex justify-center items-end gap-1 group"
          >
            <div
              style={{ height: `${(dayData.inReview / maxValue) * 100}%` }}
              className="w-8 bg-secondary-container rounded-t-md transition-all group-hover:opacity-80"
            ></div>
            <div
              style={{ height: `${(dayData.formalized / maxValue) * 100}%` }}
              className="w-8 bg-primary-container rounded-t-md transition-all group-hover:opacity-80"
            ></div>
            <span className="absolute -bottom-8 font-label-sm text-label-sm text-on-surface-variant">
              {dayData.day}
            </span>
          </div>
        ))}
      </div>
      <div className="mt-8 text-center text-on-surface-variant font-label-sm">
        Volume de cards processados por dia
      </div>
    </section>
  );
}
