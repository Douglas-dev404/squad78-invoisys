import { Link, useNavigate } from 'react-router-dom';
import { useDashboard } from '../../hooks/useDashboard';
import { Layout } from '../../components/Layout/Layout';
import { Button } from '../../components/Button/Button';
import { KPICard } from '../../components/KPICard/KPICard';
import { ProductivityChart } from '../../components/ProductivityChart/ProductivityChart';
import { CardsTable } from '../../components/CardsTable/CardsTable';
import { UI_STRINGS } from '../../utils/constants';

const CARD_CLASS =
  'rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]';

function KPISkeleton() {
  return (
    <div role="status" aria-live="polite" className="mb-lg grid grid-cols-1 gap-md md:grid-cols-3">
      {[...Array(3)].map((_, i) => (
        <div key={i} className="h-36 animate-pulse rounded-xl bg-surface-container" />
      ))}
      <span className="sr-only">Carregando indicadores…</span>
    </div>
  );
}

function ErroDashboard({ mensagem, onRetry }) {
  return (
    <div
      role="alert"
      className="mb-lg flex flex-col items-start gap-sm rounded-xl border border-error bg-error-container p-md sm:flex-row sm:items-center sm:justify-between"
    >
      <p className="font-body-md text-body-md text-on-error-container">{mensagem}</p>
      <Button onClick={onRetry}>{UI_STRINGS.RELEASES_RETRY}</Button>
    </div>
  );
}

function DashboardVazio() {
  return (
    <section className={`${CARD_CLASS} p-xl text-center`} role="status">
      <p className="font-headline-sm text-headline-sm text-primary">{UI_STRINGS.DASHBOARD_EMPTY}</p>
      <p className="mt-xs font-body-md text-body-md text-on-surface-variant">{UI_STRINGS.DASHBOARD_EMPTY_HINT}</p>
      <Link
        to="/releases"
        className="mt-md inline-flex items-center justify-center rounded-lg bg-primary-container px-4 py-3 font-label-md text-label-md text-on-primary hover:bg-tertiary-container focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
      >
        {UI_STRINGS.DASHBOARD_EMPTY_BUTTON}
      </Link>
    </section>
  );
}

function Indicador({ pct, frase, base }) {
  return (
    <div>
      <p className="font-headline-lg text-headline-lg text-primary">{pct === null ? '—' : `${pct}%`}</p>
      <p className="font-body-md text-body-md text-on-surface">{pct === null ? frase.semBase : frase.comBase}</p>
      {pct !== null && <p className="font-body-sm text-body-sm text-on-surface-variant">de {base} itens gerados</p>}
    </div>
  );
}

function QualidadeIA({ qualidade }) {
  return (
    <section className={`${CARD_CLASS} mb-lg p-lg`}>
      <h3 className="mb-md font-headline-sm text-headline-sm text-primary">{UI_STRINGS.DASHBOARD_QUALITY_TITLE}</h3>
      <div className="grid grid-cols-1 gap-md sm:grid-cols-2">
        <Indicador
          pct={qualidade.pctEditados}
          base={qualidade.itensGerados}
          frase={{ comBase: 'dos itens precisaram de edição', semBase: 'Nenhum item gerado ainda' }}
        />
        <Indicador
          pct={qualidade.pctExcluidos}
          base={qualidade.itensGerados}
          frase={{ comBase: 'dos itens foram excluídos', semBase: 'Nenhum item gerado ainda' }}
        />
      </div>
    </section>
  );
}

export function DashboardPage() {
  const navigate = useNavigate();
  const { dados, carregando, erro, recarregar } = useDashboard();

  const abrirRelease = (card) => navigate(`/releases/${encodeURIComponent(card.chave)}`);

  let conteudo;
  if (erro) {
    conteudo = <ErroDashboard mensagem={erro} onRetry={recarregar} />;
  } else if (dados?.vazio) {
    conteudo = <DashboardVazio />;
  } else {
    conteudo = (
      <>
        {carregando ? (
          <KPISkeleton />
        ) : (
          <div className="mb-lg grid grid-cols-1 gap-md md:grid-cols-3">
            <KPICard
              label="Cards Formalizados"
              value={dados.kpi.formalizados}
              icon="check_circle"
              backgroundColor="bg-primary-container"
            />
            <KPICard
              label="Em Revisão"
              value={dados.kpi.emRevisao}
              icon="pending_actions"
              backgroundColor="bg-primary-container"
            />
            <KPICard
              label="Exportados"
              value={dados.kpi.exportados}
              icon="download"
              backgroundColor="bg-tertiary-container"
            />
          </div>
        )}

        <ProductivityChart data={dados?.chart} isLoading={carregando} />

        {!carregando && <QualidadeIA qualidade={dados.qualidade} />}

        <CardsTable cards={dados?.cards} isLoading={carregando} onRowClick={abrirRelease} />
      </>
    );
  }

  return (
    <Layout>
      <header className="mb-lg">
        <h2 className="font-headline-lg text-headline-lg text-primary">Dashboard Principal</h2>
        <p className="mt-xs font-body-md text-body-md text-on-surface-variant">
          Visão geral das atividades de formalização desta semana.
        </p>
      </header>

      {conteudo}
    </Layout>
  );
}
