import { SideNavBar } from '../SideNavBar/SideNavBar';
import { TopNavBar } from '../TopNavBar/TopNavBar';
import { useSidebarMinimize } from '../../hooks/useSidebarMinimize';

export function Layout({
  children,
  onLogout,
  onProfileClick,
  activeScreen,
  onNavigate,
  fullBleed = false,
}) {
  const { isMinimized, toggle } = useSidebarMinimize();

  return (
    <div className="bg-surface-bright text-on-background min-h-screen">
      {/* Navigation */}
      <SideNavBar
        isMinimized={isMinimized}
        onToggleMinimize={toggle}
        activeScreen={activeScreen}
        onNavigate={onNavigate}
      />
      <TopNavBar
        onLogout={onLogout}
        onProfileClick={onProfileClick}
        isMinimized={isMinimized}
      />

      {/* Main Content */}
      <main
        className={`mt-16 transition-all duration-300 ${
          isMinimized ? 'ml-20' : 'ml-[260px]'
        } ${
          fullBleed
            ? 'h-[calc(100vh-4rem)] overflow-hidden flex'
            : 'p-lg pb-xl'
        }`}
      >
        {children}
      </main>
    </div>
  );
}
