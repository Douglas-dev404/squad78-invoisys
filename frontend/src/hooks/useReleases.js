import { useCallback, useEffect, useState } from 'react';
import { listarReleases } from '../services/releases.service';
import { UI_STRINGS } from '../utils/constants';

/**
 * Carrega as Releases e recarrega quando o filtro muda.
 * @param {string} [status] filtro da API (undefined = todas)
 * @returns {{ releases: Array, carregando: boolean, erro: string|null, recarregar: Function }}
 */
export function useReleases(status) {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  const pedido = `${status ?? ''}|${tentativa}`;

  useEffect(() => {
    let cancelado = false;

    listarReleases(status)
      .then((dados) => {
        if (!cancelado) {
          setResultado({ pedido, releases: Array.isArray(dados) ? dados : [], erro: null });
        }
      })
      .catch((err) => {
        if (!cancelado) {
          const mensagem = err?.detail || err?.message || UI_STRINGS.RELEASES_LOAD_ERROR;
          setResultado({ pedido, releases: [], erro: mensagem });
        }
      });

    return () => {
      cancelado = true;
    };
  }, [status, pedido]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);

  const carregando = resultado === null || resultado.pedido !== pedido;

  return {
    releases: carregando ? [] : resultado.releases,
    carregando,
    erro: carregando ? null : resultado.erro,
    recarregar,
  };
}