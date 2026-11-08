import { api } from './http.js';
import { MOCK_HERO_HIGHLIGHTS } from '../data/branding.mock.js';

export async function getHeroHighlights() {
  try {
    const items = await api.get('/branding/highlights', { auth: false });
    return items.map(({ id, titulo, descricao, icone }) => ({
      id, title: titulo, description: descricao, icon: icone,
    }));
  } catch (err) {
    console.warn('Não foi possível carregar os destaques:', err.detail || err.message);
    return [...MOCK_HERO_HIGHLIGHTS];
  }
}
