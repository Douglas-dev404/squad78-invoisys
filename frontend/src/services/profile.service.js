import { getCurrentUser } from './auth.service';

/** Dados frescos do usuário logado (`GET /auth/me`). */
export const fetchProfile = (options) => getCurrentUser(options);
