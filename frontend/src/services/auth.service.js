import { ENDPOINTS } from '../config/api.config';
import { MOCK_AUTH_USER, MOCK_PASSWORD_RESET_RESPONSE } from '../data/auth.mock';

/**
 * Camada de serviço responsável por operações de autenticação e recuperação de credenciais.
 * As assinaturas já são definitivas para integração direta com a API REST .NET.
 */

/**
 * Autentica o usuário com e-mail corporativo e senha.
 * @param {Object} credentials
 * @param {string} credentials.email
 * @param {string} credentials.password
 * @returns {Promise<{ user: typeof MOCK_AUTH_USER, token: string }>}
 */
export async function login({ email, password }) {
  // Simulação de latência de rede (600ms)
  await new Promise((resolve) => setTimeout(resolve, 600));

  // TODO: Substituir mock por chamada HTTP real com fetch ou axios ao backend .NET:
  // const response = await fetch(ENDPOINTS.AUTH.LOGIN, {
  //   method: 'POST',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify({ email, password }),
  // });
  // if (!response.ok) {
  //   const errorData = await response.json().catch(() => ({}));
  //   throw new Error(errorData.detail || 'Falha na autenticação. Verifique suas credenciais.');
  // }
  // return await response.json();

  if (!email || !password) {
    throw new Error('E-mail e senha são obrigatórios.');
  }

  // Simulação de validação básica: se a senha for "erro", simula falha
  if (password === 'erro') {
    throw new Error('Credenciais inválidas. Verifique seu e-mail e senha.');
  }

  return {
    user: {
      ...MOCK_AUTH_USER,
      email,
    },
    token: MOCK_AUTH_USER.token,
  };
}

/**
 * Solicita o envio de link de recuperação de senha para o e-mail corporativo.
 * @param {Object} payload
 * @param {string} payload.email
 * @returns {Promise<typeof MOCK_PASSWORD_RESET_RESPONSE>}
 */
export async function requestPasswordReset({ email }) {
  // Simulação de latência de rede (500ms)
  await new Promise((resolve) => setTimeout(resolve, 500));

  // TODO: Substituir mock por chamada HTTP real POST ao backend .NET:
  // const response = await fetch(ENDPOINTS.AUTH.FORGOT_PASSWORD, {
  //   method: 'POST',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify({ email }),
  // });
  // if (!response.ok) {
  //   const errorData = await response.json().catch(() => ({}));
  //   throw new Error(errorData.detail || 'Não foi possível solicitar a recuperação de senha.');
  // }
  // return await response.json();

  if (!email) {
    throw new Error('O e-mail corporativo é obrigatório.');
  }

  // Simula erro caso o e-mail seja especificamente 'inexistente@empresa.com'
  if (email.toLowerCase().includes('inexistente')) {
    throw new Error('E-mail não encontrado em nossa base corporativa.');
  }

  return {
    ...MOCK_PASSWORD_RESET_RESPONSE,
  };
}

/**
 * Reenvia o link de recuperação de senha para o mesmo e-mail.
 * @param {Object} payload
 * @param {string} payload.email
 * @returns {Promise<typeof MOCK_PASSWORD_RESET_RESPONSE>}
 */
export async function resendPasswordReset({ email }) {
  // Simulação de latência de rede (500ms)
  await new Promise((resolve) => setTimeout(resolve, 500));

  // TODO: Substituir mock por chamada HTTP real POST ao backend .NET:
  // const response = await fetch(ENDPOINTS.AUTH.RESEND_FORGOT_PASSWORD, {
  //   method: 'POST',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify({ email }),
  // });
  // if (!response.ok) {
  //   const errorData = await response.json().catch(() => ({}));
  //   throw new Error(errorData.detail || 'Não foi possível reenviar o link de recuperação.');
  // }
  // return await response.json();

  if (!email) {
    throw new Error('O e-mail corporativo é obrigatório para o reenvio.');
  }

  return {
    ...MOCK_PASSWORD_RESET_RESPONSE,
  };
}

/**
 * Encerra a sessão do usuário.
 * @returns {Promise<void>}
 */
export async function logout() {
  await new Promise((resolve) => setTimeout(resolve, 200));

  // TODO: Notificar o backend sobre o encerramento da sessão ou revogação do refresh token:
  // await fetch(ENDPOINTS.AUTH.LOGOUT, { method: 'POST' });
}

/**
 * Obtém os dados do usuário atualmente autenticado.
 * @returns {Promise<typeof MOCK_AUTH_USER | null>}
 */
export async function getCurrentUser() {
  await new Promise((resolve) => setTimeout(resolve, 300));

  // TODO: Validar token com o endpoint GET /auth/me do backend:
  // const response = await fetch(ENDPOINTS.AUTH.ME, {
  //   headers: { Authorization: `Bearer ${localStorage.getItem('token')}` },
  // });
  // if (!response.ok) return null;
  // return await response.json();

  return MOCK_AUTH_USER;
}
