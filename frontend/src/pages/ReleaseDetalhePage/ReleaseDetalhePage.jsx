import { useCallback, useId, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Layout } from '../../components/Layout/Layout';
import { Button } from '../../components/Button/Button';
import { Icon } from '../../components/Icon/Icon';
import { BarraRevisao } from '../../components/BarraRevisao/BarraRevisao';
import { ItemRevisavel } from '../../components/ItemRevisavel/ItemRevisavel';
import { useRelease } from '../../hooks/useRelease';
import { useRevisaoItem } from '../../hooks/useRevisaoItem';
import { UI_STRINGS } from '../../utils/constants';
import { RELEASE_STATUS, formatarData } from '../../utils/releases';
import { PUBLICO_LABEL, VERSAO_STATUS, acoesDaVersao, textoFinalDoItem } from '../../utils/revisao';

const CARD_CLASS =
  'rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]';

const BADGE_CLASS = 'inline-flex items-center rounded-full px-2 py-1 text-label-sm font-semibold';
const BADGE_FALLBACK = 'bg-surface-container text-on-surface-variant';

const FOCO = 'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2';

const CATEGORIAS = [
  { valor: 'nova_funcionalidade', titulo: 'Novas Funcionalidades' },
  { valor: 'melhoria', titulo: 'Melhorias' },
  { valor: 'correcao', titulo: 'Correções' },
  { valor: 'outros', titulo: 'Outros' },
];

function Selo({ mapa, status }) {
  const selo = mapa[status];
  return (
    <span className={`${BADGE_CLASS} ${selo ? selo.className : BADGE_FALLBACK}`}>
      {selo ? selo.label : status}
    </span>
  );
}

function formatarDuracao(inicio, fim) {
  if (!fim) return UI_STRINGS.RELEASE_DETALHE_EM_ANDAMENTO;
  const ms = new Date(fim) - new Date(inicio);
  if (!Number.isFinite(ms) || ms < 0) return '—';
  const segundos = Math.round(ms / 1000);
  if (segundos < 60) return `${segundos} s`;
  const minutos = Math.floor(segundos / 60);
  const resto = segundos % 60;
  return resto ? `${minutos} min ${resto} s` : `${minutos} min`;
}

const EXECUCAO_STATUS = { iniciada: 'Em andamento', concluida: 'Concluída', falhou: 'Falhou' };

const maisRecentesPrimeiro = (execucoes = []) =>
  [...execucoes].sort((a, b) => new Date(b.iniciadoEm) - new Date(a.iniciadoEm));

function ultimaExecucao(execucoes = []) {
  return maisRecentesPrimeiro(execucoes)[0] ?? null;
}

function DetalheSkeleton() {
  return (
    <div role="status" aria-live="polite" className="space-y-md">
      <div className="h-10 w-64 animate-pulse rounded-lg bg-surface-container" />
      <div className="grid gap-lg md:grid-cols-[minmax(0,1fr)_320px]">
        <div className={`${CARD_CLASS} space-y-sm p-md`}>
          {[...Array(6)].map((_, i) => (
            <div key={i} className="h-8 animate-pulse rounded-lg bg-surface-container" />
          ))}
        </div>
        <div className={`${CARD_CLASS} space-y-sm p-md`}>
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-6 animate-pulse rounded-lg bg-surface-container" />
          ))}
        </div>
      </div>
      <span className="sr-only">{UI_STRINGS.RELEASE_DETALHE_CARREGANDO}</span>
    </div>
  );
}

function NaoEncontrada({ chave }) {
  return (
    <section className={`${CARD_CLASS} p-xl text-center`} role="status">
      <h1 className="font-headline-md text-headline-md text-primary">
        {UI_STRINGS.RELEASE_DETALHE_NAO_ENCONTRADA}
      </h1>
      <p className="mt-xs font-body-md text-body-md text-on-surface-variant">{chave}</p>
      <Link to="/releases" className={`mt-md inline-block rounded text-primary hover:underline ${FOCO}`}>
        {UI_STRINGS.RELEASE_DETALHE_IR_PARA_LISTA}
      </Link>
    </section>
  );
}

function ErroCarregamento({ mensagem, onRetry }) {
  return (
    <div
      role="alert"
      className="flex flex-col items-start gap-sm rounded-xl border border-error bg-error-container p-md sm:flex-row sm:items-center sm:justify-between"
    >
      <p className="font-body-md text-body-md text-on-error-container">{mensagem}</p>
      <Button onClick={onRetry}>{UI_STRINGS.RELEASES_RETRY}</Button>
    </div>
  );
}

function Item({ item, podeEditar, revisao }) {
  const excluido = item.incluido === false;
  return (
    <li className="rounded-lg border border-surface-variant/60 p-sm">
      <ItemRevisavel item={item} podeEditar={podeEditar} revisao={revisao}>
        <ConteudoItem item={item} excluido={excluido} />
      </ItemRevisavel>
    </li>
  );
}

function ConteudoItem({ item, excluido }) {
  return (
    <div className="space-y-xs">
      <div className="flex flex-wrap items-start gap-xs">
        <p
          className={`min-w-0 flex-1 font-body-md text-body-md ${
            excluido ? 'text-on-surface-variant line-through opacity-60' : 'text-on-surface'
          }`}
        >
          {textoFinalDoItem(item)}
        </p>
        {item.textoEditadoManualmente != null && (
          <span className="inline-flex rounded-full border border-outline-variant px-2 py-0.5 text-label-sm text-on-surface-variant">
            {UI_STRINGS.RELEASE_DETALHE_EDITADO}
          </span>
        )}
      </div>
      {excluido && (
        <p className="font-body-sm text-body-sm text-on-surface-variant">
          {UI_STRINGS.RELEASE_DETALHE_EXCLUIDO}
          {item.motivoExclusao ? `: ${item.motivoExclusao}` : ''}
        </p>
      )}
      {item.origens?.length > 0 && (
        <ul aria-label={UI_STRINGS.RELEASE_DETALHE_ORIGENS} className="flex flex-wrap gap-xs">
          {item.origens.map((origem) => (
            <li
              key={origem}
              className="rounded-full bg-surface-container px-2 py-0.5 text-label-sm text-on-surface-variant"
            >
              {origem}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

/**
 * Corpo da aba de um público. A BarraRevisao (#71/#72) mostra quem aprovou e o motivo da reprovação,
 * por isso esta tela não repete essas informações.
 */
function CorpoVersao({ versao, chave, chavesHistorias, statusRelease, recarregar, revisaoItem }) {
  const itens = versao.itens ?? [];
  const acoes = acoesDaVersao(versao, statusRelease);
  return (
    <div className="space-y-md">
      <BarraRevisao
        chave={chave}
        versao={versao}
        chavesHistorias={chavesHistorias}
        statusRelease={statusRelease}
        recarregar={recarregar}
      />
      <article className={`${CARD_CLASS} space-y-xs p-md`}>
        <h2 className="font-headline-md text-headline-md text-primary">{versao.tituloExecutivo}</h2>
        <p className="whitespace-pre-line font-body-md text-body-md text-on-surface">{versao.resumoExecutivo}</p>
      </article>
      {CATEGORIAS.map(({ valor, titulo }) => {
        const daCategoria = itens.filter((item) => item.categoria === valor);
        if (daCategoria.length === 0) return null;
        return (
          <section key={valor} className={`${CARD_CLASS} space-y-sm p-md`}>
            <h3 className="font-headline-sm text-headline-sm text-on-surface">{titulo}</h3>
            <ul className="space-y-sm">
              {daCategoria.map((item) => (
                <Item key={item.id} item={item} podeEditar={acoes.editarItens} revisao={revisaoItem} />
              ))}
            </ul>
          </section>
        );
      })}
    </div>
  );
}

function AbasVersoes({ versoes, ...revisao }) {
  const base = useId();
  const [selecionado, setSelecionado] = useState(null);
  const abas = useRef([]);

  const ativa = versoes.find((v) => v.publico === selecionado) ?? versoes[0];
  const indiceAtivo = versoes.indexOf(ativa);

  const irPara = (indice) => {
    const destino = (indice + versoes.length) % versoes.length;
    setSelecionado(versoes[destino].publico);
    abas.current[destino]?.focus();
  };

  const aoTeclar = (evento) => {
    const acoes = {
      ArrowRight: () => irPara(indiceAtivo + 1),
      ArrowLeft: () => irPara(indiceAtivo - 1),
      Home: () => irPara(0),
      End: () => irPara(versoes.length - 1),
    };
    if (acoes[evento.key]) {
      evento.preventDefault();
      acoes[evento.key]();
    }
  };

  return (
    <div>
      <div
        role="tablist"
        aria-label={UI_STRINGS.RELEASE_DETALHE_ABAS}
        className="mb-md flex flex-wrap gap-xs border-b border-surface-variant/60"
      >
        {versoes.map((versao, indice) => {
          const selecionada = versao === ativa;
          return (
            <button
              key={versao.publico}
              ref={(el) => {
                abas.current[indice] = el;
              }}
              id={`${base}-aba-${versao.publico}`}
              type="button"
              role="tab"
              aria-selected={selecionada}
              aria-controls={`${base}-painel-${versao.publico}`}
              tabIndex={selecionada ? 0 : -1}
              onClick={() => setSelecionado(versao.publico)}
              onKeyDown={aoTeclar}
              className={`-mb-px flex items-center gap-xs rounded-t-lg border-b-2 px-sm py-xs font-label-md text-label-md transition-colors ${FOCO} ${
                selecionada
                  ? 'border-primary text-primary'
                  : 'border-transparent text-on-surface-variant hover:text-primary'
              }`}
            >
              {PUBLICO_LABEL[versao.publico] ?? versao.publico}
              <Selo mapa={VERSAO_STATUS} status={versao.status} />
            </button>
          );
        })}
      </div>
      <div
        role="tabpanel"
        id={`${base}-painel-${ativa.publico}`}
        aria-labelledby={`${base}-aba-${ativa.publico}`}
        tabIndex={0}
        className={`rounded-xl ${FOCO}`}
      >
        <CorpoVersao versao={ativa} {...revisao} />
      </div>
    </div>
  );
}

function SecaoLateral({ titulo, children }) {
  const [aberta, setAberta] = useState(false);
  const idConteudo = useId();
  return (
    <section className={`${CARD_CLASS} p-md`}>
      <h2 className="font-headline-sm text-headline-sm text-on-surface">
        <button
          type="button"
          aria-expanded={aberta}
          aria-controls={idConteudo}
          onClick={() => setAberta((v) => !v)}
          className={`flex w-full items-center justify-between rounded md:hidden ${FOCO}`}
        >
          {titulo}
          <Icon name={aberta ? 'expand_less' : 'expand_more'} size={20} />
        </button>
        <span className="hidden md:inline">{titulo}</span>
      </h2>
      <div id={idConteudo} className={`${aberta ? 'block' : 'hidden'} mt-sm md:block`}>
        {children}
      </div>
    </section>
  );
}

function PainelLateral({ historias = [], execucoes = [] }) {
  return (
    <aside className="space-y-md">
      <SecaoLateral titulo={UI_STRINGS.RELEASE_DETALHE_HISTORIAS}>
        {historias.length === 0 ? (
          <p className="font-body-sm text-body-sm text-on-surface-variant">
            {UI_STRINGS.RELEASE_DETALHE_SEM_HISTORIAS}
          </p>
        ) : (
          <ul className="divide-y divide-surface-variant/60">
            {historias.map((historia) => (
              <li key={historia.chave} className="py-xs font-body-sm text-body-sm">
                <div className="flex items-center gap-xs">
                  <span className="font-semibold text-primary">{historia.chave}</span>
                  <span className="text-on-surface-variant">· {historia.tipoIssue}</span>
                  {historia.possuiReleaseNoteDedicada && (
                    <span title={UI_STRINGS.RELEASE_DETALHE_RN_DEDICADA} className="text-primary">
                      <Icon
                        name="sticky_note_2"
                        size={18}
                        ariaHidden={false}
                        ariaLabel={UI_STRINGS.RELEASE_DETALHE_RN_DEDICADA}
                      />
                    </span>
                  )}
                </div>
                <p className="text-on-surface">{historia.titulo}</p>
              </li>
            ))}
          </ul>
        )}
      </SecaoLateral>

      <SecaoLateral titulo={UI_STRINGS.RELEASE_DETALHE_EXECUCOES}>
        {execucoes.length === 0 ? (
          <p className="font-body-sm text-body-sm text-on-surface-variant">
            {UI_STRINGS.RELEASE_DETALHE_SEM_EXECUCOES}
          </p>
        ) : (
          <ul className="divide-y divide-surface-variant/60">
            {maisRecentesPrimeiro(execucoes).map((execucao) => (
              <li key={execucao.iniciadoEm} className="space-y-0.5 py-xs font-body-sm text-body-sm">
                <p className={`font-semibold ${execucao.status === 'falhou' ? 'text-error' : 'text-on-surface'}`}>
                  {EXECUCAO_STATUS[execucao.status] ?? execucao.status}
                </p>
                <dl className="grid grid-cols-[auto_1fr] gap-x-sm text-on-surface-variant">
                  <dt>Modelo</dt>
                  <dd className="text-on-surface">{execucao.modeloLlm ?? '—'}</dd>
                  <dt>Início</dt>
                  <dd className="text-on-surface">{formatarData(execucao.iniciadoEm)}</dd>
                  <dt>Duração</dt>
                  <dd className="text-on-surface">{formatarDuracao(execucao.iniciadoEm, execucao.concluidoEm)}</dd>
                </dl>
                {execucao.erro && <p className="text-error">{execucao.erro}</p>}
              </li>
            ))}
          </ul>
        )}
      </SecaoLateral>
    </aside>
  );
}

function SemVersoes({ execucoes }) {
  const erro = ultimaExecucao(execucoes)?.erro;
  return (
    <section className={`${CARD_CLASS} space-y-xs p-xl text-center`} role="status">
      <p className="font-headline-sm text-headline-sm text-on-surface">{UI_STRINGS.RELEASE_DETALHE_SEM_VERSOES}</p>
      {erro && <p className="font-body-md text-body-md text-error">{erro}</p>}
    </section>
  );
}

/** Aplica sobre as versões os itens que a API devolveu depois de editar/excluir/reincluir (sem refazer o GET). */
function comItensAtualizados(versoes, atualizados) {
  return versoes.map((versao) => ({
    ...versao,
    itens: (versao.itens ?? []).map((item) => (atualizados[item.id] ? { ...item, ...atualizados[item.id] } : item)),
  }));
}

function Detalhe({ release, recarregar }) {
  const [atualizados, setAtualizados] = useState({});
  const onItemAtualizado = useCallback((item) => setAtualizados((m) => ({ ...m, [item.id]: item })), []);
  const revisaoItem = useRevisaoItem({ chave: release.chaveJira, onItemAtualizado });
  // depois de aprovar/reabrir/reprocessar o GET inteiro volta a ser a fonte da verdade
  const recarregarTudo = useCallback(() => {
    setAtualizados({});
    recarregar();
  }, [recarregar]);
  const versoes = comItensAtualizados(release.versoes ?? [], atualizados);
  return (
    <>
      <header className="mb-lg space-y-xs">
        <Link
          to="/releases"
          className={`inline-block rounded font-label-md text-label-md text-primary hover:underline ${FOCO}`}
        >
          {UI_STRINGS.RELEASE_DETALHE_VOLTAR}
        </Link>
        <div className="flex flex-wrap items-center gap-sm">
          <h1 className="font-headline-lg text-headline-lg text-primary">{release.chaveJira}</h1>
          <Selo mapa={RELEASE_STATUS} status={release.status} />
        </div>
      </header>
      <div className="grid gap-lg md:grid-cols-[minmax(0,1fr)_320px]">
        <div className="min-w-0">
          {versoes.length === 0 ? (
            <SemVersoes execucoes={release.execucoes} />
          ) : (
            <AbasVersoes
              key={release.chaveJira}
              versoes={versoes}
              chave={release.chaveJira}
              chavesHistorias={(release.historias ?? []).map((historia) => historia.chave)}
              statusRelease={release.status}
              recarregar={recarregarTudo}
              revisaoItem={revisaoItem}
            />
          )}
        </div>
        <PainelLateral historias={release.historias} execucoes={release.execucoes} />
      </div>
    </>
  );
}

export function ReleaseDetalhePage() {
  const { chave } = useParams();
  const { release, carregando, erro, recarregar } = useRelease(chave);

  let conteudo;
  if (carregando) {
    conteudo = <DetalheSkeleton />;
  } else if (erro?.status === 404) {
    conteudo = <NaoEncontrada chave={chave} />;
  } else if (erro) {
    conteudo = (
      <ErroCarregamento mensagem={erro.detail || erro.message || UI_STRINGS.RELEASE_DETALHE_ERRO} onRetry={recarregar} />
    );
  } else {
    conteudo = <Detalhe release={release} recarregar={recarregar} />;
  }

  return (
    <Layout>
      <div className="mx-auto w-full max-w-[1280px]">{conteudo}</div>
    </Layout>
  );
}
