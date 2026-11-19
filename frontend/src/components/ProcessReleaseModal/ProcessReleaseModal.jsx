import { useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { BOTAO_SECUNDARIO, BOTAO_TEXTO, Dialogo } from '../Dialogo/Dialogo';
import { InputField } from '../InputField/InputField';
import { useHistoriasVersao } from '../../hooks/useHistoriasVersao';
import { useVersoesJira } from '../../hooks/useVersoesJira';
import { processarRelease } from '../../services/releases.service';
import { UI_STRINGS } from '../../utils/constants';
import { RELEASE_STATUS } from '../../utils/releases';
import { ROTULO_TIPO, filtrarVersoes, formatarDataLancamento } from '../../utils/versoesJira';

const BADGE = 'inline-flex items-center rounded-full px-2 py-0.5 text-label-sm font-semibold';
const BADGE_FALLBACK = 'bg-surface-container text-on-surface-variant';
const CAIXA_ERRO =
  'flex items-center justify-between gap-sm rounded-lg border border-error/20 bg-error-container p-sm text-body-sm font-body-sm text-on-error-container';

function SeloStatus({ status }) {
  const selo = RELEASE_STATUS[status];
  return <span className={`${BADGE} ${selo ? selo.className : BADGE_FALLBACK}`}>{selo ? selo.label : status}</span>;
}

function ListaVersoes({ versoes, selecionada, onSelecionar }) {
  const aoTeclar = (event) => {
    if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;
    const itens = [...event.currentTarget.querySelectorAll('[role="option"]')];
    if (itens.length === 0) return;
    event.preventDefault();
    const atual = itens.indexOf(document.activeElement);
    let destino;
    if (event.key === 'Home') destino = 0;
    else if (event.key === 'End') destino = itens.length - 1;
    else if (event.key === 'ArrowDown') destino = Math.min(atual + 1, itens.length - 1);
    else destino = Math.max(atual - 1, 0);
    itens[destino].focus();
  };

  const comTabStop = versoes.some((v) => v.nome === selecionada) ? selecionada : versoes[0]?.nome;

  return (
    <ul
      role="listbox"
      aria-label={UI_STRINGS.PROCESS_MODAL_LIST_LABEL}
      onKeyDown={aoTeclar}
      className="max-h-56 divide-y divide-surface-variant/60 overflow-y-auto rounded-lg border border-outline-variant"
    >
      {versoes.map((versao) => {
        const ativa = versao.nome === selecionada;
        return (
          <li
            key={versao.nome}
            role="option"
            aria-selected={ativa}
            tabIndex={versao.nome === comTabStop ? 0 : -1}
            onClick={() => onSelecionar(versao.nome)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                onSelecionar(versao.nome);
              }
            }}
            className={`flex cursor-pointer flex-wrap items-center gap-xs px-sm py-xs outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary ${
              ativa ? 'bg-primary/5' : 'hover:bg-surface-container'
            }`}
          >
            <span className="font-label-md text-label-md text-on-surface">{versao.nome}</span>
            <span className="font-body-sm text-body-sm text-on-surface-variant">
              {formatarDataLancamento(versao.dataLancamento)}
            </span>
            {versao.lancada && (
              <span className={`${BADGE} bg-secondary-container text-on-secondary-container`}>
                {UI_STRINGS.PROCESS_MODAL_RELEASED}
              </span>
            )}
            {versao.statusRelease && <SeloStatus status={versao.statusRelease} />}
          </li>
        );
      })}
    </ul>
  );
}

function PreviaHistorias({ previa }) {
  const { carregando, erro, resumo, historias, recarregar } = previa;

  if (carregando) {
    return (
      <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
        {UI_STRINGS.PROCESS_MODAL_PREVIEW_LOADING}
      </p>
    );
  }
  if (erro) {
    return (
      <div role="alert" className={CAIXA_ERRO}>
        <span>{erro}</span>
        <button type="button" onClick={recarregar} className="shrink-0 font-label-md underline">
          {UI_STRINGS.RELEASES_RETRY}
        </button>
      </div>
    );
  }
  if (resumo.total === 0) {
    return (
      <p role="alert" className={CAIXA_ERRO}>
        {UI_STRINGS.PROCESS_MODAL_NO_STORIES}
      </p>
    );
  }

  const tipos = ['historia', 'bug', 'tarefa', 'outros'].filter((t) => t !== 'outros' || resumo.porTipo.outros > 0);

  return (
    <section aria-label={UI_STRINGS.PROCESS_MODAL_PREVIEW_TITLE} className="space-y-xs">
      <p className="font-label-md text-label-md text-on-surface">
        {resumo.total} {resumo.total === 1 ? 'história' : 'histórias'}
        <span className="font-body-sm text-body-sm text-on-surface-variant">
          {' · '}
          {tipos.map((t) => `${resumo.porTipo[t]} ${ROTULO_TIPO[t]}`).join(' · ')}
          {' · '}
          {resumo.comReleaseNote} com Release Note
        </span>
      </p>
      <ul
        tabIndex={0}
        aria-label={UI_STRINGS.PROCESS_MODAL_STORIES_LABEL}
        className="max-h-48 divide-y divide-surface-variant/60 overflow-y-auto rounded-lg border border-outline-variant"
      >
        {historias.map((h) => (
          <li key={h.chave} className="px-sm py-xs font-body-sm text-body-sm">
            <span className="font-semibold text-primary">{h.chave}</span>
            <span className="text-on-surface"> {h.titulo}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

/**
 * Modal "Processar Release" (F-17): escolhe a versão numa lista do Jira, mostra a prévia das histórias
 * e só então chama POST /releases/{chave}/processar. "Digitar manualmente" mantém o campo de texto como saída.
 * @param {Object} props
 * @param {Function} props.onClose
 * @param {Function} props.onSucesso recebe a chave processada
 */
export function ProcessarReleaseModal({ onClose, onSucesso }) {
  const [manual, setManual] = useState(false);
  const [busca, setBusca] = useState('');
  const [selecionada, setSelecionada] = useState(null);
  const [valorManual, setValorManual] = useState('');
  const [erroCampo, setErroCampo] = useState(undefined);
  const [erroApi, setErroApi] = useState(null);
  const [enviando, setEnviando] = useState(false);
  const enviandoRef = useRef(false);

  const jira = useVersoesJira();
  const previa = useHistoriasVersao(manual ? null : selecionada);

  const versao = jira.versoes.find((v) => v.nome === selecionada) ?? null;
  const visiveis = useMemo(() => filtrarVersoes(jira.versoes, busca), [jira.versoes, busca]);
  const aprovada = versao?.statusRelease === 'aprovado';
  const previaPronta = !previa.carregando && !previa.erro && previa.resumo.total > 0;
  const podeProcessar = manual ? true : Boolean(versao) && previaPronta && !aprovada;

  const selecionar = (nome) => {
    setSelecionada(nome);
    setErroApi(null);
  };

  const alternarModo = () => {
    setManual((v) => !v);
    setErroApi(null);
    setErroCampo(undefined);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (enviandoRef.current) return;

    const chave = manual ? valorManual.trim() : selecionada;
    if (!chave) {
      if (manual) setErroCampo(UI_STRINGS.PROCESS_MODAL_KEY_REQUIRED);
      return;
    }
    if (!podeProcessar) return;

    enviandoRef.current = true;
    setEnviando(true);
    setErroApi(null);

    try {
      await processarRelease(chave);
    } catch (err) {
      setErroApi(err?.detail || err?.message || UI_STRINGS.PROCESS_MODAL_GENERIC_ERROR);
      enviandoRef.current = false;
      setEnviando(false);
      return;
    }

    onSucesso(chave);
  };

  return (
    <Dialogo titulo={UI_STRINGS.PROCESS_MODAL_TITLE} onClose={onClose} bloqueado={enviando} largura="max-w-2xl">
      <form onSubmit={handleSubmit} noValidate className="space-y-md">
        {manual ? (
          <InputField
            id="chave-release"
            name="chave"
            label={UI_STRINGS.PROCESS_MODAL_KEY_LABEL}
            placeholder={UI_STRINGS.PROCESS_MODAL_KEY_PLACEHOLDER}
            value={valorManual}
            onChange={(event) => {
              setValorManual(event.target.value);
              if (erroCampo) setErroCampo(undefined);
              if (erroApi) setErroApi(null);
            }}
            error={erroCampo}
            required
            disabled={enviando}
          />
        ) : (
          <>
            {jira.carregando && (
              <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
                {UI_STRINGS.PROCESS_MODAL_LIST_LOADING}
              </p>
            )}

            {jira.erro && (
              <div role="alert" className={CAIXA_ERRO}>
                <span>{jira.erro}</span>
                <button type="button" onClick={jira.recarregar} className="shrink-0 font-label-md underline">
                  {UI_STRINGS.RELEASES_RETRY}
                </button>
              </div>
            )}

            {!jira.carregando && !jira.erro && jira.versoes.length === 0 && (
              <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
                {UI_STRINGS.PROCESS_MODAL_LIST_EMPTY}
              </p>
            )}

            {!jira.carregando && !jira.erro && jira.versoes.length > 0 && (
              <div className="space-y-xs">
                <label htmlFor="busca-versao" className="font-label-md text-label-md text-on-surface">
                  {UI_STRINGS.PROCESS_MODAL_SEARCH_LABEL}
                </label>
                <input
                  id="busca-versao"
                  type="search"
                  value={busca}
                  onChange={(event) => setBusca(event.target.value)}
                  disabled={enviando}
                  placeholder={UI_STRINGS.PROCESS_MODAL_SEARCH_PLACEHOLDER}
                  className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest p-sm font-body-md text-body-md focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/30"
                />
                {visiveis.length === 0 ? (
                  <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
                    {UI_STRINGS.PROCESS_MODAL_SEARCH_EMPTY}
                  </p>
                ) : (
                  <ListaVersoes versoes={visiveis} selecionada={selecionada} onSelecionar={selecionar} />
                )}
              </div>
            )}

            {versao?.statusRelease && (
              <div className="flex flex-wrap items-center gap-xs font-body-sm text-body-sm text-on-surface-variant">
                <span>
                  {aprovada ? UI_STRINGS.PROCESS_MODAL_APPROVED_WARNING : UI_STRINGS.PROCESS_MODAL_ALREADY_PROCESSED}
                </span>
                <Link
                  to={`/releases/${encodeURIComponent(versao.nome)}`}
                  onClick={onClose}
                  className="font-label-md text-primary underline"
                >
                  {UI_STRINGS.PROCESS_MODAL_SEE_DETAILS}
                </Link>
              </div>
            )}

            {versao && <PreviaHistorias previa={previa} />}
          </>
        )}

        <div>
          <button type="button" onClick={alternarModo} disabled={enviando} className={BOTAO_TEXTO}>
            {manual ? UI_STRINGS.PROCESS_MODAL_BACK_TO_LIST : UI_STRINGS.PROCESS_MODAL_MANUAL}
          </button>
        </div>

        {enviando && (
          <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
            {UI_STRINGS.PROCESS_MODAL_WAIT}
          </p>
        )}

        <Alert type="error" message={erroApi} />

        <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
          <button type="button" onClick={onClose} disabled={enviando} className={BOTAO_SECUNDARIO}>
            {UI_STRINGS.PROCESS_MODAL_CANCEL}
          </button>
          <Button type="submit" isLoading={enviando} disabled={!podeProcessar}>
            {enviando ? UI_STRINGS.PROCESS_MODAL_SUBMITTING : UI_STRINGS.PROCESS_MODAL_SUBMIT}
          </Button>
        </div>
      </form>
    </Dialogo>
  );
}