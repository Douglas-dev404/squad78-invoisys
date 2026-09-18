import { InputField } from '../InputField/InputField';
import { Button } from '../Button/Button';
import { Icon } from '../Icon/Icon';
import { UI_STRINGS } from '../../utils/constants';

/**
 * Formulário apresentacional para recuperação de senha.
 * Componente "burro": recebe estado e callbacks via props, sem fetch interno.
 *
 * @param {Object} props
 * @param {string} props.email - Valor do input de e-mail
 * @param {Function} props.onEmailChange - Handler de alteração do input
 * @param {{ email?: string }} [props.fieldErrors] - Erros específicos de campo
 * @param {boolean} [props.isLoading=false] - Indicador de carregamento
 * @param {Function} props.onSubmit - Callback de submissão
 * @param {Function} props.onBackToLogin - Callback para navegar de volta ao Login
 * @param {boolean} [props.isSuccess=false] - Indica se o link já foi enviado
 */
export function ForgotPasswordForm({
  email,
  onEmailChange,
  fieldErrors = {},
  isLoading = false,
  onSubmit,
  onBackToLogin,
  isSuccess = false,
}) {
  return (
    <div className="w-full">
      <form onSubmit={onSubmit} noValidate className="space-y-md">
        <InputField
          id="email"
          name="email"
          type="email"
          label={UI_STRINGS.RECOVERY_EMAIL_LABEL}
          placeholder={UI_STRINGS.RECOVERY_EMAIL_PLACEHOLDER}
          value={email}
          onChange={onEmailChange}
          error={fieldErrors.email}
          required
          autoComplete="email"
          startIcon="mail"
          disabled={isLoading || isSuccess}
        />

        <Button
          type="submit"
          isLoading={isLoading}
          disabled={isSuccess}
          icon="arrow_forward"
          fullWidth
          className="shadow-sm shadow-primary-container/20 py-sm px-md active:scale-[0.98]"
        >
          {isLoading
            ? UI_STRINGS.RECOVERY_SUBMITTING_LABEL
            : UI_STRINGS.RECOVERY_SUBMIT_LABEL}
        </Button>
      </form>

      <footer className="mt-lg pt-md border-t border-outline-variant/50 text-center">
        <button
          type="button"
          onClick={onBackToLogin}
          className="inline-flex items-center gap-base font-label-md text-label-md text-primary-container hover:text-tertiary-container transition-colors group select-none cursor-pointer focus:outline-none focus:underline"
        >
          <Icon
            name="arrow_back"
            size={18}
            className="group-hover:-translate-x-1 transition-transform"
          />
          <span>{UI_STRINGS.BACK_TO_LOGIN_LABEL}</span>
        </button>
      </footer>
    </div>
  );
}
