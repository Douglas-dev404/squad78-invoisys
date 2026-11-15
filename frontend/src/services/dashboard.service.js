import { api } from './http.js';

const porcentagem = (parte, total) => (total > 0 ? Math.round((parte / total) * 100) : null);

// `data` é DateOnly em UTC ("2026-11-15"): formatar em UTC, senão no Brasil o rótulo cai no dia anterior.
const diaDaSemana = (data) =>
  new Date(`${data}T00:00:00Z`).toLocaleDateString('pt-BR', { weekday: 'short', timeZone: 'UTC' });

/**
 * Dashboard em uma chamada (`GET /dashboard`), já no formato de KPICard, ProductivityChart e CardsTable.
 * @returns {Promise<{ kpi: Object, chart: { data: Array }, cards: { data: Array }, qualidade: Object, vazio: boolean }>}
 */
export async function getDashboard() {
  const { totais, ultimos7Dias = [], recentes = [], qualidade } = await api.get('/dashboard');

  return {
    kpi: {
      formalizados: totais.aprovadas,
      emRevisao: totais.aguardandoRevisao,
      exportados: totais.exportacoes,
    },
    chart: {
      data: ultimos7Dias.map((dia) => ({
        chave: dia.data,
        dia: diaDaSemana(dia.data),
        aprovadas: dia.aprovadas,
        geradas: dia.geradas,
      })),
    },
    cards: {
      data: recentes.map((release) => ({
        id: release.chaveJira,
        chave: release.chaveJira,
        titulo: release.tituloExecutivo ?? '—',
        status: release.status,
        criadoEm: release.criadoEm,
      })),
    },
    qualidade: {
      ...qualidade,
      pctEditados: porcentagem(qualidade.itensEditados, qualidade.itensGerados),
      pctExcluidos: porcentagem(qualidade.itensExcluidos, qualidade.itensGerados),
    },
    vazio: recentes.length === 0 && Object.values(totais).every((valor) => valor === 0),
  };
}
