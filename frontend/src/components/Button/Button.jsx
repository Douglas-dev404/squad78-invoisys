import { Icon } from '../Icon/Icon';

/**
 * Componente Button reutilizável e com suporte a estado de loading.
 * @param {Object} props
 * @param {React.ReactNode} props.children - Conteúdo textual do botão
 * @param {'button' | 'submit' | 'reset'} [props.type='button'] - Tipo HTML do botão
 * @param {Function} [props.onClick] - Callback de clique
 * @param {boolean} [props.isLoading=false] - Indicador de carregamento assíncrono
 * @param {boolean} [props.disabled=false] - Estado desabilitado
 * @param {string} [props.icon] - Nome do ícone Material Symbols para exibir à direita
 * @param {string} [props.className=''] - Classes CSS adicionais
 * @param {boolean} [props.fullWidth=false] - Se ocupa 100% da largura do contêiner
 */
export function Button({
  children,
  type = 'button',
  onClick,
  isLoading = false,
  disabled = false,
  icon,
  className = '',
  fullWidth = false,
}) {
  const isActionBlocked = disabled || isLoading;

  return (
    <button
      type={type}
      onClick={onClick}
      disabled={isActionBlocked}
      aria-busy={isLoading}
      className={`rounded-lg px-4 py-3 font-label-md text-label-md transition-all duration-200 shadow-[0px_4px_12px_rgba(27,67,50,0.1)] flex items-center justify-center gap-2 select-none bg-primary-container text-on-primary hover:bg-tertiary-container active:scale-[0.99] disabled:opacity-70 disabled:cursor-not-allowed disabled:active:scale-100 ${
        fullWidth ? 'w-full' : ''
      } ${className}`}
    >
      {isLoading ? (
        <>
          <svg
            className="animate-spin -ml-1 mr-2 h-4 w-4 text-on-primary"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
          <span>{children}</span>
        </>
      ) : (
        <>
          <span>{children}</span>
          {icon && <Icon name={icon} size={18} />}
        </>
      )}
    </button>
  );
}
