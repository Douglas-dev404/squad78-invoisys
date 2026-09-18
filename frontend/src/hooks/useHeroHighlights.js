import { useState, useEffect, useCallback } from 'react';
import { getHeroHighlights } from '../services/branding.service';

/**
 * Custom hook para carregar destaques da seção Hero.
 * Expõe { data, loading, error, refetch } permitindo renderizar
 * os estados de carregamento, erro e lista vazia.
 */
export function useHeroHighlights() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchHighlights = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await getHeroHighlights();
      setData(Array.isArray(items) ? items : []);
    } catch (err) {
      const message =
        err instanceof Error
          ? err.message
          : 'Não foi possível carregar as informações do painel.';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchHighlights();
  }, [fetchHighlights]);

  return {
    data,
    loading,
    error,
    refetch: fetchHighlights,
  };
}
