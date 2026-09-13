import { Layout } from '../../components/Layout/Layout';

export function UnderConstructionPage({
  title,
  activeScreen,
  onNavigate,
  onLogout,
  onProfileClick,
}) {
  return (
    <Layout
      onLogout={onLogout}
      onProfileClick={onProfileClick}
      activeScreen={activeScreen}
      onNavigate={onNavigate}
    >
      <header className="mb-lg">
        <h2 className="font-headline-lg text-headline-lg text-primary">
          {title}
        </h2>
      </header>

      <section className="bg-surface-container-lowest rounded-xl border border-surface-variant/50 shadow-[0px_4px_12px_rgba(27,67,50,0.08)] p-xl flex flex-col items-center justify-center text-center min-h-[400px]">
        <div className="w-20 h-20 rounded-full bg-secondary-container flex items-center justify-center mb-md">
          <span className="material-symbols-outlined text-on-secondary-container text-[40px]">
            construction
          </span>
        </div>
        <h3 className="font-headline-sm text-headline-sm text-on-surface mb-2">
          Tela em manutenção
        </h3>
        <p className="font-body-md text-body-md text-on-surface-variant max-w-md">
          Esta funcionalidade está sendo gerada e estará disponível em breve.
          Volte mais tarde para conferir as novidades.
        </p>
      </section>
    </Layout>
  );
}
