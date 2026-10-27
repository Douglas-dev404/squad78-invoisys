import {
  dashboardKPIMock,
  dashboardChartMock,
  dashboardCardsMock,
} from '../data/dashboard.mock';
import { API_BASE_URL } from '../config/api.config';

const ENDPOINTS = {
  DASHBOARD_KPI: '/dashboard/kpi',
  DASHBOARD_CHART: '/dashboard/chart',
  DASHBOARD_CARDS: '/dashboard/cards',
};

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

export async function searchDashboardCards(query) {
  // TODO: Implementar busca real na API
  // const response = await fetch(`${API_BASE_URL}${ENDPOINTS.DASHBOARD_CARDS}?search=${query}`);
  // if (!response.ok) throw new Error('Failed to search cards');
  // return response.json();

  return new Promise((resolve) => {
    setTimeout(() => {
      const filtered = dashboardCardsMock.filter(
        (card) =>
          card.jiraId.toLowerCase().includes(query.toLowerCase()) ||
          card.title.toLowerCase().includes(query.toLowerCase()) ||
          card.responsible.toLowerCase().includes(query.toLowerCase())
      );
      resolve({
        data: filtered,
        total: filtered.length,
      });
    }, 600);
  });
}
