import { useEffect, useMemo, useRef, useState } from 'react';
import { Layout } from '../../components/Layout/Layout';
import { Icon } from '../../components/Icon/Icon';
import { useExecucaoIa } from '../../hooks/useExecucaoIa';
import {
  formatarDuracaoMs,
  releaseDoEvento,
  releasesDosEventos,
  rotuloDoTipo,
  situacaoDaRelease,
} from '../../utils/execucaoIa';

const CARD = 'rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]';
const BOTAO = 'rounded-lg border border-outline-variant px-sm py-xs font-label-md text-label-md text-on-surface hover:bg-surface-container-low focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary';

const SITUACAO = {
  rodando: { label: 'Rodando', className: 'bg-amber-100 text-amber-800' },
  concluida: { label: 'Concluída', className: 'bg-green-100 text-green-800' },
  falhou: { label: 'Falhou', className: 'bg-error-container text-on-error-container' },
  ociosa: { label: '—', className: 'bg-surface-container text-on-surface-variant' },
};

const hora = (iso) => new Date(iso).toLocaleTimeString('pt-BR');

function BlocoTexto({ titulo, texto }) {
  return (
    <div>
      <p className="font-label-sm text-label-sm text-on-surface-variant">{titulo}</p>
      <pre className="mt-1 max-h-64 overflow-auto whitespace-pre-wrap rounded-lg bg-surface-container p-sm font-mono text-[12px] leading-5 text-on-surface">
        {texto}
      </pre>
    </div>
  );
}

function LinhaEvento({ evento }) {
  const tipo = rotuloDoTipo(evento.tipo);
  const temDetalhe = Boolean(evento.prompt || evento.resposta);
  return (
    <li className="space-y-xs py-sm">
      <div className="flex flex-wrap items-start gap-sm">
        <span className="w-16 shrink-0 pt-0.5 font-mono text-[12px] tabular-nums text-on-surface-variant">{hora(evento.em)}</span>
        <span className={`inline-flex shrink-0 items-center gap-1 font-label-md text-label-md ${tipo.cor}`}>
          <Icon name={tipo.icone} size={18} />
          {tipo.label}
        </span>
        <span className="min-w-0 flex-1 font-body-md text-body-md text-on-surface">{evento.mensagem}</span>
        {evento.duracaoMs != null && (
          <span className="shrink-0 rounded-full bg-surface-container px-2 py-0.5 text-label-sm text-on-surface-variant">
            {formatarDuracaoMs(evento.duracaoMs)}
          </span>
        )}
      </div>
      {temDetalhe && (
        <details className="ml-[4.5rem] space-y-xs">
          <summary className="cursor-pointer font-label-md text-label-md text-primary">
            {evento.resposta ? 'Ver a resposta da IA' : 'Ver o que foi enviado à IA'}
          </summary>
          <div className="mt-xs space-y-sm">
            {evento.prompt && <BlocoTexto titulo="Enviado à IA" texto={evento.prompt} />}
            {evento.resposta && <BlocoTexto titulo="Resposta" texto={evento.resposta} />}
          </div>
        </details>
      )}
    </li>
  );
}

export function ExecucaoIaPage() {
  const { eventos, pausado, erro, limpar, alternarPausa } = useExecucaoIa();
  const [filtro, setFiltro] = useState('todas');
  const [acompanhar, setAcompanhar] = useState(true);
  const fim = useRef(null);

  const releases = useMemo(() => releasesDosEventos(eventos), [eventos]);
  const visiveis = useMemo(
    () => (filtro === 'todas' ? eventos : eventos.filter((evento) => releaseDoEvento(evento) === filtro)),
    [eventos, filtro],
  );
  const rodando = releases.filter((release) => situacaoDaRelease(eventos, release) === 'rodando');

  useEffect(() => {
    if (acompanhar) fim.current?.scrollIntoView?.({ block: 'end' });
  }, [visiveis.length, acompanhar]);

  return (
    <Layout>
      <div className="mx-auto w-full max-w-[1100px] space-y-md">
        <header className="flex flex-wrap items-end justify-between gap-sm">
          <div>
            <h1 className="font-headline-lg text-headline-lg text-primary">Execução da IA</h1>
            <p className="font-body-md text-body-md text-on-surface-variant">
              O que o InvoiSys pergunta à IA e o que ela responde, enquanto acontece.
            </p>
          </div>
          <span
            role="status"
            className={`inline-flex items-center gap-2 rounded-full px-3 py-1 font-label-md text-label-md ${
              pausado ? 'bg-surface-container text-on-surface-variant' : 'bg-green-100 text-green-800'
            }`}
          >
            <span className={`h-2 w-2 rounded-full ${pausado ? 'bg-outline' : 'animate-pulse bg-green-600'}`} />
            {pausado ? 'Pausado' : rodando.length > 0 ? `Ao vivo · processando ${rodando.join(', ')}` : 'Ao vivo · aguardando execução'}
          </span>
        </header>

        <div className="flex flex-wrap items-center gap-sm">
          <label className="flex items-center gap-xs font-label-md text-label-md text-on-surface">
            Release
            <select
              value={filtro}
              onChange={(event) => setFiltro(event.target.value)}
              className="rounded-lg border border-outline-variant bg-surface-container-lowest px-sm py-xs font-body-md text-body-md"
            >
              <option value="todas">Todas</option>
              {releases.map((release) => (
                <option key={release} value={release}>
                  {release} · {SITUACAO[situacaoDaRelease(eventos, release)].label}
                </option>
              ))}
            </select>
          </label>
          <label className="flex items-center gap-xs font-label-md text-label-md text-on-surface">
            <input type="checkbox" checked={acompanhar} onChange={(event) => setAcompanhar(event.target.checked)} />
            Rolar sozinho
          </label>
          <div className="ml-auto flex gap-xs">
            <button type="button" onClick={alternarPausa} className={BOTAO}>{pausado ? 'Retomar' : 'Pausar'}</button>
            <button type="button" onClick={limpar} disabled={eventos.length === 0} className={`${BOTAO} disabled:opacity-50`}>Limpar tela</button>
          </div>
        </div>

        {erro && (
          <p role="alert" className="rounded-lg border border-error/20 bg-error-container p-sm font-body-sm text-body-sm text-on-error-container">
            {erro} Tentando de novo…
          </p>
        )}

        <section className={`${CARD} p-md`} aria-label="Eventos da execução" aria-live="polite">
          {visiveis.length === 0 ? (
            <div className="py-xl text-center">
              <Icon name="smart_toy" size={40} />
              <p className="mt-sm font-headline-sm text-headline-sm text-on-surface">Nada rodando agora</p>
              <p className="font-body-md text-body-md text-on-surface-variant">
                Processe uma Release ou teste a conexão da IA e acompanhe cada passo aparecer aqui.
              </p>
            </div>
          ) : (
            <ol className="divide-y divide-surface-variant/60">
              {visiveis.map((evento) => <LinhaEvento key={evento.id} evento={evento} />)}
            </ol>
          )}
          <div ref={fim} />
        </section>
      </div>
    </Layout>
  );
}
