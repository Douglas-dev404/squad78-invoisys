import { useCallback, useEffect, useState } from 'react';
import { fetchProfile } from '../services/profile.service';

/**
 * Busca o usuário logado em `/auth/me`, para a tela de Perfil mostrar dados frescos.
 * @returns {{ profile: Object|null, carregando: boolean, erro: string|null, recarregar: Function }}
 */
export function useProfile() {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  useEffect(() => {
    const controller = new AbortController();

    fetchProfile({ signal: controller.signal })
      .then((profile) => {
        if (!controller.signal.aborted) setResultado({ tentativa, profile, erro: null });
      })
      .catch((err) => {
        if (!controller.signal.aborted) {
          setResultado({ tentativa, profile: null, erro: err?.detail || err?.message || 'Não foi possível carregar o perfil.' });
        }
      });

    return () => controller.abort();
  }, [tentativa]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);
  const carregando = resultado === null || resultado.tentativa !== tentativa;

  return {
    profile: carregando ? null : resultado.profile,
    carregando,
    erro: carregando ? null : resultado.erro,
    recarregar,
  };
}
