import { useEffect } from 'react';
import { Link, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuth';
import { RotaProtegida, RotaAdmin } from './components/RotaProtegida/RotaProtegida';
import { Layout } from './components/Layout/Layout';
import { LoginPage } from './pages/LoginPage/LoginPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage/ForgotPasswordPage';
import { EmailConfirmationPage } from './pages/EmailConfirmationPage/EmailConfirmationPage';
import { DashboardPage } from './pages/DashboardPage/DashboardPage';
import { ProfilePage } from './pages/ProfilePage/ProfilePage';
import { UserManagementPage } from './pages/UserManagementPage/UserManagementPage';
import { CreateUserPage } from './pages/CreateUserPage/CreateUserPage';
import { SentHistoryPage } from './pages/SentHistoryPage/SentHistoryPage';
import { ReleasesPage } from './pages/ReleasesPage/ReleasesPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage/ResetPasswordPage';

function LoginRoute() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated } = useAuth();
  if (isAuthenticated) return <Navigate to={location.state?.from || '/dashboard'} replace />;
  return <LoginPage
    onNavigateToForgotPassword={() => navigate('/esqueci-senha')}
    onLoginSuccess={() => navigate(location.state?.from || '/dashboard', { replace: true })}
  />;
}

function RecoveryRoute() {
  const navigate = useNavigate();
  return <ForgotPasswordPage
    onNavigateToLogin={() => navigate('/login')}
    onNavigateToConfirmation={(email, response) => navigate('/email-enviado', { state: { email, ...response } })}
  />;
}

function ConfirmationRoute() {
  const navigate = useNavigate();
  const location = useLocation();
  return <EmailConfirmationPage email={location.state?.email || ''} mensagem={location.state?.mensagem} expiraEmMinutos={location.state?.expiraEmMinutos} onNavigateToLogin={() => navigate('/login')} />;
}

function ReleaseRoute() {
  return <Layout><h1 className="text-headline-lg text-primary">Detalhe da Release</h1><p>A revisão do comunicado chega na próxima entrega.</p><Link to="/releases" className="text-primary hover:underline">Voltar para Releases</Link></Layout>;
}

function App() {
  const navigate = useNavigate();
  useEffect(() => {
    const unauthorized = () => navigate('/login', { replace: true });
    window.addEventListener('invoisys:unauthorized', unauthorized);
    return () => window.removeEventListener('invoisys:unauthorized', unauthorized);
  }, [navigate]);
  return <Routes>
    <Route path="/login" element={<LoginRoute />} />
    <Route path="/esqueci-senha" element={<RecoveryRoute />} />
    <Route path="/email-enviado" element={<ConfirmationRoute />} />
    <Route path="/redefinir-senha" element={<ResetPasswordPage />} />
    <Route element={<RotaProtegida />}>
      <Route path="/dashboard" element={<DashboardPage />} />
      <Route path="/releases" element={<ReleasesPage />} />
      <Route path="/releases/:chave" element={<ReleaseRoute />} />
      <Route path="/historico" element={<SentHistoryPage />} />
      <Route path="/perfil" element={<ProfilePage />} />
      <Route element={<RotaAdmin />}>
        <Route path="/usuarios" element={<UserManagementPage />} />
        <Route path="/usuarios/novo" element={<CreateUserPage />} />
      </Route>
    </Route>
    <Route path="/" element={<Navigate to="/dashboard" replace />} />
    <Route path="*" element={<main className="p-lg"><h1>Página não encontrada</h1><Link to="/dashboard">Ir para o Dashboard</Link></main>} />
  </Routes>;
}

export default App;
