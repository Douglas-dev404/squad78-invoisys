import { api } from './http.js';

/** 200 [{ nome, descricao, dataLancamento, lancada, statusInvoisys }] · 501/502 com `detail`. */
export const listarVersoesJira = (options) => api.get('/jira/versoes', options);
