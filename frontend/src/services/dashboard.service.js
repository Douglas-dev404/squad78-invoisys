import {
  dashboardKPIMock,
  dashboardChartMock,
  dashboardCardsMock,
} from '../data/dashboard.mock';
export async function fetchDashboardKPI() {
  // TODO: Descomente a chamada real quando a API estiver pronta
  // const response = await fetch(`${API_BASE_URL}${ENDPOINTS.DASHBOARD_KPI}`);
  // if (!response.ok) throw new Error('Failed to fetch KPI data');
  // return response.json();

  // Simulação com latência de rede
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve(dashboardKPIMock);
    }, 800);
  });
}

export async function fetchDashboardChart() {
  // TODO: Descomente a chamada real quando a API estiver pronta
  // const response = await fetch(`${API_BASE_URL}${ENDPOINTS.DASHBOARD_CHART}`);
  // if (!response.ok) throw new Error('Failed to fetch chart data');
  // return response.json();

  return new Promise((resolve) => {
    setTimeout(() => {
      resolve(dashboardChartMock);
    }, 1000);
  });
}

export async function fetchDashboardCards(page = 1, limit = 10) {
  // TODO: Descomente a chamada real quando a API estiver pronta
  // const response = await fetch(`${API_BASE_URL}${ENDPOINTS.DASHBOARD_CARDS}?page=${page}&limit=${limit}`);
  // if (!response.ok) throw new Error('Failed to fetch cards');
  // return response.json();

  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        data: dashboardCardsMock,
        total: dashboardCardsMock.length,
        page,
        limit,
      });
    }, 900);
  });
}
