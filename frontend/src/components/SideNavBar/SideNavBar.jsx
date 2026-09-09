export function SideNavBar() {
  const navItems = [
    { icon: 'dashboard', label: 'Dashboard', href: '#', active: true },
    { icon: 'rate_review', label: 'Review Queue', href: '#', active: false },
    { icon: 'history', label: 'Sent History', href: '#', active: false },
    { icon: 'receipt_long', label: 'System Logs', href: '#', active: false },
    { icon: 'group', label: 'User Management', href: '#', active: false },
    { icon: 'settings', label: 'Technical Settings', href: '#', active: false },
  ];

  return (
    <nav className="fixed left-0 top-0 h-full w-[260px] bg-surface shadow-sm shadow-[0px_4px_12px_rgba(27,67,50,0.04)] z-50">
      <div className="flex flex-col h-full py-md">
        {/* Brand */}
        <div className="px-gutter mb-lg flex items-center gap-xs">
          <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center text-on-primary">
            <span
              className="material-symbols-outlined"
              style={{ fontVariationSettings: "'FILL' 1" }}
            >
              task_alt
            </span>
          </div>
          <div>
            <h1 className="font-headline-md text-headline-md font-bold text-primary">
              Invoisys
            </h1>
            <p className="font-label-sm text-label-sm text-on-surface-variant">
              Task Formalization
            </p>
          </div>
        </div>

        {/* Navigation Links */}
        <ul className="flex flex-col flex-1 px-sm gap-xs">
          {navItems.map((item) => (
            <li key={item.label}>
              <a
                href={item.href}
                className={`flex items-center gap-sm px-sm py-sm rounded-lg transition-colors duration-200 active:scale-[0.98] ${
                  item.active
                    ? 'text-primary font-bold border-r-4 border-primary bg-secondary-container/20 hover:bg-secondary-container/10'
                    : 'text-on-surface-variant hover:text-primary hover:bg-secondary-container/10'
                }`}
              >
                <span className="material-symbols-outlined">{item.icon}</span>
                <span className="font-body-md text-body-md">{item.label}</span>
              </a>
            </li>
          ))}
        </ul>

        {/* CTA */}
        <div className="px-gutter mt-auto">
          <button className="w-full bg-primary-container text-on-primary rounded-lg py-sm font-label-md text-label-md hover:opacity-90 transition-opacity active:scale-[0.98] shadow-[0px_4px_12px_rgba(27,67,50,0.04)] flex items-center justify-center gap-xs">
            <span className="material-symbols-outlined text-[18px]">add</span>
            New Formalization
          </button>
        </div>
      </div>
    </nav>
  );
}
