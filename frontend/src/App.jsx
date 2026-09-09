import { useState } from 'react';
import { LoginPage } from './pages/LoginPage/LoginPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage/ForgotPasswordPage';
import { EmailConfirmationPage } from './pages/EmailConfirmationPage/EmailConfirmationPage';
import { DashboardPage } from './pages/DashboardPage/DashboardPage';

/**
 * Componente raiz da aplicação.
 * Gerencia a navegação entre as telas do fluxo de autenticação:
 * Login → Recuperação de Senha → Confirmação de Envio → Login
 */
function App() {
  const [currentScreen, setCurrentScreen] = useState('login');
  const [recoveryEmail, setRecoveryEmail] = useState('');

  // TODO: Substituir controle de estado local por roteamento definitivo via react-router-dom:
  // <Routes>
  //   <Route path="/login" element={<LoginPage />} />
  //   <Route path="/dashboard" element={<DashboardPage />} />
  //   <Route path="/recuperar-senha" element={<ForgotPasswordPage />} />
  //   <Route path="/email-confirmacao" element={<EmailConfirmationPage />} />
  // </Routes>

  if (currentScreen === 'dashboard') {
    return (
      <DashboardPage
        onLogout={() => setCurrentScreen('login')}
      />
    );
  }

  if (currentScreen === 'email-confirmation') {
    return (
      <EmailConfirmationPage
        email={recoveryEmail}
        onNavigateToLogin={() => {
          setCurrentScreen('login');
          setRecoveryEmail('');
        }}
      />
    );
  }

  if (currentScreen === 'forgot-password') {
    return (
      <ForgotPasswordPage
        onNavigateToLogin={() => setCurrentScreen('login')}
        onNavigateToConfirmation={(email) => {
          setRecoveryEmail(email);
          setCurrentScreen('email-confirmation');
        }}
      />
    );
  }

  return (
    <LoginPage
      onNavigateToForgotPassword={() => setCurrentScreen('forgot-password')}
      onLoginSuccess={() => setCurrentScreen('dashboard')}
    />
  );
}

export default App;
