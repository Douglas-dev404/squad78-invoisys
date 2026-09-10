import { SideNavBar } from '../SideNavBar/SideNavBar';
import { TopNavBar } from '../TopNavBar/TopNavBar';

export function Layout({ children, onLogout }) {
  return (
    <div className="bg-surface-bright text-on-background min-h-screen">
      {/* Navigation */}
      <SideNavBar />
      <TopNavBar onLogout={onLogout} />

      {/* Main Content */}
      <main className="ml-[260px] mt-16 p-lg pb-xl">
        {children}
      </main>
    </div>
  );
}
