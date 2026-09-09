import { useState } from 'react';
import { BrandLogoCard } from '../../components/BrandLogoCard/BrandLogoCard';
import { ForgotPasswordForm } from '../../components/ForgotPasswordForm/ForgotPasswordForm';
import { Alert } from '../../components/Alert/Alert';
import { usePasswordRecovery } from '../../hooks/usePasswordRecovery';
import { isValidEmail } from '../../utils/validators';
import { BRAND_LOGO_URL, UI_STRINGS } from '../../utils/constants';

/**
 * Página de Recuperação de Senha da Invoisys.
 * Orquestra o estado do formulário, feedbacks de envio e navegação.
 *
 * @param {Object} props
 * @param {Function} props.onNavigateToLogin - Callback para retornar à tela de Login
 * @param {Function} props.onNavigateToConfirmation - Callback para avançar para a tela de confirmação, recebendo o e-mail como argumento
 */
export function ForgotPasswordPage({ onNavigateToLogin, onNavigateToConfirmation }) {
  const [email, setEmail] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});

  const {
    loading: isSubmitting,
    error: recoveryError,
    requestReset,
    clearError,
  } = usePasswordRecovery();

  const handleEmailChange = (e) => {
    setEmail(e.target.value);
    if (fieldErrors.email) {
      setFieldErrors((prev) => ({ ...prev, email: undefined }));
    }
    if (recoveryError) clearError();
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!email || !email.trim()) {
      setFieldErrors({ email: 'O e-mail corporativo é obrigatório.' });
      return;
    }

    if (!isValidEmail(email)) {
      setFieldErrors({ email: 'Informe um endereço de e-mail corporativo válido.' });
      return;
    }

    try {
      await requestReset({ email });
      // Avança para a tela de confirmação passando o e-mail
      if (onNavigateToConfirmation) {
        onNavigateToConfirmation(email);
      }
      // TODO: Se react-router-dom estiver configurado:
      // navigate('/email-confirmacao', { state: { email } });
    } catch (err) {
      console.error('Falha na solicitação de recuperação de senha:', err);
    }
  };

  const handleBackToLogin = (e) => {
    e?.preventDefault?.();
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
        {/* Cartão principal do formulário */}
        <section
          aria-label="Formulário de recuperação de senha"
          className="bg-surface-container-lowest rounded-xl shadow-[0px_4px_12px_rgba(27,67,50,0.04)] border border-outline-variant/30 p-md md:p-lg"
        >
          {/* Cabeçalho com Logotipo e Instruções */}
          <header className="flex flex-col items-center mb-xl text-center">
            <BrandLogoCard logoUrl={BRAND_LOGO_URL} />

            <h1 className="font-headline-lg-mobile md:font-headline-lg text-headline-lg-mobile md:text-headline-lg text-primary mb-xs">
              {UI_STRINGS.RECOVERY_TITLE}
            </h1>
            <p className="font-body-md text-body-md text-on-surface-variant">
              {UI_STRINGS.RECOVERY_SUBTITLE}
            </p>
          </header>

          {/* Feedback de Erro */}
          {recoveryError && (
            <Alert
              type="error"
              message={recoveryError}
              onClose={clearError}
            />
          )}

          {/* Formulário desacoplado */}
          <ForgotPasswordForm
            email={email}
            onEmailChange={handleEmailChange}
            fieldErrors={fieldErrors}
            isLoading={isSubmitting}
            onSubmit={handleSubmit}
            onBackToLogin={handleBackToLogin}
            isSuccess={false}
          />
        </section>

        {/* Rodapé institucional com direitos autorais */}
        <footer className="mt-lg text-center font-body-sm text-body-sm text-on-surface-variant">
          {UI_STRINGS.COPYRIGHT_TEXT}
        </footer>
      </div>
    </main>
  );
}
