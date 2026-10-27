import { useDashboard } from '../../hooks/useDashboard';
import { Layout } from '../../components/Layout/Layout';
import { SearchBar } from '../../components/SearchBar/SearchBar';
import { KPICard } from '../../components/KPICard/KPICard';
import { ProductivityChart } from '../../components/ProductivityChart/ProductivityChart';
import { CardsTable } from '../../components/CardsTable/CardsTable';
import { AlertError } from '../../components/AlertError/AlertError';

export function DashboardPage({ onLogout, onProfileClick, onNavigate }) {
  const {
    kpi,
    chart,
    cards,
    searchQuery,
    loading,
    error,
    handleSearch,
    clearError,
    refetch,
  } = useDashboard();

  const handleCardRowClick = (card) => {
    // TODO: Navegar para página de detalhes do card quando React Router estiver configurado
    // navigate(`/cards/${card.id}`);
    console.log('Card clicked:', card);
  };

  return (
    <Layout
      onLogout={onLogout}
      onProfileClick={onProfileClick}
      activeScreen="dashboard"
      onNavigate={onNavigate}
    >
      {/* Header */}
        <header className="mb-lg">
          <h2 className="font-headline-lg text-headline-lg text-primary">
            Dashboard Principal
          </h2>
          <p className="font-body-md text-body-md text-on-surface-variant mt-xs">
            Visão geral das atividades de formalização desta semana.
          </p>
        </header>

        {/* Error Alert */}
        {error && (
          <AlertError
            message={error}
            onDismiss={clearError}
            onRetry={refetch}
          />
        )}

        {/* Search Bar */}
        <SearchBar onSearch={handleSearch} isLoading={loading} />

        {/* KPI Grid */}
        {kpi && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-md mb-lg">
            <KPICard
              label="Cards Formalizados"
              value={kpi.formalizedCards.value}
              trend={
                <>
                  <span className="material-symbols-outlined text-[16px]">
                    trending_up
                  </span>
                  {kpi.formalizedCards.trend} {kpi.formalizedCards.trendLabel}
                </>
              }
              icon={kpi.formalizedCards.icon}
              backgroundColor="bg-primary-container"
            />
            <KPICard
              label="Em Revisão"
              value={kpi.inReview.value}
              trend={
                <>
                  <span className="material-symbols-outlined text-[16px]">
                    schedule
                  </span>
                  {kpi.inReview.trend}
                </>
              }
              icon={kpi.inReview.icon}
              backgroundColor="bg-primary-container"
            />
            <KPICard
              label="Enviados (Cliente)"
              value={kpi.sentToClient.value}
              trend={
                <>
                  <span className="material-symbols-outlined text-[16px]">
                    done_all
                  </span>
                  {kpi.sentToClient.trend}
                </>
              }
              icon={kpi.sentToClient.icon}
              backgroundColor="bg-tertiary-container"
            />
          </div>
        )}

        {/* Chart */}
        <ProductivityChart data={chart} isLoading={loading} />

      {/* Table */}
      <CardsTable
        cards={cards}
        isLoading={loading}
        onRowClick={handleCardRowClick}
      />
    </Layout>
  );
}
