import { BrandLogoCard } from '../../components/BrandLogoCard/BrandLogoCard';
import { EmailConfirmationCard } from '../../components/EmailConfirmationCard/EmailConfirmationCard';
import { usePasswordRecovery } from '../../hooks/usePasswordRecovery';
import { BRAND_LOGO_URL, UI_STRINGS } from '../../utils/constants';

/**
 * Página de confirmação do envio do link de recuperação de senha.
 * Orquestra o reenvio e a navegação de retorno.
 *
 * @param {Object} props
 * @param {string} [props.email=''] - E-mail para o qual o link foi enviado
 * @param {Function} props.onNavigateToLogin - Callback para retornar à tela de Login
 */
export function EmailConfirmationPage({ email = '', onNavigateToLogin }) {
  const { isResending, resendSuccess, error: resendError, resendReset } = usePasswordRecovery();

  const handleResend = async () => {
    try {
      await resendReset({ email });
      // TODO: Registrar evento de analytics de reenvio no backend:
      // trackEvent('password_reset_resent', { email });
    } catch {
      // Erros são gerenciados pelo hook e exibidos via resendError
    }
  };

  const handleBackToLogin = () => {
    if (onNavigateToLogin) {
      onNavigateToLogin();
    } else {
      // TODO: Se react-router-dom estiver configurado:
      // navigate('/login');
      window.location.href = '/login';
    }
  };

  return (
    <main className="bg-pattern min-h-screen flex items-center justify-center font-body-md text-on-surface p-gutter md:p-lg">
      <div className="w-full max-w-md">
        {/* Cartão principal */}
        <section
          aria-label="Confirmação de envio de e-mail de recuperação"
          className="bg-surface-container-lowest rounded-xl shadow-[0px_4px_12px_rgba(27,67,50,0.04)] border border-outline-variant/30 p-md md:p-lg"
        >
          {/* Cabeçalho com Logotipo e Instruções */}
          <header className="flex flex-col items-center mb-xl text-center">
            <BrandLogoCard logoUrl={BRAND_LOGO_URL} />

            <h1 className="font-headline-lg-mobile md:font-headline-lg text-headline-lg-mobile md:text-headline-lg text-primary mb-xs">
              {UI_STRINGS.EMAIL_SENT_TITLE}
            </h1>
            <p className="font-body-md text-body-md text-on-surface-variant">
              {UI_STRINGS.EMAIL_SENT_SUBTITLE}
            </p>
          </header>

          {/* Card de confirmação com ícone, reenvio e retorno */}
          <EmailConfirmationCard
            email={email}
            isResending={isResending}
            resendSuccess={resendSuccess}
            resendError={resendError}
            onResend={handleResend}
            onBackToLogin={handleBackToLogin}
          />
        </section>

        {/* Rodapé institucional */}
        <footer className="mt-lg text-center font-body-sm text-body-sm text-on-surface-variant">
          {UI_STRINGS.COPYRIGHT_TEXT}
        </footer>
      </div>
    </main>
  );
}
