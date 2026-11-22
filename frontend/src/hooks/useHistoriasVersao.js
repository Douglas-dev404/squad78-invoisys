import { useCallback, useEffect, useMemo, useState } from 'react';
import { listarHistorias } from '../services/releases.service.js';
import { listaDeHistorias, resumirHistorias } from '../utils/versoesJira.js';
import { UI_STRINGS } from '../utils/constants.js';
import { mensagemDeErro } from '../utils/revisao.js';

/**
 * Prévia das histórias de uma versão. Sem `versao`, não busca nada.
 * @param {string|null} versao
 */
export function useHistoriasVersao(versao) {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);
  const pedido = `${versao ?? ''}|${tentativa}`;

  useEffect(() => {
    if (!versao) return undefined;
    const controller = new AbortController();

    listarHistorias(versao, { signal: controller.signal })
      .then((dados) => {
        if (!controller.signal.aborted) setResultado({ pedido, dados, erro: null });
      })
      .catch((err) => {
        if (!controller.signal.aborted) {
          setResultado({ pedido, dados: null, erro: mensagemDeErro(err, UI_STRINGS.PROCESS_MODAL_PREVIEW_ERROR) });
        }
      });

    return () => controller.abort();
  }, [versao, pedido]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);
  const atual = Boolean(versao) && resultado !== null && resultado.pedido === pedido;
  const dados = atual ? resultado.dados : null;
  const historias = useMemo(() => listaDeHistorias(dados), [dados]);
  const resumo = useMemo(() => resumirHistorias(dados), [dados]);

  return {
    historias,
    resumo,
    carregando: Boolean(versao) && !atual,
    erro: atual ? resultado.erro : null,
    recarregar,
  };
}
