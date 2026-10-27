/**
 * Configuração central de rede e endpoints da API.
 * Lê a variável de ambiente VITE_API_BASE_URL configurada no .env
 * ou utiliza a porta padrão do backend .NET como fallback.
 */

export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080/api/v1';

export const API_TIMEOUT_MS = 10000;

export const ENDPOINTS = {
  AUTH: {
    LOGIN: `${API_BASE_URL}/auth/login`,
    LOGOUT: `${API_BASE_URL}/auth/logout`,
    ME: `${API_BASE_URL}/auth/me`,
    FORGOT_PASSWORD: `${API_BASE_URL}/auth/forgot-password`,
    RESEND_FORGOT_PASSWORD: `${API_BASE_URL}/auth/resend-forgot-password`,
  },
  BRANDING: {
    HIGHLIGHTS: `${API_BASE_URL}/branding/highlights`,
  },
};
