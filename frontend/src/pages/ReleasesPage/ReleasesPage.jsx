import { useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Layout } from '../../components/Layout/Layout';
import { Button } from '../../components/Button/Button';
import { ProcessarReleaseModal } from '../../components/ProcessReleaseModal/ProcessReleaseModal';
import { QuadroReleases } from '../../components/QuadroReleases/QuadroReleases';
import { useQuadroReleases } from '../../hooks/useQuadroReleases';
import { UI_STRINGS } from '../../utils/constants';
import { cartaoProcessa } from '../../utils/releases';

const CARD_CLASS =
  'overflow-hidden rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]';

function ReleasesSkeleton() {
  return (
    <section role="status" aria-live="polite" className="grid grid-cols-1 gap-md md:grid-cols-2 xl:grid-cols-4">
      {[...Array(4)].map((_, i) => (
        <div key={i} className="h-40 animate-pulse rounded-xl bg-surface-container" />
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
        Nenhuma Release no Jira nem processada ainda. Crie uma versão (fixVersion) no projeto do Jira.
      </p>
    </section>
  );
}

const normalizar = (texto) =>
  String(texto ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLocaleLowerCase('pt-BR');

export function ReleasesPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { cartoes, avisoJira, carregando, erro, recarregar } = useQuadroReleases();

  const [busca, setBusca] = useState('');
  const [modal, setModal] = useState(null);
  const modalManual = modal === 'manual' || searchParams.get('nova') === '1';
  const chaveDoModal = typeof modal === 'string' && modal !== 'manual' ? modal : null;

  const fecharModal = () => {
    setModal(null);
    if (searchParams.has('nova')) {
      const semNova = new URLSearchParams(searchParams);
      semNova.delete('nova');
      setSearchParams(semNova, { replace: true });
    }
  };

  const filtrados = useMemo(() => {
    const termo = normalizar(busca.trim());
    return termo
      ? cartoes.filter((cartao) => normalizar(`${cartao.chave} ${cartao.descricao ?? ''}`).includes(termo))
      : cartoes;
  }, [cartoes, busca]);

  const selecionar = (cartao) => {
    if (cartaoProcessa(cartao)) {
      setModal(cartao.chave);
    } else {
      navigate(`/releases/${encodeURIComponent(cartao.chave)}`);
    }
  };

  const aoProcessar = (chave) => {
    fecharModal();
    navigate(`/releases/${encodeURIComponent(chave)}`);
  };

  let conteudo;
  if (carregando) {
    conteudo = <ReleasesSkeleton />;
  } else if (erro) {
    conteudo = <ReleasesError message={erro} onRetry={recarregar} />;
  } else if (cartoes.length === 0) {
    conteudo = <ReleasesEmpty />;
  } else {
    conteudo = <QuadroReleases cartoes={filtrados} onSelecionar={selecionar} />;
  }

  return (
    <Layout>
      <div className="mx-auto w-full max-w-[1280px]">
        <header className="mb-md flex flex-col gap-sm sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-headline-lg text-headline-lg text-primary">{UI_STRINGS.RELEASES_TITLE}</h2>
            <p className="text-body-sm text-on-surface-variant">
              Versões do projeto no Jira. Clique numa Release para gerar ou revisar o comunicado.
            </p>
          </div>
          <div className="flex flex-wrap gap-sm self-start sm:self-auto">
            <button
              type="button"
              onClick={recarregar}
              disabled={carregando}
              className="inline-flex items-center gap-xs rounded-lg border border-outline-variant px-sm py-xs text-body-sm text-on-surface hover:bg-surface-container disabled:opacity-50"
            >
              <span aria-hidden="true" className="material-symbols-outlined text-[18px]">refresh</span>
              Atualizar do Jira
            </button>
            <button
              type="button"
              onClick={() => setModal('manual')}
              className="rounded-lg border border-primary px-sm py-xs text-body-sm font-medium text-primary hover:bg-primary/5"
            >
              Processar por chave
            </button>
          </div>
        </header>

        {avisoJira && (
          <div role="status" className="mb-md rounded-xl border border-amber-300 bg-amber-50 p-sm text-body-sm text-amber-900">
            Não foi possível ler as Releases do Jira: {avisoJira} Mostrando só as já processadas.
          </div>
        )}

        {!carregando && !erro && cartoes.length > 0 && (
          <label className="mb-md flex h-10 max-w-md items-center gap-xs rounded-lg border border-outline-variant bg-surface-container-lowest px-3 focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/15">
            <span aria-hidden="true" className="material-symbols-outlined text-[18px] text-on-surface-variant">search</span>
            <span className="sr-only">Buscar Release</span>
            <input
              type="search"
              value={busca}
              onChange={(event) => setBusca(event.target.value)}
              placeholder="Buscar pela versão ou descrição"
              className="min-w-0 w-full border-0 bg-transparent p-0 text-body-sm text-on-surface placeholder:text-on-surface-variant focus:ring-0"
            />
          </label>
        )}

        {conteudo}
      </div>

      {(modalManual || chaveDoModal) && (
        <ProcessarReleaseModal
          key={chaveDoModal ?? 'manual'}
          chave={chaveDoModal ?? undefined}
          onClose={fecharModal}
          onSucesso={aoProcessar}
        />
      )}
    </Layout>
  );
}
