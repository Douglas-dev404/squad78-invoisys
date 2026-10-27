export function AlertError({ message, onDismiss, onRetry }) {
  return (
    <div
      role="alert"
      className="mb-lg bg-error-container border border-error rounded-xl p-md flex items-start justify-between gap-md shadow-[0px_4px_12px_rgba(186,26,26,0.1)]"
    >
      <div className="flex items-start gap-sm flex-1">
        <span
          className="material-symbols-outlined text-error flex-shrink-0 mt-0.5"
          style={{ fontVariationSettings: "'FILL' 1" }}
        >
          error
        </span>
        <div>
          <p className="font-body-md text-body-md text-on-error-container font-medium">
            Erro ao carregar dados
          </p>
          <p className="font-body-sm text-body-sm text-on-error-container opacity-80 mt-1">
            {message}
          </p>
        </div>
      </div>
      <div className="flex items-center gap-xs flex-shrink-0">
        {onRetry && (
          <button
            onClick={onRetry}
            className="px-sm py-xs rounded-lg font-label-md text-label-md text-error hover:bg-error/10 transition-colors"
          >
            Tentar novamente
          </button>
        )}
        {onDismiss && (
          <button
            onClick={onDismiss}
            className="w-8 h-8 rounded-full flex items-center justify-center text-error hover:bg-error/10 transition-colors"
            aria-label="Fechar"
          >
            <span className="material-symbols-outlined text-[20px]">close</span>
          </button>
        )}
      </div>
    </div>
  );
}
