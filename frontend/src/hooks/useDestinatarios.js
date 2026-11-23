import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  alterarDestinatario,
  cadastrarDestinatario,
  desativarDestinatario,
  listarDestinatarios,
  reativarDestinatario,
} from '../services/destinatarios.service';
import { DESTINATARIOS_TEXTOS, lerFiltrosDestinatarios } from '../utils/destinatarios';
import { mensagemDeErro } from '../utils/revisao';

/**
 * Lista de destinatários com filtros na URL (`?publico=cliente&inativos=1`) e as ações da tela (#147).
 * `salvar` devolve o erro da API (com `errors` por campo) para o formulário mostrar; as outras ações recarregam a lista.
 */
export function useDestinatarios() {
  const [params, setParams] = useSearchParams();
  const { publico, inativos } = lerFiltrosDestinatarios(params);
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  const pedido = `${publico}|${inativos}|${tentativa}`;

  useEffect(() => {
    const controller = new AbortController();
    const chave = `${publico}|${inativos}|${tentativa}`;
    listarDestinatarios({ publico, incluirInativos: inativos }, { signal: controller.signal })
      .then((itens) => {
        if (!controller.signal.aborted) setResultado({ pedido: chave, itens, erro: null });
      })
      .catch((err) => {
        if (!controller.signal.aborted) {
          setResultado({ pedido: chave, itens: [], erro: mensagemDeErro(err, DESTINATARIOS_TEXTOS.ERRO_LISTA) });
        }
      });
    return () => controller.abort();
  }, [publico, inativos, tentativa]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);

  const gravar = (mudancas) =>
    setParams((atual) => {
      const proximos = { ...lerFiltrosDestinatarios(atual), ...mudancas };
      const saida = new URLSearchParams();
      if (proximos.publico) saida.set('publico', proximos.publico);
      if (proximos.inativos) saida.set('inativos', '1');
      return saida;
    });

  const salvar = async (id, dados) => {
    if (id) await alterarDestinatario(id, dados);
    else await cadastrarDestinatario(dados);
    recarregar();
  };

  const mudarSituacao = async (destinatario) => {
    if (destinatario.ativo) await desativarDestinatario(destinatario.id);
    else await reativarDestinatario(destinatario.id);
    recarregar();
  };

  const carregando = resultado === null || resultado.pedido !== pedido;

  return {
    itens: carregando ? [] : resultado.itens,
    carregando,
    erro: carregando ? null : resultado.erro,
    recarregar,
    publico,
    inativos,
    temFiltro: Boolean(publico || inativos),
    setPublico: (valor) => gravar({ publico: valor }),
    setInativos: (valor) => gravar({ inativos: valor }),
    limparFiltros: () => gravar({ publico: '', inativos: false }),
    salvar,
    mudarSituacao,
  };
}
