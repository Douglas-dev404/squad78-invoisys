import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROLE_LABELS } from '../../data/users.mock';
import { Layout } from '../../components/Layout/Layout';
import { ProfileField } from '../../components/ProfileField/ProfileField';

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

export function ProfilePage() {
  const navigate = useNavigate();
  const { usuario } = useAuth();
  const profile = usuario ? { ...usuario, roleLabel: ROLE_LABELS[usuario.role] } : null;

  return (
    <Layout>
      <header className="mb-lg">
        {(
          <button
            onClick={() => navigate(-1)}
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

      {profile && (
        <section className="bg-surface-container-lowest rounded-xl border border-surface-variant/50 shadow-[0px_4px_12px_rgba(27,67,50,0.08)] p-lg max-w-2xl">
          {/* Cabeçalho do perfil */}
          <div className="flex items-center gap-md mb-lg pb-lg border-b border-surface-variant/30">
            {profile.avatarUrl ? <img src={profile.avatarUrl} alt={profile.name} className="w-20 h-20 rounded-full object-cover" /> : <span className="w-20 h-20 rounded-full bg-primary text-on-primary flex items-center justify-center text-xl">{profile.name?.split(/\s+/).slice(0, 2).map(word => word[0]).join('')}</span>}
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
              icon="calendar_today"
              label="Conta criada em"
              value={formatDate(profile.createdAt)}
            />
          </div>
        </section>
      )}
    </Layout>
  );
}
