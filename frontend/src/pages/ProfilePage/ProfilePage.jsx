import { useProfile } from '../../hooks/useProfile';
import { Layout } from '../../components/Layout/Layout';
import { ProfileField } from '../../components/ProfileField/ProfileField';
import { AlertError } from '../../components/AlertError/AlertError';

function formatDate(isoDate) {
  if (!isoDate) return '—';
  return new Date(isoDate).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function ProfilePage({ onLogout, onProfileClick, onBack }) {
  const { profile, loading, error, clearError, refetch } = useProfile();

  return (
    <Layout onLogout={onLogout} onProfileClick={onProfileClick}>
      <header className="mb-lg">
        {onBack && (
          <button
            onClick={onBack}
            className="flex items-center gap-1 text-on-surface-variant hover:text-primary font-label-md text-label-md mb-sm transition-colors"
          >
            <span className="material-symbols-outlined text-[18px]">
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
        <div className="bg-surface-container-lowest rounded-xl border border-surface-variant/50 shadow-[0px_4px_12px_rgba(27,67,50,0.08)] p-lg">
          <div className="flex items-center gap-md mb-lg">
            <div className="w-20 h-20 rounded-full bg-surface-container animate-pulse" />
            <div className="flex-1 space-y-2">
              <div className="h-4 w-40 bg-surface-container animate-pulse rounded" />
              <div className="h-3 w-56 bg-surface-container animate-pulse rounded" />
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

      {!loading && !error && profile && (
        <section className="bg-surface-container-lowest rounded-xl border border-surface-variant/50 shadow-[0px_4px_12px_rgba(27,67,50,0.08)] p-lg max-w-2xl">
          {/* Cabeçalho do perfil */}
          <div className="flex items-center gap-md mb-lg pb-lg border-b border-surface-variant/30">
            <img
              src={profile.avatarUrl}
              alt={`Foto de perfil de ${profile.name}`}
              className="w-20 h-20 rounded-full object-cover border border-surface-variant/50"
            />
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
              value={formatDate(profile.createdAt)}
            />
            <ProfileField
              icon="schedule"
              label="Último acesso"
              value={formatDate(profile.lastAccessAt)}
            />
          </div>
        </section>
      )}
    </Layout>
  );
}
