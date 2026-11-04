import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { Layout } from '../Layout/Layout';

export function RotaProtegida() {
  const location = useLocation();
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace state={{ from: location }} />;
}

export function RotaAdmin() {
  const { isAdmin } = useAuth();
  return isAdmin ? <Outlet /> : <Layout><h1 className="text-headline-lg text-primary">Sem permissão</h1><p>Esta página é exclusiva de administradores.</p></Layout>;
}
