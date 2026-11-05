import { useState, useEffect, useCallback } from 'react';

export function useAsyncResource(loader, initialData = null) {
  const [state, setState] = useState({ data: initialData, loading: true, error: null });
  const [request, setRequest] = useState(0);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const data = await loader();
        if (active) setState({ data, loading: false, error: null });
      } catch (error) {
        if (active) setState({ data: initialData, loading: false,
          error: error.message || 'Não foi possível carregar os dados. Tente de novo.' });
      }
    }
    load();
    return () => { active = false; };
  }, [loader, initialData, request]);

  const refetch = useCallback(() => {
    setState((current) => ({ ...current, loading: true, error: null }));
    setRequest((current) => current + 1);
  }, []);
  const clearError = useCallback(() => setState((current) => ({ ...current, error: null })), []);
  return { ...state, refetch, clearError };
}
