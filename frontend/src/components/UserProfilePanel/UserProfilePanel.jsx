import { useState } from 'react';
import { BOTAO_PERIGO, BOTAO_SECUNDARIO, Dialogo } from '../Dialogo/Dialogo';
import { formatarData } from '../../utils/releases';

const TEXTO_DESATIVAR =
  'O usuário não conseguirá mais entrar. Se já estiver logado, a sessão atual vale até expirar (até 8 horas).';
const TOOLTIP_PROPRIA_CONTA = 'Você não pode desativar a própria conta.';

export function UserProfilePanel({ user, isSelf, onDeactivate, onClose, isSaving, saveError }) {
  const [confirmando, setConfirmando] = useState(false);

  if (!user) return null;

  const isActive = user.status === 'active';

  const confirmar = async () => {
    const ok = await onDeactivate();
    if (ok) setConfirmando(false);
  };

  return (
    <aside className="w-[400px] bg-surface-container-lowest border-l border-surface-variant shadow-[-4px_0px_24px_rgba(27,67,50,0.03)] flex flex-col h-full overflow-hidden flex-shrink-0">
      <div className="p-gutter border-b border-surface-variant/50 flex justify-between items-start">
        <h3 className="font-headline-sm text-headline-sm text-primary">Perfil do Usuário</h3>
        <button
          onClick={onClose}
          className="text-on-surface-variant hover:text-on-surface transition-colors"
          aria-label="Fechar painel"
        >
          <span className="material-symbols-outlined">close</span>
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-gutter flex flex-col gap-lg">
        <div className="flex flex-col items-center text-center gap-sm">
          <div className="w-24 h-24 rounded-full bg-surface-container-high flex items-center justify-center text-on-surface-variant font-headline-lg text-headline-lg border-4 border-surface shadow-sm">
            {user.initials}
          </div>
          <div>
            <h2 className="font-headline-md text-headline-md text-on-surface">{user.name}</h2>
            <p className="font-body-md text-body-md text-on-surface-variant">{user.email}</p>
            <p className="mt-xs font-body-sm text-body-sm text-on-surface">{user.roleLabel}</p>
            <span
              className={`inline-block mt-xs px-sm py-xs rounded-full font-label-sm text-label-sm ${
                isActive
                  ? 'bg-secondary-container text-on-secondary-container'
                  : 'border border-outline text-outline'
              }`}
            >
              {isActive ? 'Ativo' : 'Inativo'}
            </span>
            <p className="mt-sm font-body-sm text-body-sm text-on-surface-variant">
              Membro desde {formatarData(user.createdAt)}
            </p>
          </div>
        </div>

        {saveError && (
          <p role="alert" className="font-body-sm text-body-sm text-error">
            {saveError}
          </p>
        )}
      </div>

      {isActive && (
        <div className="p-md border-t border-surface-variant/50 bg-surface">
          <span className="block" title={isSelf ? TOOLTIP_PROPRIA_CONTA : undefined}>
            <button
              type="button"
              onClick={() => setConfirmando(true)}
              disabled={isSelf || isSaving}
              aria-describedby={isSelf ? 'desativar-propria-conta' : undefined}
              className={`w-full ${BOTAO_PERIGO}`}
            >
              Desativar
            </button>
          </span>
          {isSelf && (
            <p id="desativar-propria-conta" className="mt-xs text-center font-body-sm text-body-sm text-on-surface-variant">
              {TOOLTIP_PROPRIA_CONTA}
            </p>
          )}
        </div>
      )}

      {confirmando && (
        <Dialogo titulo="Desativar usuário" onClose={() => setConfirmando(false)} bloqueado={isSaving}>
          <div className="space-y-md">
            <p className="font-body-md text-body-md text-on-surface-variant">
              Desativar <strong className="text-on-surface">{user.name}</strong> ({user.email})?
            </p>
            <p className="font-body-md text-body-md text-on-surface">{TEXTO_DESATIVAR}</p>
            {saveError && (
              <p role="alert" className="font-body-sm text-body-sm text-error">
                {saveError}
              </p>
            )}
            <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
              <button type="button" onClick={() => setConfirmando(false)} disabled={isSaving} className={BOTAO_SECUNDARIO}>
                Cancelar
              </button>
              <button type="button" onClick={confirmar} disabled={isSaving} aria-busy={isSaving} className={BOTAO_PERIGO}>
                {isSaving ? 'Desativando…' : 'Desativar'}
              </button>
            </div>
          </div>
        </Dialogo>
      )}
    </aside>
  );
}
