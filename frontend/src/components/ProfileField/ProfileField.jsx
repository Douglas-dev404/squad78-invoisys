export function ProfileField({ icon, label, value }) {
  return (
    <div className="flex items-start gap-sm py-sm border-b border-surface-variant/30 last:border-b-0">
      {icon && (
        <span className="material-symbols-outlined text-on-surface-variant flex-shrink-0 mt-0.5">
          {icon}
        </span>
      )}
      <div className="flex-1">
        <p className="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider">
          {label}
        </p>
        <p className="font-body-md text-body-md text-on-surface mt-1">
          {value || '—'}
        </p>
      </div>
    </div>
  );
}
