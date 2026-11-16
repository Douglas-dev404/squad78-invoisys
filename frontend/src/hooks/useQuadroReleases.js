import { useCallback, useEffect, useState } from 'react';
import { listarQuadroReleases } from '../services/releases.service';
import { UI_STRINGS } from '../utils/constants';

/**
 * Carrega o quadro de Releases (Jira + status no InvoiSys).
 * @returns {{ cartoes: Array, avisoJira: string|null, carregando: boolean, erro: string|null, recarregar: Function }}
 */
export function useQuadroReleases() {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  useEffect(() => {
    let cancelado = false;

    listarQuadroReleases()
      .then((dados) => {
        if (!cancelado) {
          setResultado({
            tentativa,
            cartoes: Array.isArray(dados?.cartoes) ? dados.cartoes : [],
            avisoJira: dados?.avisoJira ?? null,
            erro: null,
          });
        }
      })
      .catch((err) => {
        if (!cancelado) {
          setResultado({
            tentativa,
            cartoes: [],
            avisoJira: null,
            erro: err?.detail || err?.message || UI_STRINGS.RELEASES_LOAD_ERROR,
          });
        }
      });

    return () => {
      cancelado = true;
    };
  }, [tentativa]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);
  const carregando = resultado === null || resultado.tentativa !== tentativa;

  return {
    cartoes: carregando ? [] : resultado.cartoes,
    avisoJira: carregando ? null : resultado.avisoJira,
    carregando,
    erro: carregando ? null : resultado.erro,
    recarregar,
  };
}
