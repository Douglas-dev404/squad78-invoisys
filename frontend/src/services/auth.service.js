import { api } from './http.js';
import { clearSession } from '../utils/session.js';

export function mapUsuario(usuario) {
  return {
    id: usuario.id, name: usuario.nome, email: usuario.email,
    role: usuario.papel, avatarUrl: usuario.avatarUrl, createdAt: usuario.criadoEm,
  };
}

export async function login({ email, password, senha = password }) {
  const response = await api.post('/auth/login', { email, senha }, { auth: false });
  return { ...response, usuario: mapUsuario(response.usuario) };
}

export async function logout() {
  try {
    await api.post('/auth/logout');
  } finally {
    clearSession();
  }
}

export async function getCurrentUser(options) {
  return mapUsuario(await api.get('/auth/me', options));
}

export const requestPasswordReset = ({ email }) => api.post('/auth/forgot-password', { email }, { auth: false });
export const resendPasswordReset = ({ email }) => api.post('/auth/resend-forgot-password', { email }, { auth: false });
