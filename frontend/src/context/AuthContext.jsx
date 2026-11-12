import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import * as authService from '../services/auth.service';
import { clearSession, getSession, saveSession } from '../utils/session';

import { AuthContext } from './auth-context';

export function AuthProvider({ children }) {
  const navigate = useNavigate();
  const [usuario, setUsuario] = useState(null);
  const [carregando, setCarregando] = useState(() => Boolean(getSession()));
  const [expiraEm, setExpiraEm] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [restoreError, setRestoreError] = useState(null);

  useEffect(() => {
    const session = getSession();
    if (!session) return;
    const controller = new AbortController();
    authService.getCurrentUser({ signal: controller.signal }).then(user => {
      if (controller.signal.aborted) return;
      saveSession({ ...session, usuario: user });
      setUsuario(user);
      setExpiraEm(session.expiraEm);
    }).catch(err => {
      if (controller.signal.aborted) return;
      if (err.status === 401) clearSession();
      else setRestoreError(err.detail || 'Não foi possível restaurar a sessão.');
    }).finally(() => {
      if (!controller.signal.aborted) setCarregando(false);
    });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const endSession = () => {
      clearSession();
      setUsuario(null);
      setExpiraEm(null);
      setRestoreError(null);
    };
    const storageChanged = event => {
      if ((event.key === 'invoisys.session' || event.key === null) && !getSession()) endSession();
    };
    window.addEventListener('invoisys:unauthorized', endSession);
    window.addEventListener('storage', storageChanged);
    const timer = expiraEm ? setTimeout(() => {
      endSession();
      navigate('/login', { replace: true });
    }, Math.max(0, Date.parse(expiraEm) - Date.now())) : null;
    return () => {
      window.removeEventListener('invoisys:unauthorized', endSession);
      window.removeEventListener('storage', storageChanged);
      if (timer) clearTimeout(timer);
    };
  }, [expiraEm, navigate]);

  const login = useCallback(async credentials => {
    setLoading(true);
    setError(null);
    try {
      const response = await authService.login(credentials);
      saveSession(response);
      setUsuario(response.usuario);
      setExpiraEm(response.expiraEm);
      return response;
    } catch (err) {
      setError(err.status === 401 ? 'E-mail ou senha inválidos.' : err.status === 429
        ? 'Muitas tentativas. Aguarde um minuto e tente de novo.' : err.detail || err.message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
    const pending = authService.logout();
    clearSession();
    setUsuario(null);
    setExpiraEm(null);
    setRestoreError(null);
    navigate('/login', { replace: true });
    try { await pending; } catch {}
  }, [navigate]);
  const clearError = useCallback(() => setError(null), []);

  if (carregando) return <main role="status" className="p-lg">Restaurando sessão…</main>;
  if (restoreError) return <main className="p-lg"><p role="alert">{restoreError}</p><button onClick={() => window.location.reload()}>Tentar novamente</button><button onClick={logout}>Ir para o login</button></main>;
  return <AuthContext.Provider value={{ usuario, user: usuario, data: usuario, carregando, loading, error,
    isAuthenticated: Boolean(usuario), isAdmin: usuario?.role === 'admin', login, logout, clearError }}>
    {children}
  </AuthContext.Provider>;
}
