import { useCallback, useRef, useState } from 'react';
import { exportarComunicado } from '../services/releases.service';
import { mensagemDeErro } from '../utils/revisao';

/**
 * Exportar o comunicado aprovado em Markdown (#72). Cada chamada cria um registro novo no histórico.
 * @param {Object} params
 * @param {string} params.chave
 * @param {string} params.publico
 * @returns {{ exportar: Function, exportando: boolean, erro: string|null,
 *   resultado: { id, publico, formato, conteudo, geradoPor, geradoEm }|null, limpar: Function }}
 */
export function useExportacao({ chave, publico }) {
  const [exportando, setExportando] = useState(false);
  const [erro, setErro] = useState(null);
  const [resultado, setResultado] = useState(null);
  const ocupado = useRef(false);

  const exportar = useCallback(async () => {
    if (ocupado.current) return null;
    ocupado.current = true;
    setExportando(true);
    setErro(null);
    try {
      const exportado = await exportarComunicado(chave, publico, 'markdown');
      setResultado(exportado);
      return exportado;
    } catch (err) {
      setErro(mensagemDeErro(err));
      return null;
    } finally {
      ocupado.current = false;
      setExportando(false);
    }
  }, [chave, publico]);

  const limpar = useCallback(() => {
    setErro(null);
    setResultado(null);
  }, []);

  return { exportar, exportando, erro, resultado, limpar };
}
