import { api } from './http.js';

const TIMEOUT_PROCESSAR_MS = 180000;

/**
 * Lista Releases. Sem status devolve todas.
 * @param {string} [status] pendente | processando | aguardando_revisao | aprovado | falhou
 * @returns {Promise<Array<{ chaveJira: string, status: string, criadoEm: string }>>}
 */
export async function listarReleases(status) {
  const query = status ? `?status=${encodeURIComponent(status)}` : '';
  return api.get(`/releases${query}`);
}

/**
 * Dispara a IA para uma Release (pode levar de 10 s a 2 min).
 * Erros: 409, 501 e 502 chegam com `detail` em português.
 */
export async function processarRelease(chave) {
  return api.post(
    `/releases/${encodeURIComponent(chave)}/processar`,
    undefined,
    { timeoutMs: TIMEOUT_PROCESSAR_MS }
  );
}
const caminhoRelease = (chave) => `/releases/${encodeURIComponent(chave)}`;
const caminhoItem = (chave, itemId) => `${caminhoRelease(chave)}/itens/${encodeURIComponent(itemId)}`;

/** Troca o texto de um item. 200 com o item atualizado · 404 · 409 (versão aprovada) · 422 (texto vazio). */
export const editarItem = (chave, itemId, texto) => api.patch(caminhoItem(chave, itemId), { texto });

/** Tira o item do comunicado (continua na lista, riscado). Motivo é opcional. */
export const excluirItem = (chave, itemId, motivo) =>
  api.post(`${caminhoItem(chave, itemId)}/excluir`, motivo?.trim() ? { motivo: motivo.trim() } : {});

/** Devolve ao comunicado um item excluído. */
export const reincluirItem = (chave, itemId) => api.post(`${caminhoItem(chave, itemId)}/reincluir`);

/** Aprovar, reprovar e reabrir respondem 204. Quem revisou vem do token; não enviar nome no corpo. */
export const aprovarVersao = (chave, publico) => api.post(`${caminhoRelease(chave)}/aprovar`, { publico });

export const reprovarVersao = (chave, publico, motivo) =>
  api.post(`${caminhoRelease(chave)}/reprovar`, { publico, motivo });

export const reabrirVersao = (chave, publico) => api.post(`${caminhoRelease(chave)}/reabrir`, { publico });

/** 201 { id, publico, formato, conteudo, geradoPor, geradoEm } · 409 se a versão não está aprovada. */
export const exportarComunicado = (chave, publico, formato = 'markdown') =>
  api.post(`${caminhoRelease(chave)}/exportar`, { publico, formato });
