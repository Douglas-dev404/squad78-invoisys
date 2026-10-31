import { useMemo, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Layout } from '../../components/Layout/Layout';
import { Button } from '../../components/Button/Button';
import { ProcessarReleaseModal } from '../../components/ProcessarReleaseModal/ProcessarReleaseModal';
import { useReleases } from '../../hooks/useReleases';
import { UI_STRINGS } from '../../utils/constants';
import {
  PAGE_SIZE,
  RELEASE_FILTER_TABS,
  RELEASE_STATUS,
  formatarData,
} from '../../utils/releases';

const CARD_CLASS =
  'overflow-hidden rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]';

const BADGE_FALLBACK = 'bg-surface-container text-on-surface-variant';

function ReleasesSkeleton() {
  return (
    <section role="status" aria-live="polite" className={`${CARD_CLASS} space-y-sm p-md`}>
      {[...Array(5)].map((_, i) => (
        <div key={i} className="h-10 animate-pulse rounded-lg bg-surface-container" />
      ))}
      <span className="sr-only">{UI_STRINGS.RELEASES_LOADING}</span>
    </section>
  );
}

function ReleasesError({ message, onRetry }) {
  return (
    <div
      role="alert"
      className="flex flex-col items-start gap-sm rounded-xl border border-error bg-error-container p-md sm:flex-row sm:items-center sm:justify-between"
    >
      <p className="font-body-md text-body-md text-on-error-container">{message}</p>
      <Button onClick={onRetry}>{UI_STRINGS.RELEASES_RETRY}</Button>
    </div>
  );
}

function ReleasesEmpty() {
  return (
    <section className={`${CARD_CLASS} p-xl text-center`} role="status">
      <p className="font-body-md text-body-md text-on-surface-variant">
        {UI_STRINGS.RELEASES_EMPTY}
      </p>
    </section>
  );
}

function ReleasesTable({ itens, total, inicio, pagina, totalPaginas, onPageChange }) {
  const fim = inicio + itens.length;

  return (
    <section aria-label="Lista de Releases" className={CARD_CLASS}>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[480px] border-collapse text-left text-body-sm">
          <thead className="bg-surface-bright text-label-sm text-on-surface-variant">
            <tr>
              <th scope="col" className="px-md py-sm font-medium">{UI_STRINGS.RELEASES_COL_KEY}</th>
              <th scope="col" className="px-md py-sm font-medium">{UI_STRINGS.RELEASES_COL_STATUS}</th>
              <th scope="col" className="px-md py-sm font-medium">{UI_STRINGS.RELEASES_COL_CREATED}</th>
            </tr>
          </thead>
          <tbody>
            {itens.map((release) => {
              const selo = RELEASE_STATUS[release.status];
              return (
                <tr
                  key={release.chaveJira}
                  className="border-t border-surface-variant/60 transition-colors hover:bg-secondary-container/5"
                >
                  <td className="whitespace-nowrap px-md py-sm font-semibold">
                    <Link
                      to={`/releases/${encodeURIComponent(release.chaveJira)}`}
                      className="text-primary hover:underline"
                    >
                      {release.chaveJira}
                    </Link>
                  </td>
                  <td className="px-md py-sm">
                    <span
                      className={`inline-flex rounded-full px-2 py-1 text-label-sm font-semibold ${
                        selo ? selo.className : BADGE_FALLBACK
                      }`}
                    >
                      {selo ? selo.label : release.status}
                    </span>
                  </td>
                  <td className="whitespace-nowrap px-md py-sm text-on-surface-variant">
                    {formatarData(release.criadoEm)}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div className="flex flex-col gap-sm border-t border-surface-variant/60 px-md py-sm text-body-sm text-on-surface-variant sm:flex-row sm:items-center sm:justify-between">
        <span aria-live="polite">
          Mostrando {inicio + 1} a {fim} de {total} resultados
        </span>
        {totalPaginas > 1 && (
          <nav aria-label="Páginas de Releases" className="flex items-center gap-1">
            <button
              type="button"
              aria-label="Página anterior"
              disabled={pagina === 1}
              onClick={() => onPageChange(pagina - 1)}
              className="rounded-lg p-2 hover:bg-surface-container disabled:cursor-not-allowed disabled:opacity-40"
            >
              <span aria-hidden="true" className="material-symbols-outlined text-[18px]">chevron_left</span>
            </button>
            {Array.from({ length: totalPaginas }, (_, i) => i + 1).map((numero) => (
              <button
                key={numero}
                type="button"
                aria-label={`Página ${numero}`}
                aria-current={pagina === numero ? 'page' : undefined}
                onClick={() => onPageChange(numero)}
                className={`h-9 min-w-9 rounded-lg px-2 ${
                  pagina === numero ? 'bg-primary text-on-primary' : 'hover:bg-surface-container'
                }`}
              >
                {numero}
              </button>
            ))}
            <button
              type="button"
              aria-label="Próxima página"
              disabled={pagina === totalPaginas}
              onClick={() => onPageChange(pagina + 1)}
              className="rounded-lg p-2 hover:bg-surface-container disabled:cursor-not-allowed disabled:opacity-40"
            >
              <span aria-hidden="true" className="material-symbols-outlined text-[18px]">chevron_right</span>
            </button>
          </nav>
        )}
      </div>
    </section>
  );
}

export function ReleasesPage({ onLogout, onProfileClick, onNavigate }) {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const statusParam = searchParams.get('status');
  const status = statusParam && Object.hasOwn(RELEASE_STATUS, statusParam) ? statusParam : '';

  const { releases, carregando, erro, recarregar } = useReleases(status || undefined);

  const [modalAberto, setModalAberto] = useState(false);
  const [paginaSalva, setPaginaSalva] = useState({ status, numero: 1 });

  const ordenadas = useMemo(
    () => [...releases].sort((a, b) => new Date(b.criadoEm) - new Date(a.criadoEm)),
    [releases]
  );

  const totalPaginas = Math.max(1, Math.ceil(ordenadas.length / PAGE_SIZE));
  const pagina = Math.min(paginaSalva.status === status ? paginaSalva.numero : 1, totalPaginas);
  const inicio = (pagina - 1) * PAGE_SIZE;
  const visiveis = ordenadas.slice(inicio, inicio + PAGE_SIZE);

  const trocarAba = (valor) => setSearchParams(valor ? { status: valor } : {});
  const irParaPagina = (numero) => setPaginaSalva({ status, numero });
  const aoProcessar = (chave) => navigate(`/releases/${encodeURIComponent(chave)}`);

  let conteudo;
  if (carregando) {
    conteudo = <ReleasesSkeleton />;
  } else if (erro) {
    conteudo = <ReleasesError message={erro} onRetry={recarregar} />;
  } else if (ordenadas.length === 0) {
    conteudo = <ReleasesEmpty />;
  } else {
    conteudo = (
      <ReleasesTable
        itens={visiveis}
        total={ordenadas.length}
        inicio={inicio}
        pagina={pagina}
        totalPaginas={totalPaginas}
        onPageChange={irParaPagina}
      />
    );
  }

  return (
    <Layout
      onLogout={onLogout}
      onProfileClick={onProfileClick}
      activeScreen="releases"
      onNavigate={onNavigate}
    >
      <div className="mx-auto w-full max-w-[1280px]">
        <header className="mb-lg flex flex-col gap-sm sm:flex-row sm:items-center sm:justify-between">
          <h2 className="font-headline-lg text-headline-lg text-primary">
            {UI_STRINGS.RELEASES_TITLE}
          </h2>
          <Button onClick={() => setModalAberto(true)} className="self-start sm:self-auto">
            {UI_STRINGS.RELEASES_PROCESS_BUTTON}
          </Button>
        </header>

        <div
          role="tablist"
          aria-label="Filtrar Releases por status"
          className="mb-md flex flex-wrap gap-xs border-b border-surface-variant/60"
        >
          {RELEASE_FILTER_TABS.map((aba) => {
            const ativa = aba.value === status;
            return (
              <button
                key={aba.value || 'todas'}
                type="button"
                role="tab"
                aria-selected={ativa}
                onClick={() => trocarAba(aba.value)}
                className={`-mb-px border-b-2 px-sm py-xs font-label-md text-label-md transition-colors ${
                  ativa
                    ? 'border-primary text-primary'
                    : 'border-transparent text-on-surface-variant hover:text-primary'
                }`}
              >
                {aba.label}
              </button>
            );
          })}
        </div>

        {conteudo}
      </div>

      {modalAberto && (
        <ProcessarReleaseModal
          onClose={() => setModalAberto(false)}
          onSucesso={aoProcessar}
        />
      )}
    </Layout>
  );
}