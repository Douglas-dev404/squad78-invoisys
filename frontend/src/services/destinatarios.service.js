import { api } from './http.js';

/**
 * Destinatários de e-mail (#144), por nome.
 * @returns {Promise<Array<{ id, nome, email, publico, ativo, criadoEm }>>}
 */
export function listarDestinatarios({ publico, incluirInativos = false } = {}, options) {
  const query = new URLSearchParams();
  if (publico) query.set('publico', publico);
  if (incluirInativos) query.set('incluirInativos', 'true');
  const sufixo = query.size > 0 ? `?${query}` : '';
  return api.get(`/destinatarios${sufixo}`, options);
}

const corpo = ({ nome, email, publico }) => ({ nome: nome.trim(), email: email.trim(), publico });

/** 201 · 409 se o e-mail já está no público · 422 com `errors` por campo. */
export const cadastrarDestinatario = (dados) => api.post('/destinatarios', corpo(dados));

/** 200 · 404 · 409 · 422. */
export const alterarDestinatario = (id, dados) =>
  api.put(`/destinatarios/${encodeURIComponent(id)}`, corpo(dados));

export const desativarDestinatario = (id) => api.post(`/destinatarios/${encodeURIComponent(id)}/desativar`);

export const reativarDestinatario = (id) => api.post(`/destinatarios/${encodeURIComponent(id)}/reativar`);
