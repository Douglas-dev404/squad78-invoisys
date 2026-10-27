export function UserListItem({ user, isSelected, onClick }) {
  const isActive = user.status === 'active';

  return (
    <div
      onClick={onClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick?.();
        }
      }}
      className={`bg-surface-container-lowest rounded-xl p-sm flex items-center justify-between shadow-[0px_4px_12px_rgba(27,67,50,0.04)] cursor-pointer hover:bg-surface-container-low transition-colors ${
        isSelected ? 'border-2 border-primary-fixed' : 'border border-surface-variant/50'
      } ${!isActive ? 'opacity-75' : ''}`}
    >
      <div className="flex items-center gap-md min-w-0 flex-1">
        {user.avatarUrl ? (
          <img
            className="w-12 h-12 rounded-full object-cover border border-surface-variant flex-shrink-0"
            src={user.avatarUrl}
            alt={`Foto de perfil de ${user.name}`}
          />
        ) : (
          <div className="w-12 h-12 rounded-full bg-surface-container-high flex items-center justify-center text-on-surface-variant font-headline-sm text-headline-sm flex-shrink-0">
            {user.initials}
          </div>
        )}
        <div className="min-w-0">
          <h3 className="font-headline-sm text-headline-sm text-on-surface truncate">
            {user.name}
          </h3>
          <p className="font-body-sm text-body-sm text-on-surface-variant truncate">
            {user.email}
          </p>
        </div>
      </div>
      <div className="flex items-center gap-sm flex-shrink-0 ml-md">
        <div className="font-body-sm text-body-sm text-on-surface hidden sm:block whitespace-nowrap">
          {user.roleLabel}
        </div>
        {isActive ? (
          <span className="px-sm py-xs rounded-full bg-secondary-container text-on-secondary-container font-label-sm text-label-sm whitespace-nowrap">
            Ativo
          </span>
        ) : (
          <span className="px-sm py-xs rounded-full border border-outline text-outline font-label-sm text-label-sm whitespace-nowrap">
            Inativo
          </span>
        )}
      </div>
    </div>
  );
}
