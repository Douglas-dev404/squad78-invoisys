import { PUBLICO_LABEL } from './revisao.js';

export const BUSCA_MAX = 200;

export function lerFiltros(params) {
  const numero = Number.parseInt(params.get('pagina') ?? '', 10);
  const publico = (params.get('publico') ?? '').toLowerCase();
  return {
    pagina: Number.isInteger(numero) && numero >= 1 ? numero : 1,
    publico: Object.hasOwn(PUBLICO_LABEL, publico) ? publico : '',
    busca: (params.get('busca') ?? '').trim().slice(0, BUSCA_MAX),
  };
}
