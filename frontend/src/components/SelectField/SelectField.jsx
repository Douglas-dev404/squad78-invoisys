export function SelectField({
  id,
  name,
  label,
  value,
  onChange,
  options,
  placeholder = 'Selecione uma opção',
  required = false,
  error,
  disabled = false,
}) {
  const errorId = error ? `${id}-error` : undefined;

  return (
    <div className="flex flex-col gap-base">
      <label
        htmlFor={id}
        className="font-label-md text-label-md text-on-surface ml-0.5"
      >
        {label}
        {required && (
          <span className="text-error ml-1" aria-hidden="true">
            *
          </span>
        )}
      </label>

      <select
        id={id}
        name={name}
        value={value}
        onChange={onChange}
        required={required}
        disabled={disabled}
        aria-invalid={Boolean(error)}
        aria-describedby={errorId}
        className={`w-full bg-surface-container-lowest border rounded-lg font-body-md text-body-md text-on-surface focus:outline-none focus:ring-2 transition-all shadow-sm disabled:opacity-60 disabled:cursor-not-allowed px-4 py-3 ${
          error
            ? 'border-error focus:border-error focus:ring-error-container'
            : 'border-outline-variant focus:border-primary-container focus:ring-secondary-container/50'
        }`}
      >
        <option value="" disabled>
          {placeholder}
        </option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>

      {error && (
        <span
          id={errorId}
          role="alert"
          className="text-error font-label-sm text-label-sm ml-1 mt-0.5"
        >
          {error}
        </span>
      )}
    </div>
  );
}
