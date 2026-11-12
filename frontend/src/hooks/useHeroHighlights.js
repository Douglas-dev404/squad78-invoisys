import { useState, useEffect, useCallback } from 'react';
import { getHeroHighlights } from '../services/branding.service';

export function useHeroHighlights() {
  const [state, setState] = useState({ data: [], loading: true, error: null });
  const [request, setRequest] = useState(0);
  const refetch = useCallback(() => {
    setState(previous => ({ ...previous, loading: true, error: null }));
    setRequest(previous => previous + 1);
  }, []);

  useEffect(() => {
    let active = true;
    getHeroHighlights().then(data => {
      if (active) setState({ data, loading: false, error: null });
    });
    return () => { active = false; };
  }, [request]);

  return { ...state, refetch };
}
