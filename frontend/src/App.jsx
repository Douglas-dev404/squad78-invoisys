import { useState } from 'react';
import { LoginPage } from './pages/LoginPage/LoginPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage/ForgotPasswordPage';
import { EmailConfirmationPage } from './pages/EmailConfirmationPage/EmailConfirmationPage';
import { DashboardPage } from './pages/DashboardPage/DashboardPage';
import { ProfilePage } from './pages/ProfilePage/ProfilePage';
import { UnderConstructionPage } from './pages/UnderConstructionPage/UnderConstructionPage';
import { UserManagementPage } from './pages/UserManagementPage/UserManagementPage';
import { CreateUserPage } from './pages/CreateUserPage/CreateUserPage';

const UNDER_CONSTRUCTION_SCREENS = {
  'review-queue': 'Review Queue',
  'sent-history': 'Sent History',
  'system-logs': 'System Logs',
  'technical-settings': 'Technical Settings',
};

/**
 * Componente raiz da aplicação.
 * Gerencia a navegação entre as telas do fluxo de autenticação:
 * Login → Recuperação de Senha → Confirmação de Envio → Login
 */
function App() {
  const [currentScreen, setCurrentScreen] = useState('login');
  const [previousScreen, setPreviousScreen] = useState('dashboard');
  const [recoveryEmail, setRecoveryEmail] = useState('');

  const navigateToProfile = () => {
    setPreviousScreen(currentScreen);
    setCurrentScreen('profile');
  };

  const handleSidebarNavigate = (screenKey) => {
    setCurrentScreen(screenKey);
  };

  // TODO: Substituir controle de estado local por roteamento definitivo via react-router-dom:
  // <Routes>
  //   <Route path="/login" element={<LoginPage />} />
  //   <Route path="/dashboard" element={<DashboardPage />} />
  //   <Route path="/perfil" element={<ProfilePage />} />
  //   <Route path="/recuperar-senha" element={<ForgotPasswordPage />} />
  //   <Route path="/email-confirmacao" element={<EmailConfirmationPage />} />
  // </Routes>

  if (currentScreen === 'profile') {
    return (
      <ProfilePage
        onLogout={() => setCurrentScreen('login')}
        onProfileClick={navigateToProfile}
        onNavigate={handleSidebarNavigate}
        onBack={() => setCurrentScreen(previousScreen)}
      />
    );
  }

  if (currentScreen === 'dashboard') {
    return (
      <DashboardPage
        onLogout={() => setCurrentScreen('login')}
        onProfileClick={navigateToProfile}
        onNavigate={handleSidebarNavigate}
      />
    );
  }

  if (currentScreen === 'create-user') {
    return (
      <CreateUserPage
        onLogout={() => setCurrentScreen('login')}
        onProfileClick={navigateToProfile}
        onNavigate={handleSidebarNavigate}
        onCancel={() => setCurrentScreen('user-management')}
        onCreated={() => setCurrentScreen('user-management')}
      />
    );
  }

  if (currentScreen === 'user-management') {
    return (
      <UserManagementPage
        onLogout={() => setCurrentScreen('login')}
        onProfileClick={navigateToProfile}
        onNavigate={handleSidebarNavigate}
        onCreateUser={() => setCurrentScreen('create-user')}
      />
    );
  }

  if (UNDER_CONSTRUCTION_SCREENS[currentScreen]) {
    return (
      <UnderConstructionPage
        title={UNDER_CONSTRUCTION_SCREENS[currentScreen]}
        activeScreen={currentScreen}
        onNavigate={handleSidebarNavigate}
        onLogout={() => setCurrentScreen('login')}
        onProfileClick={navigateToProfile}
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
