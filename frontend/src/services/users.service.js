import { api } from './http.js';
import { ROLE_LABELS } from '../utils/constants';

const iniciais = (nome) =>
  nome
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((parte) => parte[0]?.toUpperCase() || '')
    .join('');

/** Converte `UsuarioAdminOut` da API para o formato das telas. A API não devolve avatarUrl nesta rota. */
const paraUsuario = (u) => ({
  id: u.id,
  name: u.nome,
  email: u.email,
  role: u.papel,
  roleLabel: ROLE_LABELS[u.papel] ?? u.papel,
  status: u.ativo ? 'active' : 'inactive',
  createdAt: u.criadoEm,
  initials: iniciais(u.nome),
});

/** Lista todos os usuários (só admin). */
export async function fetchUsers() {
  const usuarios = await api.get('/usuarios');
  return usuarios.map(paraUsuario);
}

/** Cadastra um usuário. 409 se o e-mail já existe · 422 com `errors` por campo. */
export async function createUser({ fullName, email, role, senhaInicial }) {
  const criado = await api.post('/usuarios', {
    nome: fullName.trim(),
    email: email.trim(),
    papel: role,
    senhaInicial,
  });
  return paraUsuario(criado);
}

/** Desativa um usuário (204). Não existe reativar. 422 se for a própria conta. */
export const deactivateUser = (id) => api.post(`/usuarios/${encodeURIComponent(id)}/desativar`);
