import { Icon } from '../Icon/Icon';
import { UI_STRINGS } from '../../utils/constants';

/**
 * Componente apresentacional do cartão de confirmação de envio de e-mail.
 * Exibe o ícone de sucesso, textos explicativos, ação de reenvio e navegação.
 * Componente "burro": sem fetch, sem lógica de negócio interna.
 *
 * @param {Object} props
 * @param {string} props.email - E-mail para o qual o link foi enviado (exibição contextual)
 * @param {boolean} [props.isResending=false] - Estado de carregamento do reenvio
 * @param {boolean} [props.resendSuccess=false] - Indica reenvio bem-sucedido
 * @param {string|null} [props.resendError=null] - Mensagem de erro do reenvio
 * @param {Function} props.onResend - Callback para reenviar o link
 * @param {Function} props.onBackToLogin - Callback para retornar ao login
 */
export function EmailConfirmationCard({
  email,
  isResending = false,
  resendSuccess = false,
  resendError = null,
  onResend,
  onBackToLogin,
}) {
  return (
    <div className="flex flex-col items-center space-y-md">
      {/* Ícone de confirmação com variação FILL 1 (preenchido) */}
      <div className="w-16 h-16 rounded-full bg-primary-container/10 flex items-center justify-center mb-xs">
        <Icon
          name="check_circle"
          size={40}
          filled
          className="text-primary-container"
          ariaHidden={false}
          ariaLabel="E-mail enviado com sucesso"
        />
      </div>

      {/* Textos e ação de reenvio */}
      <div className="text-center space-y-xs">
        <p className="font-body-sm text-body-sm text-on-surface-variant">
          {UI_STRINGS.DIDNT_RECEIVE_EMAIL}
        </p>

        {resendSuccess ? (
          <p className="font-label-md text-label-md text-secondary">
            {UI_STRINGS.RESEND_SUCCESS_MESSAGE}
          </p>
        ) : (
          <button
            type="button"
            onClick={onResend}
            disabled={isResending}
            aria-busy={isResending}
            className="font-label-md text-label-md text-primary-container hover:text-tertiary-container transition-colors underline underline-offset-4 disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {isResending
              ? UI_STRINGS.RESENDING_LINK_LABEL
              : UI_STRINGS.RESEND_LINK_LABEL}
          </button>
        )}

        {resendError && (
          <p role="alert" className="font-label-sm text-label-sm text-error mt-1">
            {resendError}
          </p>
        )}
      </div>

      {/* Separador e link de retorno ao Login */}
      <footer className="w-full mt-lg pt-md border-t border-outline-variant/50 text-center">
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
