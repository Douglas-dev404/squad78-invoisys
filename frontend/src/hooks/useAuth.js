import { useState, useCallback } from 'react';
import { login as loginService, logout as logoutService } from '../services/auth.service';

/**
 * Custom hook para gerenciamento de estado e operações de autenticação.
 * Expõe { data (ou user), loading, error, login, logout, clearError }.
 */
export function useAuth() {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const login = useCallback(async ({ email, password }) => {
    setLoading(true);
    setError(null);

    try {
      const response = await loginService({ email, password });
      setUser(response.user);
      // TODO: Salvar o token no localStorage ou cookie seguro HttpOnly:
      // localStorage.setItem('auth_token', response.token);
      return response;
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Ocorreu um erro ao realizar o login.';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
    setLoading(true);
    try {
      await logoutService();
      setUser(null);
      // TODO: Remover token de autenticação armazenado:
      // localStorage.removeItem('auth_token');
    } finally {
      setLoading(false);
    }
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    data: user,
    user,
    loading,
    error,
    isAuthenticated: Boolean(user),
    login,
    logout,
    clearError,
  };
}
