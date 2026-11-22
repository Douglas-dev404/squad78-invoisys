import { useCallback, useEffect, useState } from 'react';
import { listarVersoesJira } from '../services/jira.service.js';
import { UI_STRINGS } from '../utils/constants.js';
import { mensagemDeErro } from '../utils/revisao.js';

const SEM_VERSOES = [];

/**
 * Versões do Jira para o "Processar Release" (GET /jira/versoes).
 * @returns {{ versoes: Array<{ nome, descricao, dataLancamento, lancada, statusInvoisys }>,
 *   carregando: boolean, erro: string|null, vazio: boolean, recarregar: Function }}
 */
export function useVersoesJira() {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);
  const pedido = tentativa;

  useEffect(() => {
    const controller = new AbortController();

    listarVersoesJira({ signal: controller.signal })
      .then((versoes) => {
        if (!controller.signal.aborted) setResultado({ pedido, versoes, erro: null });
      })
      .catch((err) => {
        if (!controller.signal.aborted) {
          setResultado({ pedido, versoes: SEM_VERSOES, erro: mensagemDeErro(err, UI_STRINGS.JIRA_VERSOES_ERRO) });
        }
      });

    return () => controller.abort();
  }, [pedido]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);
  const atual = resultado !== null && resultado.pedido === pedido;
  const versoes = atual ? resultado.versoes : SEM_VERSOES;
  const erro = atual ? resultado.erro : null;

  return {
    versoes,
    carregando: !atual,
    erro,
    vazio: atual && !erro && versoes.length === 0,
    recarregar,
  };
}
