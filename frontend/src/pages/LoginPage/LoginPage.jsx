import { useState } from 'react';
import { BrandHeader } from '../../components/BrandHeader/BrandHeader';
import { LoginForm } from '../../components/LoginForm/LoginForm';
import { HeroSection } from '../../components/HeroSection/HeroSection';
import { Alert } from '../../components/Alert/Alert';
import { useAuth } from '../../hooks/useAuth';
import { useHeroHighlights } from '../../hooks/useHeroHighlights';
import { validateLoginForm } from '../../utils/validators';
import { BRAND_NAME, BRAND_DEFAULT_ICON, UI_STRINGS } from '../../utils/constants';

/**
 * Página de Login da plataforma Invoisys.
 * Responsável pela orquestração dos dados, hooks e composição visual.
 *
 * @param {Object} props
 * @param {Function} [props.onNavigateToForgotPassword] - Callback para navegar até a tela de recuperação de senha
 * @param {Function} [props.onLoginSuccess] - Callback para navegar ao dashboard após login bem-sucedido
 */
export function LoginPage({ onNavigateToForgotPassword, onLoginSuccess }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});

  // Custom hook para operações de autenticação
  const {
    login,
    loading: isSubmitting,
    error: authError,
    clearError: clearAuthError,
  } = useAuth();

  // Custom hook para carregar os destaques da seção lateral (Hero)
  const {
    data: highlights,
    loading: isLoadingHighlights,
    error: highlightsError,
    refetch: refetchHighlights,
  } = useHeroHighlights();

  const handleEmailChange = (e) => {
    setEmail(e.target.value);
    if (fieldErrors.email) {
      setFieldErrors((prev) => ({ ...prev, email: undefined }));
    }
    if (authError) clearAuthError();
  };

  const handlePasswordChange = (e) => {
    setPassword(e.target.value);
    if (fieldErrors.password) {
      setFieldErrors((prev) => ({ ...prev, password: undefined }));
    }
    if (authError) clearAuthError();
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Validação local dos campos antes de submeter
    const validation = validateLoginForm({ email, password });
    if (!validation.isValid) {
      setFieldErrors(validation.errors);
      return;
    }

    try {
      await login({ email, password });

      // Redirecionar para o dashboard imediatamente após login bem-sucedido
      if (onLoginSuccess) {
        onLoginSuccess();
      } else {
        // TODO: Integrar com react-router-dom para navegação definitiva
        window.location.href = '/dashboard';
      }
    } catch (err) {
      // O erro já é tratado e armazenado no estado do hook useAuth
      console.error('Falha ao autenticar usuário:', err);
    }
  };

  const handleForgotPassword = (e) => {
    e.preventDefault();
    if (onNavigateToForgotPassword) {
      onNavigateToForgotPassword();
    } else {
      // TODO: Integrar com a rota de recuperação de senha (/recuperar-senha) via react-router-dom:
      // navigate('/recuperar-senha');
      window.location.href = '/recuperar-senha';
    }
  };

  return (
    <main className="bg-surface font-body-md text-on-surface antialiased min-h-screen flex flex-col md:flex-row w-full">
      {/* Coluna Esquerda: Formulário de Autenticação */}
      <section
        aria-label="Formulário de acesso"
        className="w-full md:w-5/12 lg:w-4/12 flex flex-col items-center justify-center p-gutter min-h-screen relative z-10 bg-surface shadow-[4px_0_24px_rgba(27,67,50,0.02)]"
      >
        <div className="w-full max-w-sm flex flex-col">
          {/* Logotipo e Nome da Plataforma */}
          <BrandHeader name={BRAND_NAME} iconName={BRAND_DEFAULT_ICON} />

          {/* Boas-vindas e Orientações */}
          <div className="mb-lg">
            <h2 className="font-headline-sm text-headline-sm text-primary mb-2">
              {UI_STRINGS.LOGIN_TITLE}
            </h2>
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              {UI_STRINGS.LOGIN_SUBTITLE}
            </p>
          </div>

          {/* Alertas de Feedback */}
          {authError && (
            <Alert
              type="error"
              message={authError}
              onClose={clearAuthError}
            />
          )}

          {/* Formulário desacoplado */}
          <LoginForm
            email={email}
            password={password}
            fieldErrors={fieldErrors}
            isLoading={isSubmitting}
            onSubmit={handleSubmit}
            onEmailChange={handleEmailChange}
            onPasswordChange={handlePasswordChange}
            onForgotPasswordClick={handleForgotPassword}
          />
        </div>
      </section>

      {/* Coluna Direita: Painel Visual Hero com destaques e 3 estados (loading, erro, vazio) */}
      <HeroSection
        highlights={highlights}
        isLoading={isLoadingHighlights}
        error={highlightsError}
        onRetry={refetchHighlights}
      />
    </main>
  );
}
