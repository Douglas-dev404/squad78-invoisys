import { SideNavBar } from '../SideNavBar/SideNavBar';
import { TopNavBar } from '../TopNavBar/TopNavBar';
import { useSidebarMinimize } from '../../hooks/useSidebarMinimize';

export function Layout({ children, onLogout, onProfileClick }) {
  const { isMinimized, toggle } = useSidebarMinimize();

  return (
    <div className="bg-surface-bright text-on-background min-h-screen">
      {/* Navigation */}
      <SideNavBar isMinimized={isMinimized} onToggleMinimize={toggle} />
      <TopNavBar
        onLogout={onLogout}
        onProfileClick={onProfileClick}
        isMinimized={isMinimized}
      />

      {/* Main Content */}
      <main
        className={`mt-16 p-lg pb-xl transition-all duration-300 ${
          isMinimized ? 'ml-20' : 'ml-[260px]'
        }`}
      >
        {children}
      </main>
    </div>
  );
}
