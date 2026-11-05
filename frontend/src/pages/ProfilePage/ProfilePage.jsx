import { formatDateTime } from '../../utils/format';
import { useProfile } from '../../hooks/useProfile';
import { Layout } from '../../components/Layout/Layout';
import { ProfileField } from '../../components/ProfileField/ProfileField';
import { AlertError } from '../../components/AlertError/AlertError';

export function ProfilePage({ onLogout, onProfileClick, onBack, onNavigate }) {
  const { profile, loading, error, clearError, refetch } = useProfile();

  return (
    <Layout
      onLogout={onLogout}
      onProfileClick={onProfileClick}
      activeScreen="profile"
      onNavigate={onNavigate}
    >
      <header className="mb-lg">
        {onBack && (
          <button
            onClick={onBack}
            className="flex items-center gap-1 text-on-surface-variant hover:text-primary font-label-md text-label-md mb-sm transition-colors"
          >
            <span aria-hidden="true" className="material-symbols-outlined text-[18px]">
              arrow_back
            </span>
            Voltar
          </button>
        )}
        <h2 className="font-headline-lg text-headline-lg text-primary">
          Meu Perfil
        </h2>
        <p className="font-body-md text-body-md text-on-surface-variant mt-xs">
          Visualize suas informações de conta. Esses dados são apenas leitura.
        </p>
      </header>

      {error && (
        <AlertError message={error} onDismiss={clearError} onRetry={refetch} />
      )}

      {loading && (
        <div className="bg-surface-container-lowest rounded-xl border border-surface-variant/50 shadow-[0px_4px_12px_rgba(27,67,50,0.08)] p-sm sm:p-lg">
          <div className="flex flex-wrap items-center gap-sm mb-lg">
            <div className="w-20 h-20 rounded-full bg-surface-container animate-pulse" />
            <div className="flex-1 space-y-2">
              <div className="h-4 w-full max-w-40 bg-surface-container animate-pulse rounded" />
              <div className="h-3 w-full max-w-56 bg-surface-container animate-pulse rounded" />
            </div>
          </div>
          {[...Array(5)].map((_, i) => (
            <div
              key={i}
              className="h-12 bg-surface-container animate-pulse rounded-lg mb-2"
            />
          ))}
        </div>
      )}

      {!loading && !error && !profile && <p role="status">Nenhuma informação de perfil disponível.</p>}
      {!loading && !error && profile && (
        <section className="bg-surface-container-lowest rounded-xl border border-surface-variant/50 shadow-[0px_4px_12px_rgba(27,67,50,0.08)] p-sm sm:p-lg max-w-2xl">
          {/* Cabeçalho do perfil */}
          <div className="flex flex-wrap items-center gap-sm mb-lg pb-lg border-b border-surface-variant/30">
            {profile.avatarUrl ? <img
              src={profile.avatarUrl}
              alt={`Foto de perfil de ${profile.name}`}
              className="w-20 h-20 shrink-0 rounded-full object-cover border border-surface-variant/50"
            /> : <span aria-hidden="true" className="w-20 h-20 shrink-0 rounded-full bg-secondary-container text-on-secondary-container flex items-center justify-center text-headline-md">
              {profile.name.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toLocaleUpperCase('pt-BR')}
            </span>}
            <div>
              <h3 className="font-headline-sm text-headline-sm text-on-surface">
                {profile.name}
              </h3>
              <span className="inline-flex items-center mt-1 px-2 py-1 rounded-full bg-primary-container text-on-primary font-label-sm text-[11px] leading-tight font-semibold">
                {profile.roleLabel}
              </span>
            </div>
          </div>

          {/* Dados do perfil (somente leitura) */}
          <div>
            <ProfileField icon="mail" label="E-mail" value={profile.email} />
            <ProfileField
              icon="apartment"
              label="Departamento"
              value={profile.department}
            />
            <ProfileField icon="call" label="Telefone" value={profile.phone} />
            <ProfileField
              icon="location_on"
              label="Localização"
              value={profile.location}
            />
            <ProfileField
              icon="calendar_today"
              label="Conta criada em"
              value={formatDateTime(profile.createdAt)}
            />
            <ProfileField
              icon="schedule"
              label="Último acesso"
              value={formatDateTime(profile.lastAccessAt)}
            />
          </div>
        </section>
      )}
    </Layout>
  );
}
