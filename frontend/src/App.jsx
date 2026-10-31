import { useState } from 'react';
import { Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { LoginPage } from './pages/LoginPage/LoginPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage/ForgotPasswordPage';
import { EmailConfirmationPage } from './pages/EmailConfirmationPage/EmailConfirmationPage';
import { DashboardPage } from './pages/DashboardPage/DashboardPage';
import { ProfilePage } from './pages/ProfilePage/ProfilePage';
import { UnderConstructionPage } from './pages/UnderConstructionPage/UnderConstructionPage';
import { UserManagementPage } from './pages/UserManagementPage/UserManagementPage';
import { CreateUserPage } from './pages/CreateUserPage/CreateUserPage';
import { SentHistoryPage } from './pages/SentHistoryPage/SentHistoryPage';
import { ReleasesPage } from './pages/ReleasesPage/ReleasesPage';

const UNDER_CONSTRUCTION_SCREENS = {
  'system-logs': 'System Logs',
  'technical-settings': 'Technical Settings',
};

/**
 * Componente raiz da aplicação.
 * Telas de Releases (/releases) usam react-router; as demais seguem o
 * controle por estado local até migrarem para rotas.
 */
function App() {
  const [currentScreen, setCurrentScreen] = useState('login');
  const [previousScreen, setPreviousScreen] = useState('dashboard');
  const [recoveryEmail, setRecoveryEmail] = useState('');
  const navigate = useNavigate();
  const { pathname } = useLocation();

  const isReleasesRoute = pathname.startsWith('/releases');
  const activeScreen = isReleasesRoute ? 'releases' : currentScreen;

  const goTo = (screenKey) => {
    if (screenKey === 'releases') {
      navigate('/releases');
      return;
    }
    if (isReleasesRoute) navigate('/');
    setCurrentScreen(screenKey);
  };

  const handleLogout = () => goTo('login');

  const navigateToProfile = () => {
    setPreviousScreen(activeScreen);
    goTo('profile');
  };

  if (isReleasesRoute) {
    return (
      <Routes>
        <Route
          path="/releases"
          element={
            <ReleasesPage
              onLogout={handleLogout}
              onProfileClick={navigateToProfile}
              onNavigate={goTo}
            />
          }
        />
        {/* Provisório: a tela de detalhe é a F-06 */}
        <Route
          path="/releases/:chave"
          element={
            <UnderConstructionPage
              title="Detalhe da Release"
              activeScreen="releases"
              onNavigate={goTo}
              onLogout={handleLogout}
              onProfileClick={navigateToProfile}
            />
          }
        />
        <Route path="*" element={<Navigate to="/releases" replace />} />
      </Routes>
    );
  }

  if (currentScreen === 'profile') {
    return (
      <ProfilePage
        onLogout={handleLogout}
        onProfileClick={navigateToProfile}
        onNavigate={goTo}
        onBack={() => goTo(previousScreen)}
      />
    );
  }

  if (currentScreen === 'dashboard') {
    return (
      <DashboardPage
        onLogout={handleLogout}
        onProfileClick={navigateToProfile}
        onNavigate={goTo}
      />
    );
  }

  if (currentScreen === 'create-user') {
    return (
      <CreateUserPage
        onLogout={handleLogout}
        onProfileClick={navigateToProfile}
        onNavigate={goTo}
        onCancel={() => setCurrentScreen('user-management')}
        onCreated={() => setCurrentScreen('user-management')}
      />
    );
  }

  if (currentScreen === 'user-management') {
    return (
      <UserManagementPage
        onLogout={handleLogout}
        onProfileClick={navigateToProfile}
        onNavigate={goTo}
        onCreateUser={() => setCurrentScreen('create-user')}
      />
    );
  }

  if (currentScreen === 'sent-history') {
    return (
      <SentHistoryPage
        onLogout={handleLogout}
        onProfileClick={navigateToProfile}
        onNavigate={goTo}
      />
    );
  }

  if (UNDER_CONSTRUCTION_SCREENS[currentScreen]) {
    return (
      <UnderConstructionPage
        title={UNDER_CONSTRUCTION_SCREENS[currentScreen]}
        activeScreen={currentScreen}
        onNavigate={goTo}
        onLogout={handleLogout}
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