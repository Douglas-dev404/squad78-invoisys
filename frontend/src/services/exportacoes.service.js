import { api } from './http.js';

export const TAMANHO_PAGINA = 20;

/**
 * Histórico de exportações, mais recente primeiro.
 * @returns {Promise<{ itens: Array<{ id, chaveRelease, tituloExecutivo, publico, formato, geradoPor, geradoEm }>,
 *   total: number, pagina: number, tamanho: number }>}
 */
export function listarExportacoes({ pagina = 1, tamanho = TAMANHO_PAGINA, publico, busca } = {}, options) {
  const query = new URLSearchParams({ pagina: String(pagina), tamanho: String(tamanho) });
  if (publico) query.set('publico', publico);
  if (busca?.trim()) query.set('busca', busca.trim());
  return api.get(`/exportacoes?${query}`, options);
}

/** Uma exportação com o `conteudo` gerado. 404 se não existe. */
export const buscarExportacao = (id, options) => api.get(`/exportacoes/${encodeURIComponent(id)}`, options);
