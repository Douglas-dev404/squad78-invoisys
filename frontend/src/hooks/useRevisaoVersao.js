import { useCallback, useRef, useState } from 'react';
import {
  aprovarVersao,
  processarRelease,
  reabrirVersao,
  reprovarVersao,
} from '../services/releases.service';
import { erroDoCampo, mensagemDeErro } from '../utils/revisao';

/**
 * Aprovar, reprovar, reabrir e reprocessar uma versão (#71). Depois de cada sucesso chama
 * `recarregar()` (do useRelease da #69): o status muda na versão e na Release, então o GET
 * inteiro é a fonte da verdade.
 * @param {Object} params
 * @param {string} params.chave chave da Release
 * @param {string} params.publico público da aba (cliente, comercial, suporte, interno)
 * @param {Function} [params.recarregar]
 * @returns {{ aprovar, reprovar, reabrir, reprocessar, executando: string|null, erro: string|null,
 *   erroMotivo: string|null, limparErro: Function }}
 */
export function useRevisaoVersao({ chave, publico, recarregar }) {
  const [executando, setExecutando] = useState(null);
  const [erro, setErro] = useState(null);
  const [erroMotivo, setErroMotivo] = useState(null);
  const ocupado = useRef(false);

  const executar = useCallback(
    async (acao, chamada) => {
      if (ocupado.current) return false;
      ocupado.current = true;
      setExecutando(acao);
      setErro(null);
      setErroMotivo(null);
      try {
        await chamada();
        recarregar?.();
        return true;
      } catch (err) {
        setErro(mensagemDeErro(err));
        setErroMotivo(erroDoCampo(err, 'motivo'));
        return false;
      } finally {
        ocupado.current = false;
        setExecutando(null);
      }
    },
    [recarregar],
  );

  const aprovar = useCallback(() => executar('aprovar', () => aprovarVersao(chave, publico)), [chave, publico, executar]);
  const reprovar = useCallback(
    (motivo) => executar('reprovar', () => reprovarVersao(chave, publico, motivo.trim())),
    [chave, publico, executar],
  );
  const reabrir = useCallback(() => executar('reabrir', () => reabrirVersao(chave, publico)), [chave, publico, executar]);
  const reprocessar = useCallback(() => executar('reprocessar', () => processarRelease(chave)), [chave, executar]);
  const limparErro = useCallback(() => {
    setErro(null);
    setErroMotivo(null);
  }, []);

  return { aprovar, reprovar, reabrir, reprocessar, executando, erro, erroMotivo, limparErro };
}
