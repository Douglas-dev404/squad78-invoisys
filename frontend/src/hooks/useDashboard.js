import { useCallback, useEffect, useState } from 'react';
import { getDashboard } from '../services/dashboard.service';
import { UI_STRINGS } from '../utils/constants';

/**
 * Carrega o Dashboard. `recarregar` refaz o GET (botão "Tentar de novo").
 * @returns {{ dados: Object|null, carregando: boolean, erro: string|null, recarregar: Function }}
 */
export function useDashboard() {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);

  useEffect(() => {
    let cancelado = false;

    getDashboard()
      .then((dados) => {
        if (!cancelado) setResultado({ tentativa, dados, erro: null });
      })
      .catch((err) => {
        if (!cancelado) {
          setResultado({ tentativa, dados: null, erro: err?.detail || err?.message || UI_STRINGS.DASHBOARD_LOAD_ERROR });
        }
      });

    return () => {
      cancelado = true;
    };
  }, [tentativa]);

  const recarregar = useCallback(() => setTentativa((n) => n + 1), []);

  const carregando = resultado === null || resultado.tentativa !== tentativa;

  return {
    dados: carregando ? null : resultado.dados,
    carregando,
    erro: carregando ? null : resultado.erro,
    recarregar,
  };
}
