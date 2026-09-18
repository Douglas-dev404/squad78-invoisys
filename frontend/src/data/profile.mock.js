import { MOCK_AUTH_USER } from './auth.mock';

export const ROLE_LABELS = {
  release_manager: 'Analista de Release',
  admin: 'Administrador',
  reviewer: 'Revisor',
};

export const profileMock = {
  id: MOCK_AUTH_USER.id,
  name: MOCK_AUTH_USER.name,
  email: MOCK_AUTH_USER.email,
  role: MOCK_AUTH_USER.role,
  roleLabel: ROLE_LABELS[MOCK_AUTH_USER.role] || MOCK_AUTH_USER.role,
  avatarUrl: MOCK_AUTH_USER.avatarUrl,
  department: 'Engenharia de Software',
  phone: '+55 (11) 98765-4321',
  location: 'São Paulo, Brasil',
  createdAt: MOCK_AUTH_USER.createdAt,
  lastAccessAt: '2026-09-11T08:42:00.000Z',
};
