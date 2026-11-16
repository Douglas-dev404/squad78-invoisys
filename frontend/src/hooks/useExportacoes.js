import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { listarExportacoes } from '../services/exportacoes.service';
import { mensagemDeErro } from '../utils/revisao';

export const TAMANHO_PAGINA = 10;
const ESPERA_BUSCA_MS = 400;
const ERRO_LISTAR = 'Não foi possível carregar o histórico de exportações.';

/**
 * Histórico de exportações (F-12). Filtros e página ficam na URL
 * (`?pagina=2&publico=cliente&busca=INV`) para sobreviver ao recarregar.
 * A busca digitada só vai para a URL (e para a API) depois de 400 ms sem digitar.
 */
export function useExportacoes() {
  const [params, setParams] = useSearchParams();
  const pagina = Math.max(1, Number.parseInt(params.get('pagina') ?? '', 10) || 1);
  const publico = params.get('publico') ?? '';
  const busca = params.get('busca') ?? '';

  const [buscaDigitada, setBuscaDigitada] = useState(busca);
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  const atualizarUrl = useCallback(
    (mudancas, { substituir = false } = {}) => {
      setParams(
        (atuais) => {
          const proximos = new URLSearchParams(atuais);
          Object.entries(mudancas).forEach(([chave, valor]) => {
            const vazio = valor === '' || valor === null || valor === undefined || (chave === 'pagina' && Number(valor) <= 1);
            if (vazio) proximos.delete(chave);
            else proximos.set(chave, String(valor));
          });
          return proximos;
        },
        { replace: substituir },
      );
    },
    [setParams],
  );

  useEffect(() => {
    const termo = buscaDigitada.trim();
    if (termo === busca) return undefined;
    const temporizador = window.setTimeout(
      () => atualizarUrl({ busca: termo, pagina: 1 }, { substituir: true }),
      ESPERA_BUSCA_MS,
    );
    return () => window.clearTimeout(temporizador);
  }, [buscaDigitada, busca, atualizarUrl]);

  const pedido = `${pagina}|${publico}|${busca}|${tentativa}`;

  useEffect(() => {
    let cancelado = false;
    listarExportacoes({ pagina, tamanho: TAMANHO_PAGINA, publico: publico || undefined, busca: busca || undefined })
      .then((dados) => {
        if (!cancelado) setResultado({ pedido, dados, erro: null });
      })
      .catch((err) => {
        if (!cancelado) setResultado({ pedido, dados: null, erro: mensagemDeErro(err, ERRO_LISTAR) });
      });
    return () => {
      cancelado = true;
    };
  }, [pagina, publico, busca, pedido]);

  const carregando = resultado === null || resultado.pedido !== pedido;
  const dados = carregando ? null : resultado.dados;
  const total = dados?.total ?? 0;
  const tamanho = dados?.tamanho || TAMANHO_PAGINA;

  return {
    itens: dados?.itens ?? [],
    total,
    pagina,
    totalPaginas: Math.max(1, Math.ceil(total / tamanho)),
    carregando,
    erro: carregando ? null : resultado.erro,
    filtros: { publico, busca: buscaDigitada },
    temFiltro: Boolean(publico || busca),
    mudarBusca: setBuscaDigitada,
    mudarPublico: (valor) => atualizarUrl({ publico: valor, pagina: 1 }),
    irParaPagina: (numero) => atualizarUrl({ pagina: numero }),
    limparFiltros: () => {
      setBuscaDigitada('');
      atualizarUrl({ publico: '', busca: '', pagina: 1 });
    },
    recarregar: () => setTentativa((n) => n + 1),
  };
}
