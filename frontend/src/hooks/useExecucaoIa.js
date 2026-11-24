import { useCallback, useEffect, useRef, useState } from 'react';
import { buscarEventosAoVivo } from '../services/integracoes.service';
import { INTERVALO_POLLING_MS, acrescentarEventos } from '../utils/execucaoIa';

/**
 * Acompanha a execução da IA em tempo quase real: consulta /execucoes/ao-vivo a cada segundo
 * enquanto a tela está aberta e não pausada, sempre a partir do último id recebido.
 */
export function useExecucaoIa() {
  const [eventos, setEventos] = useState([]);
  const [pausado, setPausado] = useState(false);
  const [erro, setErro] = useState(null);
  const ultimoId = useRef(0);

  useEffect(() => {
    if (pausado) return undefined;
    const controller = new AbortController();
    let timer;

    const buscar = async () => {
      try {
        const resposta = await buscarEventosAoVivo({ depois: ultimoId.current, signal: controller.signal });
        ultimoId.current = Math.max(ultimoId.current, resposta.ultimoId);
        setEventos((atuais) => acrescentarEventos(atuais, resposta.eventos));
        setErro(null);
      } catch (error) {
        if (controller.signal.aborted) return;
        setErro(error?.detail || error?.message || 'Não foi possível acompanhar a execução.');
      }
      if (!controller.signal.aborted) timer = setTimeout(buscar, INTERVALO_POLLING_MS);
    };

    buscar();
    return () => {
      controller.abort();
      clearTimeout(timer);
    };
  }, [pausado]);

  const limpar = useCallback(() => setEventos([]), []);
  const alternarPausa = useCallback(() => setPausado((valor) => !valor), []);

  return { eventos, pausado, erro, limpar, alternarPausa };
}
