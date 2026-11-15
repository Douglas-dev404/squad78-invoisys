import { useNavigate } from 'react-router-dom';
import { useCreateUser } from '../../hooks/useCreateUser';
import { Layout } from '../../components/Layout/Layout';
import { InputField } from '../../components/InputField/InputField';
import { PermissionOptionCard } from '../../components/PermissionOptionCard/PermissionOptionCard';
import { ROLE_LABELS } from '../../utils/constants';
import { SENHA_INICIAL_MINIMO } from '../../utils/validators';

const ROLE_OPTIONS = [
  {
    value: 'admin',
    label: ROLE_LABELS.admin,
    description: 'Acesso total, inclusive cadastrar e desativar usuários.',
  },
  {
    value: 'release_manager',
    label: ROLE_LABELS.release_manager,
    description: 'Processa, revisa, aprova e exporta comunicados de Release.',
  },
];

export function CreateUserPage() {
  const navigate = useNavigate();
  const {
    fullName,
    email,
    role,
    senhaInicial,
    fieldErrors,
    isSubmitting,
    submitError,
    clearSubmitError,
    handleFullNameChange,
    handleEmailChange,
    handleRoleChange,
    handleSenhaInicialChange,
    handleSubmit,
  } = useCreateUser({ onSuccess: () => navigate('/usuarios') });

  const onSubmit = (e) => {
    handleSubmit(e);
  };

  return (
    <Layout>
      <div className="max-w-4xl mx-auto space-y-lg">
        {/* Page Header */}
        <div>
          <h2 className="font-headline-lg text-headline-lg text-primary mb-xs">
            Novo Usuário
          </h2>
          <p className="font-body-md text-body-md text-on-surface-variant">
            Cadastre um novo colaborador, defina o papel e a senha inicial.
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
                <InputField
                  id="senhaInicial"
                  name="senhaInicial"
                  type="password"
                  label={`Senha inicial (mínimo ${SENHA_INICIAL_MINIMO} caracteres)`}
                  value={senhaInicial}
                  onChange={handleSenhaInicialChange}
                  required
                  error={fieldErrors.senhaInicial}
                  disabled={isSubmitting}
                  autoComplete="new-password"
                />
              </div>
            </div>

            <hr className="border-surface-variant/50" />

            {/* Papel */}
            <fieldset className="space-y-md" aria-describedby={fieldErrors.role ? 'role-error' : undefined}>
              <div>
                <legend className="font-headline-sm text-headline-sm text-on-surface mb-xs">
                  Papel <span className="text-error">*</span>
                </legend>
                <p className="font-body-sm text-body-sm text-on-surface-variant">
                  Define o que o usuário pode fazer no sistema.
                </p>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-sm">
                {ROLE_OPTIONS.map((option) => (
                  <PermissionOptionCard
                    key={option.value}
                    type="radio"
                    name="role"
                    value={option.value}
                    label={option.label}
                    description={option.description}
                    checked={role === option.value}
                    onChange={handleRoleChange}
                    disabled={isSubmitting}
                  />
                ))}
              </div>
              {fieldErrors.role && (
                <p id="role-error" role="alert" className="font-body-sm text-body-sm text-error">
                  {fieldErrors.role}
                </p>
              )}
            </fieldset>

            {/* Actions */}
            <div className="flex flex-col-reverse sm:flex-row justify-end gap-sm pt-md">
              <button
                type="button"
                onClick={() => navigate('/usuarios')}
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
