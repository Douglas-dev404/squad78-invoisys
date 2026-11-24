import { api } from './http.js';

// Testar a IA espera a resposta do provedor: mais que os 10 s das demais chamadas.
const TIMEOUT_TESTE_MS = 45000;

/** Integrações em uso (só admin). Os segredos nunca voltam: só `tokenConfigurado`/`chaveConfigurada`. */
export const obterIntegracoes = (opts) => api.get('/integracoes', opts);

/** Salva o Jira. Campo em branco mantém o que já existe. 422 com `errors` por campo. */
export const salvarJira = (dados) => api.put('/integracoes/jira', dados);

/** Salva a IA (provedor, modelo e chave do provedor escolhido). */
export const salvarIa = (dados) => api.put('/integracoes/ia', dados);

/** Chama o Jira de verdade com o que está salvo. Sempre 200: `ok` diz se conectou. */
export const testarJira = () => api.post('/integracoes/jira/testar', undefined, { timeoutMs: TIMEOUT_TESTE_MS });

/** Chama a IA de verdade com o que está salvo. */
export const testarIa = () => api.post('/integracoes/ia/testar', undefined, { timeoutMs: TIMEOUT_TESTE_MS });

/** Eventos da execução da IA depois do id `depois` (polling da tela "Execução da IA"). */
export function buscarEventosAoVivo({ depois = 0, release, signal } = {}) {
  const query = new URLSearchParams({ depois: String(depois) });
  if (release) query.set('release', release);
  return api.get(`/execucoes/ao-vivo?${query}`, { signal });
}
