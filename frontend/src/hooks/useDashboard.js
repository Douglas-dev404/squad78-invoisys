import { useState, useMemo } from 'react';
import { fetchDashboardKPI, fetchDashboardChart, fetchDashboardCards } from '../services/dashboard.service';
import { useAsyncResource } from './useAsyncResource';

async function loadDashboard() {
  const [kpi, chart, cards] = await Promise.all([
    fetchDashboardKPI(), fetchDashboardChart(), fetchDashboardCards(),
  ]);
  return { kpi, chart, cards };
}

export function useDashboard() {
  const resource = useAsyncResource(loadDashboard);
  const [searchQuery, setSearchQuery] = useState('');
  const cards = useMemo(() => {
    if (!resource.data) return null;
    const query = searchQuery.trim().toLocaleLowerCase('pt-BR');
    const data = resource.data.cards.data.filter((card) =>
      `${card.jiraId} ${card.title} ${card.responsible}`.toLocaleLowerCase('pt-BR').includes(query));
    return { ...resource.data.cards, data, total: data.length };
  }, [resource.data, searchQuery]);
  return { ...resource, kpi: resource.data?.kpi, chart: resource.data?.chart,
    cards, searchQuery, handleSearch: setSearchQuery };
}
