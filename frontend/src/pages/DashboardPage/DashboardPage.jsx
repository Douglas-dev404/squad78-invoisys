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
    loading,
    error,
    handleSearch,
    clearError,
    refetch,
  } = useDashboard();


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
            Painel principal
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

        {!loading && !error && kpi && [kpi.formalizedCards.value, kpi.inReview.value, kpi.sentToClient.value].every((value) => value === 0) &&
          <p role="status" className="mb-md">Nenhuma atividade registrada. Os indicadores serão atualizados após o processamento de uma Release.</p>}
        {/* Search Bar */}
        <SearchBar onSearch={handleSearch} isLoading={loading} />

        {loading && <div role="status" className="grid grid-cols-1 xl:grid-cols-3 gap-md mb-lg">
          <span className="sr-only">Carregando indicadores...</span>
          {[1, 2, 3].map((id) => <div key={id} className="h-40 bg-surface-container animate-pulse rounded-xl" />)}
        </div>}
        {!loading && !error && !kpi && <p role="status">Nenhum indicador disponível.</p>}
        {/* KPI Grid */}
        {kpi && (
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-md mb-lg">
            <KPICard
              label="Comunicados formalizados"
              value={kpi.formalizedCards.value}
              trend={
                <>
                  <span aria-hidden="true" className="material-symbols-outlined text-[16px]">
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
                  <span aria-hidden="true" className="material-symbols-outlined text-[16px]">
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
                  <span aria-hidden="true" className="material-symbols-outlined text-[16px]">
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
      />
    </Layout>
  );
}
