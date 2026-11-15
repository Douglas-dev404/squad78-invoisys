import { useCallback, useEffect, useState } from 'react';
import { buscarRelease } from '../services/releases.service';

/**
 * Carrega o detalhe de uma Release. `recarregar` refaz o GET (usado depois de cada ação de revisão).
 * @param {string} chave chave da Release (fixVersion do Jira)
 * @returns {{ release: Object|null, carregando: boolean, erro: import('../services/http').ApiError|Error|null, recarregar: Function }}
 */
export function useRelease(chave) {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  const pedido = `${chave}|${tentativa}`;

  useEffect(() => {
    let cancelado = false;

    buscarRelease(chave)
      .then((dados) => {
        if (!cancelado) setResultado({ pedido, release: dados, erro: null });
      })
      .catch((err) => {
        if (!cancelado) setResultado({ pedido, release: null, erro: err });
      });

    return () => {
      cancelado = true;
    };
  }, [chave, pedido]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);

  const carregando = resultado === null || resultado.pedido !== pedido;

  return {
    release: carregando ? null : resultado.release,
    carregando,
    erro: carregando ? null : resultado.erro,
    recarregar,
  };
}
