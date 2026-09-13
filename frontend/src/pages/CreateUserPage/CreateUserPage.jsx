import { useCreateUser } from '../../hooks/useCreateUser';
import { Layout } from '../../components/Layout/Layout';
import { InputField } from '../../components/InputField/InputField';
import { SelectField } from '../../components/SelectField/SelectField';
import { PermissionOptionCard } from '../../components/PermissionOptionCard/PermissionOptionCard';

const ROLE_OPTIONS = [
  { value: 'admin', label: 'Administrador' },
  { value: 'reviewer', label: 'Revisor' },
  { value: 'viewer', label: 'Visualizador' },
];

const EXTRA_PERMISSION_OPTIONS = [
  {
    key: 'systemLogs',
    label: 'Acesso a Logs do Sistema',
    description: 'Visualização do histórico de atividades.',
  },
  {
    key: 'apiSettings',
    label: 'Configurações de API',
    description: 'Gerenciamento de chaves e integrações.',
  },
  {
    key: 'teamManagement',
    label: 'Gestão de Equipes',
    description: 'Criar e editar grupos de usuários.',
  },
  {
    key: 'exportReports',
    label: 'Exportação de Relatórios',
    description: 'Download de dados em CSV/PDF.',
  },
];

export function CreateUserPage({ onLogout, onProfileClick, onNavigate, onCancel, onCreated }) {
  const {
    fullName,
    email,
    role,
    extraPermissions,
    fieldErrors,
    isSubmitting,
    submitError,
    clearSubmitError,
    handleFullNameChange,
    handleEmailChange,
    handleRoleChange,
    toggleExtraPermission,
    handleSubmit,
  } = useCreateUser({ onSuccess: onCreated });

  const onSubmit = (e) => {
    handleSubmit(e);
  };

  return (
    <Layout
      onLogout={onLogout}
      onProfileClick={onProfileClick}
      activeScreen="user-management"
      onNavigate={onNavigate}
    >
      <div className="max-w-4xl mx-auto space-y-lg">
        {/* Page Header */}
        <div>
          <h2 className="font-headline-lg text-headline-lg text-primary mb-xs">
            Novo Usuário
          </h2>
          <p className="font-body-md text-body-md text-on-surface-variant">
            Cadastre um novo colaborador e defina suas permissões de acesso.
          </p>
        </div>

        {/* Form Card */}
        <div className="bg-surface-container-lowest rounded-xl shadow-[0px_4px_12px_rgba(27,67,50,0.08)] border border-surface-variant/50 p-md md:p-xl">
          <form className="space-y-lg" onSubmit={onSubmit} noValidate>
            {submitError && (
              <div
                role="alert"
                className="bg-error-container border border-error rounded-lg p-sm flex items-start gap-sm"
              >
                <span
                  className="material-symbols-outlined text-error flex-shrink-0"
                  style={{ fontVariationSettings: "'FILL' 1" }}
                >
                  error
                </span>
                <div className="flex-1">
                  <p className="font-body-sm text-body-sm text-on-error-container">
                    {submitError}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={clearSubmitError}
                  className="text-error hover:opacity-70 transition-opacity"
                  aria-label="Fechar"
                >
                  <span className="material-symbols-outlined text-[18px]">
                    close
                  </span>
                </button>
              </div>
            )}

            {/* Personal Info Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-gutter">
              <InputField
                id="fullName"
                name="fullName"
                label="Nome Completo"
                value={fullName}
                onChange={handleFullNameChange}
                placeholder="Ex: João da Silva"
                required
                error={fieldErrors.fullName}
                disabled={isSubmitting}
              />
              <InputField
                id="email"
                name="email"
                type="email"
                label="E-mail Corporativo"
                value={email}
                onChange={handleEmailChange}
                placeholder="joao.silva@empresa.com"
                required
                error={fieldErrors.email}
                disabled={isSubmitting}
                autoComplete="email"
              />
              <div className="md:col-span-2">
                <SelectField
                  id="role"
                  name="role"
                  label="Cargo/Função"
                  value={role}
                  onChange={handleRoleChange}
                  options={ROLE_OPTIONS}
                  placeholder="Selecione um cargo"
                  required
                  error={fieldErrors.role}
                  disabled={isSubmitting}
                />
              </div>
            </div>

            <hr className="border-surface-variant/50" />

            {/* Permissions Section */}
            <div className="space-y-md">
              <div>
                <h3 className="font-headline-sm text-headline-sm text-on-surface mb-xs">
                  Permissões Rápidas
                </h3>
                <p className="font-body-sm text-body-sm text-on-surface-variant">
                  Selecione os módulos adicionais que este usuário poderá
                  acessar.
                </p>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-sm">
                {EXTRA_PERMISSION_OPTIONS.map((option) => (
                  <PermissionOptionCard
                    key={option.key}
                    label={option.label}
                    description={option.description}
                    checked={extraPermissions[option.key]}
                    onChange={() => toggleExtraPermission(option.key)}
                  />
                ))}
              </div>
            </div>

            {/* Actions */}
            <div className="flex flex-col-reverse sm:flex-row justify-end gap-sm pt-md">
              <button
                type="button"
                onClick={onCancel}
                disabled={isSubmitting}
                className="px-md py-sm rounded-lg border border-primary text-primary font-label-md text-label-md hover:bg-primary/5 transition-colors text-center disabled:opacity-60 disabled:cursor-not-allowed"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                aria-busy={isSubmitting}
                className="px-md py-sm rounded-lg bg-primary-container text-on-primary font-label-md text-label-md hover:bg-primary transition-colors text-center shadow-sm disabled:opacity-60 disabled:cursor-not-allowed"
              >
                {isSubmitting ? 'Cadastrando...' : 'Cadastrar Usuário'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Layout>
  );
}
