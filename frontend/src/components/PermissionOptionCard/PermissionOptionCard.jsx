/**
 * Card selecionável com rótulo e descrição. `type="radio"` + `name` vira um seletor de opção única.
 */
export function PermissionOptionCard({ label, description, checked, onChange, type = 'checkbox', name, value, disabled = false }) {
  return (
    <label
      className={`flex items-start gap-sm p-sm rounded-lg border transition-colors cursor-pointer group has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary ${
        checked ? 'border-primary bg-primary/5' : 'border-surface-variant hover:bg-surface-container'
      } ${disabled ? 'cursor-not-allowed opacity-60' : ''}`}
    >
      <div className="flex items-center h-5">
        <input
          type={type}
          name={name}
          value={value}
          checked={checked}
          onChange={onChange}
          disabled={disabled}
          className={`w-4 h-4 text-primary bg-surface-container-lowest border-surface-variant focus:ring-primary/20 focus:ring-2 ${
            type === 'radio' ? '' : 'rounded'
          }`}
        />
      </div>
      <div className="flex flex-col">
        <span className="font-label-md text-label-md text-on-surface group-hover:text-primary transition-colors">
          {label}
        </span>
        <span className="font-body-sm text-body-sm text-on-surface-variant">{description}</span>
      </div>
    </label>
  );
}
