import { useEffect, useMemo, useRef, useState } from 'react';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { SelectField } from '../SelectField/SelectField';
import {
  buscarHistoriasDaRelease,
  listarVersoesJira,
  processarRelease,
} from '../../services/releases.service';
import { UI_STRINGS } from '../../utils/constants';

/**
 * Modal "Processar Release": envia a release e as histórias selecionadas ao pipeline.
 * @param {Object} props
 * @param {Function} props.onClose - Fecha o modal
 * @param {Function} props.onSucesso - Recebe a chave processada (a página navega)
 */
export function ProcessarReleaseModal({ onClose, onSucesso }) {
  const [valor, setValor] = useState('');
  const [erroCampo, setErroCampo] = useState(undefined);
  const [erroApi, setErroApi] = useState(null);
  const [versoes, setVersoes] = useState([]);
  const [historias, setHistorias] = useState([]);
  const [chavesSelecionadas, setChavesSelecionadas] = useState([]);
  const [carregandoVersoes, setCarregandoVersoes] = useState(true);
  const [carregandoHistorias, setCarregandoHistorias] = useState(false);
  const [erroVersoes, setErroVersoes] = useState(null);
  const [erroHistorias, setErroHistorias] = useState(null);
  const [enviando, setEnviando] = useState(false);
  const enviandoRef = useRef(false);
  const dialogRef = useRef(null);

  useEffect(() => {
    dialogRef.current?.focus();
  }, []);

  useEffect(() => {
    let cancelado = false;
    setHistorias([]);
    setChavesSelecionadas([]);
    setErroHistorias(null);

    if (!valor) {
      setCarregandoHistorias(false);
      return undefined;
    }

    setCarregandoHistorias(true);
    buscarHistoriasDaRelease(valor)
      .then((dados) => {
        if (cancelado) return;
        const lista = Array.isArray(dados?.historias) ? dados.historias : [];
        setHistorias(lista);
        setChavesSelecionadas(lista.map((historia) => historia.chave));
      })
      .catch((err) => {
        if (!cancelado) {
          setErroHistorias(err?.detail || err?.message || UI_STRINGS.PROCESS_MODAL_STORIES_ERROR);
        }
      })
      .finally(() => {
        if (!cancelado) setCarregandoHistorias(false);
      });

    return () => {
      cancelado = true;
    };
  }, [valor]);

  useEffect(() => {
    let cancelado = false;

    listarVersoesJira()
      .then((dados) => {
        if (!cancelado) setVersoes(Array.isArray(dados) ? dados : []);
      })
      .catch((err) => {
        if (!cancelado) setErroVersoes(err?.detail || err?.message || UI_STRINGS.PROCESS_MODAL_VERSIONS_ERROR);
      })
      .finally(() => {
        if (!cancelado) setCarregandoVersoes(false);
      });

    return () => {
      cancelado = true;
    };
  }, []);

  const opcoesVersao = useMemo(
    () => [...versoes]
      .sort((a, b) => a.nome.localeCompare(b.nome, undefined, { numeric: true, sensitivity: 'base' }))
      .map((versao) => {
        const estado = versao.arquivada ? ' · arquivada' : versao.lancada ? ' · lançada' : '';
        return { value: versao.nome, label: `${versao.nome}${estado}` };
      }),
    [versoes]
  );

  useEffect(() => {
    if (enviando) return undefined;
    const aoTeclar = (event) => {
      if (event.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', aoTeclar);
    return () => window.removeEventListener('keydown', aoTeclar);
  }, [enviando, onClose]);

  const handleChange = (event) => {
    const novaVersao = event.target.value;
    setValor(novaVersao);
    setHistorias([]);
    setChavesSelecionadas([]);
    setErroHistorias(null);
    setCarregandoHistorias(Boolean(novaVersao));
    if (erroCampo) setErroCampo(undefined);
    if (erroApi) setErroApi(null);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (enviandoRef.current) return;

    const chave = valor.trim();
    if (!chave) {
      setErroCampo(UI_STRINGS.PROCESS_MODAL_KEY_REQUIRED);
      return;
    }
    if (chavesSelecionadas.length === 0) {
      setErroCampo(UI_STRINGS.PROCESS_MODAL_STORIES_REQUIRED);
      return;
    }

    enviandoRef.current = true;
    setEnviando(true);
    setErroApi(null);

    try {
      await processarRelease(chave, chavesSelecionadas);
    } catch (err) {
      setErroApi(err?.detail || err?.message || UI_STRINGS.PROCESS_MODAL_GENERIC_ERROR);
      enviandoRef.current = false;
      setEnviando(false);
      return;
    }

    onSucesso(chave);
  };

  const alternarHistoria = (chaveHistoria) => {
    setChavesSelecionadas((atuais) => atuais.includes(chaveHistoria)
      ? atuais.filter((chave) => chave !== chaveHistoria)
      : [...atuais, chaveHistoria]);
    if (erroCampo) setErroCampo(undefined);
  };

  const selecionarTodas = () => {
    setChavesSelecionadas(historias.map((historia) => historia.chave));
    if (erroCampo) setErroCampo(undefined);
  };

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-black/40 p-gutter"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !enviando) onClose();
      }}
    >
      <div
        ref={dialogRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby="processar-release-titulo"
        className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-xl border border-surface-variant/50 bg-surface-container-lowest p-md shadow-[0px_4px_12px_rgba(27,67,50,0.08)] outline-none"
      >
        <div className="mb-md flex items-start justify-between gap-sm">
          <h3
            id="processar-release-titulo"
            className="font-headline-sm text-headline-sm text-primary"
          >
            {UI_STRINGS.PROCESS_MODAL_TITLE}
          </h3>
          <button
            type="button"
            onClick={onClose}
            disabled={enviando}
            aria-label="Fechar"
            className="text-on-surface-variant transition-colors hover:text-on-surface disabled:cursor-not-allowed disabled:opacity-40"
          >
            <span aria-hidden="true" className="material-symbols-outlined">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit} noValidate className="space-y-md">
          <SelectField
            id="chave-release"
            name="chave"
            label={UI_STRINGS.PROCESS_MODAL_KEY_LABEL}
            placeholder={carregandoVersoes
              ? UI_STRINGS.PROCESS_MODAL_VERSIONS_LOADING
              : UI_STRINGS.PROCESS_MODAL_KEY_PLACEHOLDER}
            value={valor}
            onChange={handleChange}
            options={opcoesVersao}
            error={valor ? undefined : erroCampo}
            required
            disabled={enviando || carregandoVersoes || Boolean(erroVersoes) || opcoesVersao.length === 0}
          />

          {valor && (
            <section aria-labelledby="historias-release-label" className="space-y-sm">
              <div className="flex flex-wrap items-center justify-between gap-sm">
                <h4 id="historias-release-label" className="font-label-md text-label-md text-on-surface">
                  {UI_STRINGS.PROCESS_MODAL_STORIES_LABEL}
                </h4>
                {!carregandoHistorias && !erroHistorias && historias.length > 0 && (
                  <div className="flex gap-sm">
                    <button
                      type="button"
                      onClick={selecionarTodas}
                      disabled={enviando}
                      className="font-label-sm text-label-sm text-primary underline disabled:opacity-60"
                    >
                      {UI_STRINGS.PROCESS_MODAL_STORIES_SELECT_ALL}
                    </button>
                    <button
                      type="button"
                      onClick={() => setChavesSelecionadas([])}
                      disabled={enviando}
                      className="font-label-sm text-label-sm text-primary underline disabled:opacity-60"
                    >
                      {UI_STRINGS.PROCESS_MODAL_STORIES_CLEAR_ALL}
                    </button>
                  </div>
                )}
              </div>

              {carregandoHistorias && (
                <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
                  {UI_STRINGS.PROCESS_MODAL_STORIES_LOADING}
                </p>
              )}
              {erroHistorias && <Alert type="error" message={erroHistorias} />}
              {!carregandoHistorias && !erroHistorias && historias.length === 0 && (
                <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
                  {UI_STRINGS.PROCESS_MODAL_STORIES_EMPTY}
                </p>
              )}
              {!carregandoHistorias && !erroHistorias && historias.length > 0 && (
                <>
                  <ul className="max-h-64 space-y-xs overflow-y-auto rounded-lg border border-outline-variant p-sm">
                    {historias.map((historia) => (
                      <li key={historia.chave}>
                        <label className="flex cursor-pointer items-start gap-sm rounded-md p-sm hover:bg-surface-container-low">
                          <input
                            type="checkbox"
                            checked={chavesSelecionadas.includes(historia.chave)}
                            onChange={() => alternarHistoria(historia.chave)}
                            disabled={enviando}
                            className="mt-1 accent-primary"
                          />
                          <span className="min-w-0 font-body-sm text-body-sm text-on-surface">
                            <span className="font-label-md text-label-md">{historia.chave}</span>
                            <span className="text-on-surface-variant"> · {historia.tipoIssue}</span>
                            <span className="block break-words">{historia.titulo}</span>
                            {historia.possuiReleaseNoteDedicada && (
                              <span className="mt-xs inline-block rounded-full bg-secondary-container px-sm py-[2px] font-label-sm text-label-sm text-on-secondary-container">
                                Release Note dedicada
                              </span>
                            )}
                          </span>
                        </label>
                      </li>
                    ))}
                  </ul>
                  <p className="font-body-sm text-body-sm text-on-surface-variant">
                    {chavesSelecionadas.length} de {historias.length} {UI_STRINGS.PROCESS_MODAL_STORIES_SELECTED}
                  </p>
                  {chavesSelecionadas.length === 0 && (
                    <p role="alert" className="font-body-sm text-body-sm text-error">
                      {UI_STRINGS.PROCESS_MODAL_STORIES_REQUIRED}
                    </p>
                  )}
                </>
              )}
              {valor && erroCampo && (
                <p role="alert" className="font-body-sm text-body-sm text-error">
                  {erroCampo}
                </p>
              )}
            </section>
          )}

          {erroVersoes && <Alert type="error" message={erroVersoes} />}
          {!carregandoVersoes && !erroVersoes && opcoesVersao.length === 0 && (
            <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
              {UI_STRINGS.PROCESS_MODAL_VERSIONS_EMPTY}
            </p>
          )}

          {enviando && (
            <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">
              {UI_STRINGS.PROCESS_MODAL_WAIT}
            </p>
          )}

          <Alert type="error" message={erroApi} />

          <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
            <button
              type="button"
              onClick={onClose}
              disabled={enviando}
              className="rounded-lg border border-primary px-md py-sm text-center font-label-md text-label-md text-primary transition-colors hover:bg-primary/5 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {UI_STRINGS.PROCESS_MODAL_CANCEL}
            </button>
            <Button
              type="submit"
              isLoading={enviando}
              disabled={carregandoVersoes || carregandoHistorias || Boolean(erroVersoes) || Boolean(erroHistorias) || opcoesVersao.length === 0 || !valor || chavesSelecionadas.length === 0}
            >
              {enviando
                ? UI_STRINGS.PROCESS_MODAL_SUBMITTING
                : UI_STRINGS.PROCESS_MODAL_SUBMIT}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
