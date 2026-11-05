export function TopNavBar({ onLogout, onProfileClick, isMinimized = false,
  menuOpen, onToggleMenu, menuButtonRef }) {
  return (
    <header className={`fixed top-0 right-0 left-0 bg-surface-container-lowest border-b border-surface-variant z-40 ${
      isMinimized ? 'md:left-20' : 'md:left-[260px]'}`}>
      <div className="flex items-center justify-between md:justify-end gap-xs px-sm sm:px-gutter h-16">
        <button ref={menuButtonRef} type="button" onClick={onToggleMenu}
          aria-label={menuOpen ? 'Fechar menu' : 'Abrir menu'} aria-expanded={menuOpen}
          aria-controls="menu-principal" className="md:hidden p-2 rounded-lg">
          <span aria-hidden="true" className="material-symbols-outlined">{menuOpen ? 'close' : 'menu'}</span>
        </button>
        <div className="flex items-center gap-xs sm:gap-sm">
          <button type="button" disabled title="Notificações ainda indisponíveis" aria-label="Notificações indisponíveis" className="p-2 rounded-full opacity-60">
            <span aria-hidden="true" className="material-symbols-outlined">notifications</span>
          </button>
          <button type="button" onClick={onLogout} aria-label="Sair da conta" title="Sair" className="p-2 rounded-full hover:bg-surface-container">
            <span aria-hidden="true" className="material-symbols-outlined">logout</span>
          </button>
          <button type="button" onClick={onProfileClick} aria-label="Ver meu perfil" title="Meu perfil"
            className="w-10 h-10 rounded-full bg-primary text-on-primary flex items-center justify-center">
            <span aria-hidden="true" className="material-symbols-outlined">person</span>
          </button>
        </div>
      </div>
    </header>
  );
}
