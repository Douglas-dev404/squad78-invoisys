/**
 * Mock de autenticação representando o formato retornado pelo endpoint de login
 * e de recuperação de senha.
 * Inclui campos id, dados de perfil, token JWT simulado e data de criação.
 */

export const MOCK_AUTH_USER = {
  id: 'usr_884f2910a',
  name: 'Analista de Release',
  email: 'analista@invoisys.com.br',
  role: 'release_manager',
  avatarUrl: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=128&q=80',
  createdAt: '2026-01-15T10:30:00.000Z',
};
