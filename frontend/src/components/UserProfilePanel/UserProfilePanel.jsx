import { useEffect, useRef } from 'react';
import { Button } from '../Button/Button';
import { formatDateTime } from '../../utils/format';

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
  const dialog = useRef(null);
  useEffect(() => {
    const element = dialog.current;
    if (!element) return;
    const opener = document.activeElement;
    element.showModal();
    const previous = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => { element.close(); document.body.style.overflow = previous; opener?.focus(); };
  }, []);

  if (!user || !draftPermissions) return null;

  const isActive = user.status === 'active';

  return (
    <dialog ref={dialog} onCancel={onClose} onKeyDown={(event) => {
      if (event.key !== 'Tab') return;
      const controls = [...event.currentTarget.querySelectorAll('button:not(:disabled), input:not(:disabled), [tabindex="0"]')];
      const first = controls[0];
      const last = controls.at(-1);
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
    }} role="dialog" aria-modal="true" aria-labelledby="perfil-usuario-titulo"
      className="user-dialog bg-surface-container-lowest text-on-surface border border-surface-variant rounded-xl p-0">
      <div className="flex flex-col max-h-[calc(100dvh-2rem)]">
      {/* Panel Header */}
      <div className="p-sm sm:p-gutter border-b border-surface-variant/50 flex justify-between items-start">
        <h3 id="perfil-usuario-titulo" className="font-headline-sm text-headline-sm text-primary">
          Perfil do Usuário
        </h3>
        <button
          type="button" onClick={onClose}
          className="text-on-surface-variant hover:text-on-surface transition-colors"
          aria-label="Fechar painel"
        >
          <span aria-hidden="true" className="material-symbols-outlined">close</span>
        </button>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto p-sm sm:p-gutter flex flex-col gap-lg">
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
            <p className="font-body-md text-body-md text-on-surface-variant break-words">
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
                  id={`usuario-permissao-${option.key}`}
                  name={`permissao-${option.key}`}
                  type="checkbox"
                  disabled={isSaving}
                  checked={Boolean(draftPermissions[option.key])}
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
                    {formatDateTime(entry.date)}
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
        <Button onClick={onToggleStatus} isLoading={isSaving} className="flex-1 !bg-surface-container-high !text-on-surface">
          {isActive ? 'Desativar' : 'Ativar'}
        </Button>
        <Button onClick={onSave} isLoading={isSaving} className="flex-1">Salvar</Button>
      </div>
    </div>
    </dialog>
  );
}
