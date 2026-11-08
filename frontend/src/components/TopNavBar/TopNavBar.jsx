import { useAuth } from '../../hooks/useAuth';
import { ROLE_LABELS } from '../../data/users.mock';
export function TopNavBar({ onLogout, onProfileClick, isMinimized = false }) {
  const { usuario } = useAuth();
  const initials = usuario?.name?.split(/\s+/).slice(0, 2).map(word => word[0]).join('');
  const handleLogout = () => {
    if (onLogout) {
      onLogout();
    }
  };

  return (
    <header
      className={`fixed top-0 bg-surface-container-lowest border-b border-surface-variant/50 z-40 transition-all duration-300 ${
        isMinimized ? 'ml-20 w-[calc(100%-80px)]' : 'ml-[260px] w-[calc(100%-260px)]'
      }`}
    >
      <div className="flex justify-end items-center w-full px-gutter h-16">
        {/* Actions */}
        <div className="flex items-center gap-sm">
          <button
            className="w-10 h-10 rounded-full flex items-center justify-center text-on-surface-variant hover:bg-surface-container-low transition-colors"
            aria-label="Notificações"
          >
            <span className="material-symbols-outlined">notifications</span>
          </button>
          <button
            className="w-10 h-10 rounded-full flex items-center justify-center text-on-surface-variant hover:bg-surface-container-low transition-colors"
            aria-label="Ajuda"
          >
            <span className="material-symbols-outlined">help_outline</span>
          </button>
          <button className="bg-primary text-on-primary px-sm py-xs rounded-lg font-label-md text-label-md hover:bg-primary/90 transition-colors ml-sm">
            Formalizar
          </button>
          <button
            onClick={handleLogout}
            className="w-10 h-10 rounded-full flex items-center justify-center text-on-surface-variant hover:bg-surface-container-low transition-colors ml-sm"
            aria-label="Sair da conta"
            title="Sair"
          >
            <span className="material-symbols-outlined">logout</span>
          </button>
          <div className="ml-sm pl-sm border-l border-surface-variant">
            <button
              onClick={onProfileClick}
              className="rounded-full focus:outline-none focus:ring-2 focus:ring-primary/40 transition-shadow"
              aria-label="Ver meu perfil"
              title="Meu perfil"
            >
              <span className="flex items-center gap-xs">
                <span className="text-right"><span className="block">{usuario?.name}</span><span className="block text-xs">{ROLE_LABELS[usuario?.role] || usuario?.role}</span></span>
                {usuario?.avatarUrl ? <img className="w-8 h-8 rounded-full object-cover" alt={usuario.name} src={usuario.avatarUrl} /> : <span className="w-8 h-8 rounded-full bg-primary text-on-primary flex items-center justify-center">{initials}</span>}
              </span>
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}
