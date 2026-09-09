import { ENDPOINTS } from '../config/api.config';
import { MOCK_BRAND_INFO, MOCK_HERO_HIGHLIGHTS } from '../data/branding.mock';

/**
 * Camada de serviço responsável por buscar dados institucionais e destaques visuais.
 */

/**
 * Obtém a lista de destaques informativos para exibição na seção lateral (Hero).
 * @returns {Promise<Array<typeof MOCK_HERO_HIGHLIGHTS[0]>>}
 */
export async function getHeroHighlights() {
  // Simulação de latência de rede (400ms)
  await new Promise((resolve) => setTimeout(resolve, 400));

  // TODO: Substituir mock por chamada HTTP real com fetch ou axios ao backend:
  // const response = await fetch(ENDPOINTS.BRANDING.HIGHLIGHTS);
  // if (!response.ok) {
  //   throw new Error('Não foi possível carregar os destaques institucionais.');
  // }
  // return await response.json();

  return [...MOCK_HERO_HIGHLIGHTS];
}

/**
 * Obtém informações institucionais da marca e textos da interface.
 * @returns {Promise<typeof MOCK_BRAND_INFO>}
 */
export async function getBrandInfo() {
  await new Promise((resolve) => setTimeout(resolve, 200));

  // TODO: Obter configurações customizadas de tenant da empresa via API:
  // const response = await fetch(`${API_BASE_URL}/tenant/settings`);
  // return await response.json();

  return { ...MOCK_BRAND_INFO };
}
