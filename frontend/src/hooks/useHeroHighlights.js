import { getHeroHighlights } from '../services/branding.service';
import { useAsyncResource } from './useAsyncResource';

export function useHeroHighlights() {
  return useAsyncResource(getHeroHighlights);
}
