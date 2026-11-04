import { useNavigate } from 'react-router-dom';
import { clearSession } from '../../utils/session';
import { SideNavBar } from '../SideNavBar/SideNavBar';
import { TopNavBar } from '../TopNavBar/TopNavBar';
import { useSidebarMinimize } from '../../hooks/useSidebarMinimize';

export function Layout({
  children,
  fullBleed = false,
}) {
  const navigate = useNavigate();
  const onLogout = () => { clearSession(); navigate('/login', { replace: true }); };
  const { isMinimized, toggle } = useSidebarMinimize();

  return (
    <div className="bg-surface-bright text-on-background min-h-screen">
      {/* Navigation */}
      <SideNavBar
        isMinimized={isMinimized}
        onToggleMinimize={toggle}
      />
      <TopNavBar
        onLogout={onLogout}
        onProfileClick={() => navigate('/perfil')}
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
