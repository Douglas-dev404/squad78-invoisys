import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { getSession } from '../../utils/session';
import { Layout } from '../Layout/Layout';

export function RotaProtegida() {
  const location = useLocation();
  return getSession() ? <Outlet /> : <Navigate to="/login" replace state={{ from: location }} />;
}

export function RotaAdmin() {
  return getSession()?.usuario?.papel === 'admin' ? <Outlet /> : <Layout><h1 className="text-headline-lg text-primary">Sem permissão</h1><p>Esta página é exclusiva de administradores.</p></Layout>;
}
