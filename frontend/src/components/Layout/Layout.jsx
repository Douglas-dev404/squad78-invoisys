import { useState, useRef, useEffect } from 'react';
import { SideNavBar } from '../SideNavBar/SideNavBar';
import { TopNavBar } from '../TopNavBar/TopNavBar';
import { useSidebarMinimize } from '../../hooks/useSidebarMinimize';

export function Layout({ children, onLogout, onProfileClick, activeScreen, onNavigate, fullBleed = false }) {
  const { isMinimized, toggle } = useSidebarMinimize();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuButton = useRef(null);
  useEffect(() => {
    const desktop = window.matchMedia('(min-width: 768px)');
    const closeMobileMenu = (event) => { if (event.matches) setMenuOpen(false); };
    desktop.addEventListener('change', closeMobileMenu);
    return () => desktop.removeEventListener('change', closeMobileMenu);
  }, []);
  useEffect(() => {
    if (!menuOpen) return;
    function close(event) {
      if (event.key === 'Escape') { setMenuOpen(false); menuButton.current?.focus(); }
    }
    document.addEventListener('keydown', close);
    return () => document.removeEventListener('keydown', close);
  }, [menuOpen]);
  return (
    <div className="bg-surface-bright text-on-background min-h-screen">
      <a href="#conteudo-principal" className="skip-link">Ir para o conteúdo</a>
      <SideNavBar isMinimized={isMinimized} onToggleMinimize={toggle}
        mobileOpen={menuOpen} activeScreen={activeScreen}
        onNavigate={(screen) => { setMenuOpen(false); onNavigate?.(screen); }} />
      <TopNavBar onLogout={onLogout} onProfileClick={onProfileClick} isMinimized={isMinimized}
        menuOpen={menuOpen} menuButtonRef={menuButton} onToggleMenu={() => setMenuOpen((open) => !open)} />
      <main id="conteudo-principal" tabIndex={-1} inert={menuOpen}
        className={`min-w-0 mt-16 transition-all duration-300 ${isMinimized ? 'md:ml-20' : 'md:ml-[260px]'} ${
          fullBleed ? 'min-h-[calc(100dvh-4rem)] flex flex-col xl:flex-row' : 'p-sm sm:p-md lg:p-lg pb-xl'}`}>
        {children}
      </main>
    </div>
  );
}
