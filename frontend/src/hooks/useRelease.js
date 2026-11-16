import { useCallback, useEffect, useState } from 'react';
import { buscarRelease } from '../services/releases.service';

/**
 * Carrega o detalhe de uma Release. `recarregar` refaz o GET (usado depois de cada ação de revisão).
 * Durante um recarregamento da mesma chave, a Release anterior continua na tela (`atualizando = true`),
 * para a aba e a posição do usuário não se perderem depois de aprovar, reabrir ou reprocessar.
 * @param {string} chave chave da Release (fixVersion do Jira)
 * @returns {{ release: Object|null, carregando: boolean, atualizando: boolean,
 *   erro: import('../services/http').ApiError|Error|null, recarregar: Function }}
 */
export function useRelease(chave) {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  const pedido = `${chave}|${tentativa}`;

  useEffect(() => {
    let cancelado = false;

    buscarRelease(chave)
      .then((dados) => {
        if (!cancelado) setResultado({ chave, pedido, release: dados, erro: null });
      })
      .catch((err) => {
        if (!cancelado) setResultado({ chave, pedido, release: null, erro: err });
      });

    return () => {
      cancelado = true;
    };
  }, [chave, pedido]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);

  const atual = resultado !== null && resultado.pedido === pedido;
  const anteriorDaMesmaChave = resultado !== null && resultado.chave === chave && resultado.release !== null;
  const carregando = !atual && !anteriorDaMesmaChave;

  return {
    release: carregando ? null : resultado.release,
    carregando,
    atualizando: !atual && anteriorDaMesmaChave,
    erro: atual ? resultado.erro : null,
    recarregar,
  };
}
