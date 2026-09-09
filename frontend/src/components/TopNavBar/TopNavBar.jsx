export function TopNavBar() {
  return (
    <header className="fixed top-0 ml-[260px] w-[calc(100%-260px)] bg-surface-container-lowest border-b border-surface-variant/50 z-40">
      <div className="flex justify-between items-center w-full px-gutter h-16">
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
            Formalize
          </button>
          <div className="ml-sm pl-sm border-l border-surface-variant">
            <img
              className="w-8 h-8 rounded-full object-cover border border-surface-variant/50"
              alt="Foto de perfil"
              src="https://lh3.googleusercontent.com/aida-public/AB6AXuCKLcr-v89KoV9oSKV-9JPg_L-t1F-Sv8gJoP3E8f2mTajVf5NcsZVIjTJqB6HA9JzmG8r-v-LjiUp4GBRW_unG63RYRCjo38mRKsAxg-X4UmEBuOH72pM4xvWZcnZH19QUkCALyNgW_7oxl7JCsjtSTe471M2QlRoAVzaNV-mXUPFV5osGBDCwAmmops2g1TuYf8geKxyqWvZpGmKjsiG15IWDEoX6tHpMXRmV-g1hyz8ZCNzczIU69A"
            />
          </div>
        </div>
      </div>
    </header>
  );
}
