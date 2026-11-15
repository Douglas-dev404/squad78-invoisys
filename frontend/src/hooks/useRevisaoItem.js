import { useCallback, useRef, useState } from 'react';
import { editarItem, excluirItem, reincluirItem } from '../services/releases.service';
import { mensagemDeErro, erroDoCampo } from '../utils/revisao';

/**
 * Editar, excluir e reincluir itens (#70). Cada item tem seu próprio `salvando`/`erro`:
 * a falha de um não afeta os outros.
 * @param {Object} params
 * @param {string} params.chave chave da Release
 * @param {(item: Object) => void} params.onItemAtualizado recebe o item devolvido pela API
 *   (use `substituirItem` de utils/revisao para trocá-lo na lista, sem refazer o GET)
 */
export function useRevisaoItem({ chave, onItemAtualizado }) {
  const [estados, setEstados] = useState({});
  const emAndamento = useRef(new Set());

  const atualizarEstado = (itemId, estado) =>
    setEstados((atual) => ({ ...atual, [itemId]: { ...atual[itemId], ...estado } }));

  const executar = useCallback(
    async (itemId, chamada) => {
      if (emAndamento.current.has(itemId)) return false;
      emAndamento.current.add(itemId);
      atualizarEstado(itemId, { salvando: true, erro: null, erroTexto: null });
      try {
        const atualizado = await chamada();
        onItemAtualizado?.(atualizado);
        atualizarEstado(itemId, { salvando: false });
        return true;
      } catch (err) {
        atualizarEstado(itemId, {
          salvando: false,
          erro: mensagemDeErro(err),
          erroTexto: erroDoCampo(err, 'texto'),
        });
        return false;
      } finally {
        emAndamento.current.delete(itemId);
      }
    },
    [onItemAtualizado],
  );

  const editar = useCallback(
    (itemId, texto) => executar(itemId, () => editarItem(chave, itemId, texto.trim())),
    [chave, executar],
  );
  const excluir = useCallback(
    (itemId, motivo) => executar(itemId, () => excluirItem(chave, itemId, motivo)),
    [chave, executar],
  );
  const reincluir = useCallback((itemId) => executar(itemId, () => reincluirItem(chave, itemId)), [chave, executar]);

  const estadoDo = useCallback(
    (itemId) => ({ salvando: false, erro: null, erroTexto: null, ...estados[itemId] }),
    [estados],
  );
  const limparErro = useCallback((itemId) => atualizarEstado(itemId, { erro: null, erroTexto: null }), []);

  return { editar, excluir, reincluir, estadoDo, limparErro };
}
