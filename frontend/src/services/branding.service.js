import { MOCK_HERO_HIGHLIGHTS } from '../data/branding.mock';

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
