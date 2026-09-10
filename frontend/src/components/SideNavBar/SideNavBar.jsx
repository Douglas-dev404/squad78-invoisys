export function SideNavBar({ isMinimized = false, onToggleMinimize }) {
  const navItems = [
    { icon: 'dashboard', label: 'Dashboard', href: '#', active: true },
    { icon: 'rate_review', label: 'Review Queue', href: '#', active: false },
    { icon: 'history', label: 'Sent History', href: '#', active: false },
    { icon: 'receipt_long', label: 'System Logs', href: '#', active: false },
    { icon: 'group', label: 'User Management', href: '#', active: false },
    { icon: 'settings', label: 'Technical Settings', href: '#', active: false },
  ];

  return (
    <nav
      className={`fixed left-0 top-0 h-full bg-surface shadow-sm shadow-[0px_4px_12px_rgba(27,67,50,0.08)] z-50 transition-all duration-300 ${
        isMinimized ? 'w-20' : 'w-[260px]'
      }`}
    >
      <div className="flex flex-col h-full py-md">
        {/* Header with Brand and Minimize Button */}
        <div
          className={`flex items-center justify-center gap-xs mb-lg transition-all duration-300 ${
            isMinimized ? 'px-sm' : 'px-gutter'
          }`}
        >
          <div className="flex items-center gap-xs flex-1 justify-between">
            {!isMinimized && (
              <div className="flex items-center gap-xs flex-1">
                <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center text-on-primary flex-shrink-0">
                  <span
                    className="material-symbols-outlined"
                    style={{ fontVariationSettings: "'FILL' 1" }}
                  >
                    task_alt
                  </span>
                </div>
                <div className="flex-1">
                  <h1 className="font-headline-md text-headline-md font-bold text-primary">
                    Invoisys
                  </h1>
                  <p className="font-label-sm text-label-sm text-on-surface-variant">
                    Task Formalization
                  </p>
                </div>
              </div>
            )}
            {isMinimized && (
              <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center text-on-primary flex-shrink-0">
                <span
                  className="material-symbols-outlined"
                  style={{ fontVariationSettings: "'FILL' 1" }}
                >
                  task_alt
                </span>
              </div>
            )}
          </div>
          <button
            onClick={onToggleMinimize}
            className={`w-8 h-8 rounded-lg flex items-center justify-center text-on-surface-variant hover:bg-surface-container-low transition-colors flex-shrink-0 ${
              isMinimized ? 'hidden' : ''
            }`}
            aria-label={isMinimized ? 'Expandir' : 'Minimizar'}
            title={isMinimized ? 'Expandir' : 'Minimizar'}
          >
            <span className="material-symbols-outlined text-[20px]">
              {isMinimized ? 'chevron_right' : 'chevron_left'}
            </span>
          </button>
        </div>

        {/* Navigation Links */}
        <ul className={`flex flex-col flex-1 gap-xs transition-all duration-300 ${isMinimized ? 'px-1' : 'px-sm'}`}>
          {navItems.map((item) => (
            <li key={item.label}>
              <a
                href={item.href}
                className={`flex items-center gap-sm py-sm rounded-lg transition-colors duration-200 active:scale-[0.98] ${
                  isMinimized ? 'px-2 justify-center' : 'px-sm justify-start'
                } ${
                  item.active
                    ? 'text-primary font-bold border-r-4 border-primary bg-secondary-container/20 hover:bg-secondary-container/10'
                    : 'text-on-surface-variant hover:text-primary hover:bg-secondary-container/10'
                }`}
                title={isMinimized ? item.label : undefined}
              >
                <span className="material-symbols-outlined flex-shrink-0">
                  {item.icon}
                </span>
                {!isMinimized && (
                  <span className="font-body-md text-body-md whitespace-nowrap">
                    {item.label}
                  </span>
                )}
              </a>
            </li>
          ))}
        </ul>

        {/* CTA */}
        <div className={`transition-all duration-300 ${isMinimized ? 'px-1' : 'px-gutter'}`}>
          <button
            className="w-full bg-primary-container text-on-primary rounded-lg py-sm font-label-md text-label-md hover:opacity-90 transition-opacity active:scale-[0.98] shadow-[0px_4px_12px_rgba(27,67,50,0.08)] flex items-center justify-center gap-xs"
            title={isMinimized ? 'New Formalization' : undefined}
          >
            <span className="material-symbols-outlined text-[18px] flex-shrink-0">
              add
            </span>
            {!isMinimized && <span className="whitespace-nowrap">New Formalization</span>}
          </button>
        </div>
      </div>
    </nav>
  );
}
