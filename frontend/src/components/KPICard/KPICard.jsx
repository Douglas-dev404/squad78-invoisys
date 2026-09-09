export function KPICard({ label, value, trend, icon, backgroundColor }) {
  return (
    <div
      className={`${backgroundColor} rounded-xl p-md shadow-[0px_4px_12px_rgba(27,67,50,0.08)] relative overflow-hidden group border border-primary/10`}
    >
      <div className="absolute right-0 top-0 w-32 h-32 bg-black/5 rounded-full blur-2xl -mr-10 -mt-10 transition-transform group-hover:scale-110"></div>
      <div className="relative z-10 flex flex-col h-full justify-between">
        <div className="flex justify-between items-start mb-lg">
          <span className="font-label-md text-label-md text-on-primary uppercase tracking-wider opacity-90">
            {label}
          </span>
          {icon && (
            <span
              className="material-symbols-outlined text-on-primary opacity-80"
              style={{ fontVariationSettings: "'FILL' 1" }}
            >
              {icon}
            </span>
          )}
        </div>
        <div>
          <span className="font-headline-lg text-headline-lg text-on-primary block">
            {value}
          </span>
          <span className="font-body-sm text-body-sm text-on-primary opacity-80 flex items-center gap-1 mt-1">
            {trend}
          </span>
        </div>
      </div>
    </div>
  );
}
