import { Icon } from '../Icon/Icon';

/**
 * Componente Alert para feedback acessível de erros e avisos.
 * @param {Object} props
 * @param {'error' | 'success' | 'info'} [props.type='error'] - Tipo visual do alerta
 * @param {string} props.message - Conteúdo da mensagem
 * @param {Function} [props.onClose] - Callback para fechar o alerta
 */
export function Alert({ type = 'error', message, onClose }) {
  if (!message) return null;

  const typeStyles = {
    error: 'bg-error-container text-on-error-container border-error/20',
    success: 'bg-secondary-container text-on-secondary-container border-secondary/20',
    info: 'bg-surface-container text-on-surface border-outline-variant',
  };

  const iconNames = {
    error: 'error',
    success: 'check_circle',
    info: 'info',
  };

  return (
    <div
      role="alert"
      className={`flex items-start justify-between gap-3 p-3 rounded-lg border text-body-sm font-body-sm transition-all duration-200 mb-4 ${typeStyles[type] || typeStyles.error}`}
    >
      <div className="flex items-center gap-2">
        <Icon name={iconNames[type] || 'info'} size={20} className="shrink-0" />
        <span>{message}</span>
      </div>
      {onClose && (
        <button
          type="button"
          onClick={onClose}
          aria-label="Fechar alerta"
          className="p-0.5 rounded hover:bg-black/10 transition-colors"
        >
          <Icon name="close" size={16} />
        </button>
      )}
    </div>
  );
}
