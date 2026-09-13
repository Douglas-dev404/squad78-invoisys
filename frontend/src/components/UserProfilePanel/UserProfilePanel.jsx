const PERMISSION_OPTIONS = [
  { key: 'admin', label: 'Administrador (Acesso Total)' },
  { key: 'reviewer', label: 'Revisor de Tarefas' },
  { key: 'viewer', label: 'Visualizador (Somente Leitura)' },
];

export function UserProfilePanel({
  user,
  draftPermissions,
  onTogglePermission,
  onSave,
  onToggleStatus,
  onClose,
  isSaving,
  saveError,
}) {
  if (!user || !draftPermissions) return null;

  const isActive = user.status === 'active';

  return (
    <aside className="w-[400px] bg-surface-container-lowest border-l border-surface-variant shadow-[-4px_0px_24px_rgba(27,67,50,0.03)] flex flex-col h-full overflow-hidden flex-shrink-0">
      {/* Panel Header */}
      <div className="p-gutter border-b border-surface-variant/50 flex justify-between items-start">
        <h3 className="font-headline-sm text-headline-sm text-primary">
          Perfil do Usuário
        </h3>
        <button
          onClick={onClose}
          className="text-on-surface-variant hover:text-on-surface transition-colors"
          aria-label="Fechar painel"
        >
          <span className="material-symbols-outlined">close</span>
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-gutter flex flex-col gap-lg">
        {/* Identity Section */}
        <div className="flex flex-col items-center text-center gap-sm">
          {user.avatarUrl ? (
            <img
              className="w-24 h-24 rounded-full object-cover border-4 border-surface shadow-sm"
              src={user.avatarUrl}
              alt={`Foto de perfil de ${user.name}`}
            />
          ) : (
            <div className="w-24 h-24 rounded-full bg-surface-container-high flex items-center justify-center text-on-surface-variant font-headline-lg text-headline-lg border-4 border-surface shadow-sm">
              {user.initials}
            </div>
          )}
          <div>
            <h2 className="font-headline-md text-headline-md text-on-surface">
              {user.name}
            </h2>
            <p className="font-body-md text-body-md text-on-surface-variant">
              {user.email}
            </p>
            <span
              className={`inline-block mt-xs px-sm py-xs rounded-full font-label-sm text-label-sm ${
                isActive
                  ? 'bg-secondary-container text-on-secondary-container'
                  : 'border border-outline text-outline'
              }`}
            >
              Status: {isActive ? 'Ativo' : 'Inativo'}
            </span>
          </div>
        </div>

        {/* Permissions Section */}
        <div className="bg-surface rounded-xl p-md border border-surface-variant/50">
          <h4 className="font-label-md text-label-md text-primary mb-sm uppercase tracking-wider">
            Permissões de Acesso
          </h4>
          <div className="flex flex-col gap-sm">
            {PERMISSION_OPTIONS.map((option) => (
              <label
                key={option.key}
                className="flex items-center gap-sm cursor-pointer group"
              >
                <input
                  type="checkbox"
                  checked={draftPermissions[option.key]}
                  onChange={() => onTogglePermission(option.key)}
                  className="w-5 h-5 rounded text-primary-container border-outline focus:ring-primary focus:ring-offset-0 bg-surface-container-lowest"
                />
                <span className="font-body-sm text-body-sm text-on-surface group-hover:text-primary transition-colors">
                  {option.label}
                </span>
              </label>
            ))}
          </div>
        </div>

        {/* Recent Activity Section */}
        <div>
          <h4 className="font-label-md text-label-md text-primary mb-sm uppercase tracking-wider">
            Últimas Atividades
          </h4>
          {user.activity.length === 0 ? (
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              Nenhuma atividade registrada.
            </p>
          ) : (
            <div className="relative border-l-2 border-surface-container-high ml-xs flex flex-col gap-md pl-sm pb-sm">
              {user.activity.map((entry) => (
                <div key={entry.id} className="relative">
                  <div className="absolute -left-[25px] top-1 w-3 h-3 rounded-full bg-surface-container-high border-2 border-surface-container-lowest" />
                  <p className="font-body-sm text-body-sm text-on-surface">
                    {entry.description}
                  </p>
                  <p className="font-label-sm text-label-sm text-on-surface-variant mt-1">
                    {entry.date}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>

        {saveError && (
          <p role="alert" className="font-body-sm text-body-sm text-error">
            {saveError}
          </p>
        )}
      </div>

      {/* Panel Footer Actions */}
      <div className="p-md border-t border-surface-variant/50 flex gap-sm bg-surface">
        <button
          onClick={onToggleStatus}
          disabled={isSaving}
          aria-busy={isSaving}
          className="flex-1 bg-surface-container-high text-on-surface font-label-md text-label-md py-sm rounded-lg hover:bg-surface-container-highest transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {isActive ? 'Desativar' : 'Ativar'}
        </button>
        <button
          onClick={onSave}
          disabled={isSaving}
          aria-busy={isSaving}
          className="flex-1 bg-primary text-on-primary font-label-md text-label-md py-sm rounded-lg hover:bg-primary/90 transition-colors shadow-sm disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {isSaving ? 'Salvando...' : 'Salvar'}
        </button>
      </div>
    </aside>
  );
}
