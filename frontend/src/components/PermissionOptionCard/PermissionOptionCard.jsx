export function PermissionOptionCard({ label, description, checked, onChange }) {
  return (
    <label className="flex items-start gap-sm p-sm rounded-lg border border-surface-variant hover:bg-surface-container transition-colors cursor-pointer group">
      <div className="flex items-center h-5">
        <input
          type="checkbox"
          checked={checked}
          onChange={onChange}
          className="w-4 h-4 text-primary bg-surface-container-lowest border-surface-variant rounded focus:ring-primary/20 focus:ring-2"
        />
      </div>
      <div className="flex flex-col">
        <span className="font-label-md text-label-md text-on-surface group-hover:text-primary transition-colors">
          {label}
        </span>
        <span className="font-body-sm text-body-sm text-on-surface-variant">
          {description}
        </span>
      </div>
    </label>
  );
}
