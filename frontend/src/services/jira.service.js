import { api } from './http.js';

/** Versões (fixVersions) do Jira, com data de lançamento e status no InvoiSys quando já processadas. 501/502 trazem `detail`. */
export const listarVersoesJira = (options) => api.get('/jira/versoes', options);