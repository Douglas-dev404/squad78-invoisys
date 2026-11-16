import { api } from './http.js';

/**
 * Histórico de exportações paginado no servidor (`GET /exportacoes`, B-07).
 * @param {{ pagina?: number, tamanho?: number, publico?: string, busca?: string }} filtros
 * @returns {Promise<{ itens: Array<{ id, chaveRelease, tituloExecutivo, publico, formato, geradoPor, geradoEm }>,
 *   total: number, pagina: number, tamanho: number }>}
 */
export function listarExportacoes({ pagina, tamanho, publico, busca } = {}) {
  const query = new URLSearchParams();
  if (pagina) query.set('pagina', String(pagina));
  if (tamanho) query.set('tamanho', String(tamanho));
  if (publico) query.set('publico', publico);
  if (busca) query.set('busca', busca);
  const sufixo = query.toString();
  return api.get(`/exportacoes${sufixo ? `?${sufixo}` : ''}`);
}

/**
 * Uma exportação com o `conteudo` gerado.
 * @param {string} id
 */
export const buscarExportacao = (id) => api.get(`/exportacoes/${encodeURIComponent(id)}`);
