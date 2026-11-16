import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { listarExportacoes, TAMANHO_PAGINA } from '../services/exportacoes.service';
import { PUBLICO_LABEL } from '../utils/revisao';

const DEBOUNCE_MS = 400;

function lerFiltros(params) {
  const numero = Number.parseInt(params.get('pagina') ?? '', 10);
  const publico = params.get('publico') ?? '';
  return {
    pagina: Number.isInteger(numero) && numero >= 1 ? numero : 1,
    publico: Object.hasOwn(PUBLICO_LABEL, publico) ? publico : '',
    busca: (params.get('busca') ?? '').trim(),
  };
}

/**
 * Histórico de exportações com filtros na URL (`?pagina=2&publico=cliente&busca=INV`).
 * A busca digitada vai para a URL depois de 400 ms sem digitação; público e página vão na hora.
 */
export function useExportacoes() {
  const [params, setParams] = useSearchParams();
  const { pagina, publico, busca } = lerFiltros(params);

  const [textoBusca, setTextoBusca] = useState(busca);
  const [buscaNaUrl, setBuscaNaUrl] = useState(busca);
  const [buscaGravada, setBuscaGravada] = useState(busca);
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  // URL mudou por fora (menu, voltar do navegador): o campo acompanha. Gravação nossa não mexe no que está sendo digitado.
  if (busca !== buscaNaUrl) {
    setBuscaNaUrl(busca);
    if (busca !== buscaGravada) {
      setBuscaGravada(busca);
      setTextoBusca(busca);
    }
  }

  const gravar = useCallback(
    (mudancas, { replace = false } = {}) => {
      setParams(
        (atual) => {
          const proximos = { ...lerFiltros(atual), ...mudancas };
          const saida = new URLSearchParams();
          if (proximos.pagina > 1) saida.set('pagina', String(proximos.pagina));
          if (proximos.publico) saida.set('publico', proximos.publico);
          if (proximos.busca.trim()) saida.set('busca', proximos.busca.trim());
          return saida;
        },
        { replace }
      );
    },
    [setParams]
  );

  useEffect(() => {
    const digitada = textoBusca.trim();
    if (digitada === busca) return undefined;
    const timer = setTimeout(() => {
      setBuscaGravada(digitada);
      gravar({ busca: digitada, pagina: 1 }, { replace: true });
    }, DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [textoBusca, busca, gravar]);

  const pedido = `${pagina}|${publico}|${busca}|${tentativa}`;

  useEffect(() => {
    const controller = new AbortController();
    const chave = `${pagina}|${publico}|${busca}|${tentativa}`;
    listarExportacoes({ pagina, tamanho: TAMANHO_PAGINA, publico, busca }, { signal: controller.signal })
      .then((dados) => {
        if (!controller.signal.aborted) setResultado({ pedido: chave, dados, erro: null });
      })
      .catch((err) => {
        if (!controller.signal.aborted) {
          setResultado({ pedido: chave, dados: null, erro: err?.detail || err?.message || 'Não foi possível carregar o histórico.' });
        }
      });
    return () => controller.abort();
  }, [pagina, publico, busca, tentativa]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);

  const carregando = resultado === null || resultado.pedido !== pedido;
  const dados = carregando ? null : resultado.dados;
  const total = dados?.total ?? 0;
  const tamanho = dados?.tamanho ?? TAMANHO_PAGINA;

  return {
    itens: dados?.itens ?? [],
    total,
    pagina,
    totalPaginas: Math.max(1, Math.ceil(total / tamanho)),
    carregando,
    erro: carregando ? null : resultado.erro,
    recarregar,
    busca: textoBusca,
    publico,
    temFiltro: Boolean(publico || busca),
    setBusca: setTextoBusca,
    setPublico: (valor) => gravar({ publico: valor, pagina: 1 }),
    irParaPagina: (numero) => gravar({ pagina: numero }),
    limparFiltros: () => {
      setTextoBusca('');
      setBuscaGravada('');
      gravar({ publico: '', busca: '', pagina: 1 });
    },
  };
}
