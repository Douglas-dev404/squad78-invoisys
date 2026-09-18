import { Icon } from '../Icon/Icon';

/**
 * Componente de campo de entrada de formulário.
 * Acessível, sem manipulação direta de DOM, controlado inteiramente via props.
 * Suporta opcionalmente ícone posicionado à esquerda (startIcon).
 *
 * @param {Object} props
 * @param {string} props.id - Identificador único do input
 * @param {string} props.name - Nome do campo no formulário
 * @param {string} props.label - Rótulo visível para o usuário
 * @param {string} [props.type='text'] - Tipo do input (email, password, text, etc.)
 * @param {string} props.value - Valor atual do campo
 * @param {Function} props.onChange - Handler de alteração
 * @param {string} [props.placeholder] - Texto de dica
 * @param {boolean} [props.required=false] - Se o campo é obrigatório
 * @param {string} [props.autoComplete] - Sugestão de autocomplete do navegador
 * @param {string} [props.error] - Mensagem de erro de validação
 * @param {boolean} [props.disabled=false] - Se o campo está desabilitado
 * @param {string} [props.startIcon] - Nome do ícone Material Symbols à esquerda
 */
export function InputField({
  id,
  name,
  label,
  type = 'text',
  value,
  onChange,
  placeholder,
  required = false,
  autoComplete,
  error,
  disabled = false,
  startIcon,
}) {
  const errorId = error ? `${id}-error` : undefined;

  const basePaddingClass = startIcon ? 'pl-xl pr-sm py-sm' : 'px-4 py-3';

  return (
    <div className="flex flex-col gap-base">
      <label
        htmlFor={id}
        className="font-label-md text-label-md text-on-surface ml-0.5"
      >
        {label}
        {required && <span className="text-error ml-1" aria-hidden="true">*</span>}
      </label>

      <div className="relative">
        {startIcon && (
          <div
            className="absolute inset-y-0 left-0 pl-sm flex items-center pointer-events-none text-on-surface-variant"
            aria-hidden="true"
          >
            <Icon name={startIcon} size={20} />
          </div>
        )}

        <input
          id={id}
          name={name}
          type={type}
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          required={required}
          autoComplete={autoComplete}
          disabled={disabled}
          aria-invalid={Boolean(error)}
          aria-describedby={errorId}
          className={`w-full bg-surface-container-lowest border rounded-lg font-body-md text-body-md text-on-surface focus:outline-none focus:ring-2 transition-all shadow-sm disabled:opacity-60 disabled:cursor-not-allowed placeholder:text-outline ${basePaddingClass} ${
            error
              ? 'border-error focus:border-error focus:ring-error-container'
              : 'border-outline-variant focus:border-primary-container focus:ring-secondary-container/50'
          }`}
        />
      </div>

      {error && (
        <span
          id={errorId}
          role="alert"
          className="text-error font-label-sm text-label-sm ml-1 mt-0.5 animate-fadeIn"
        >
          {error}
        </span>
      )}
    </div>
  );
}
