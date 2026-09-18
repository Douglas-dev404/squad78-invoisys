import { InputField } from '../InputField/InputField';
import { Button } from '../Button/Button';
import { UI_STRINGS } from '../../utils/constants';

/**
 * Componente do formulário de login.
 * Totalmente controlado e sem regras de negócio ou fetches internos.
 *
 * @param {Object} props
 * @param {string} props.email - Valor do input de e-mail
 * @param {string} props.password - Valor do input de senha
 * @param {{ email?: string, password?: string }} [props.fieldErrors] - Erros específicos de cada campo
 * @param {boolean} [props.isLoading=false] - Indicador de carregamento/submissão
 * @param {Function} props.onSubmit - Handler de submissão do formulário
 * @param {Function} props.onEmailChange - Handler de alteração do e-mail
 * @param {Function} props.onPasswordChange - Handler de alteração da senha
 * @param {string} [props.forgotPasswordText] - Rótulo do link de recuperação
 * @param {string} [props.forgotPasswordUrl='#'] - URL ou rota de recuperação
 * @param {Function} [props.onForgotPasswordClick] - Handler de clique em esqueci minha senha
 */
export function LoginForm({
  email,
  password,
  fieldErrors = {},
  isLoading = false,
  onSubmit,
  onEmailChange,
  onPasswordChange,
  forgotPasswordText = UI_STRINGS.FORGOT_PASSWORD_LABEL,
  forgotPasswordUrl = '#',
  onForgotPasswordClick,
}) {
  return (
    <form
      onSubmit={onSubmit}
      noValidate
      className="flex flex-col gap-sm w-full"
    >
      <InputField
        id="email"
        name="email"
        type="email"
        label={UI_STRINGS.EMAIL_LABEL}
        placeholder={UI_STRINGS.EMAIL_PLACEHOLDER}
        value={email}
        onChange={onEmailChange}
        error={fieldErrors.email}
        required
        autoComplete="username"
        disabled={isLoading}
      />

      <div className="mt-2">
        <InputField
          id="password"
          name="password"
          type="password"
          label={UI_STRINGS.PASSWORD_LABEL}
          placeholder={UI_STRINGS.PASSWORD_PLACEHOLDER}
          value={password}
          onChange={onPasswordChange}
          error={fieldErrors.password}
          required
          autoComplete="current-password"
          disabled={isLoading}
        />
      </div>

      <div className="mt-4">
        <Button
          type="submit"
          isLoading={isLoading}
          icon="arrow_forward"
          fullWidth
        >
          {isLoading
            ? UI_STRINGS.SUBMITTING_BUTTON_LABEL
            : UI_STRINGS.SUBMIT_BUTTON_LABEL}
        </Button>
      </div>

      <footer className="mt-6 text-center">
        <a
          href={forgotPasswordUrl}
          onClick={onForgotPasswordClick}
          className="font-body-sm text-body-sm text-on-surface-variant hover:text-primary transition-colors duration-200 focus:outline-none focus:underline"
        >
          {forgotPasswordText}
        </a>
      </footer>
    </form>
  );
}
