import { api } from './api'; //

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