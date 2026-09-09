import { useState, useEffect, useCallback } from 'react';
import {
  fetchDashboardKPI,
  fetchDashboardChart,
  fetchDashboardCards,
  searchDashboardCards,
} from '../services/dashboard.service';

export function useDashboard() {
  const [kpi, setKpi] = useState(null);
  const [chart, setChart] = useState(null);
  const [cards, setCards] = useState(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const loadDashboard = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [kpiData, chartData, cardsData] = await Promise.all([
        fetchDashboardKPI(),
        fetchDashboardChart(),
        fetchDashboardCards(),
      ]);
      setKpi(kpiData);
      setChart(chartData);
      setCards(cardsData);
    } catch (err) {
      setError(err.message || 'Erro ao carregar dashboard');
    } finally {
      setLoading(false);
    }
  }, []);

  const handleSearch = useCallback(async (query) => {
    setSearchQuery(query);
    if (!query.trim()) {
      await loadDashboard();
      return;
    }
    try {
      setLoading(true);
      setError(null);
      const searchResults = await searchDashboardCards(query);
      setCards(searchResults);
    } catch (err) {
      setError(err.message || 'Erro ao buscar cards');
    } finally {
      setLoading(false);
    }
  }, [loadDashboard]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  return {
    kpi,
    chart,
    cards,
    searchQuery,
    loading,
    error,
    handleSearch,
    clearError,
    refetch: loadDashboard,
  };
}
