import { useEffect, useRef, useState } from 'react';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { InputField } from '../InputField/InputField';
import { processarRelease } from '../../services/releases.service';
import { UI_STRINGS } from '../../utils/constants';

/**
 * Modal "Processar Release": envia a chave ao POST /releases/{chave}/processar.
 * @param {Object} props
 * @param {Function} props.onClose - Fecha o modal
 * @param {Function} props.onSucesso - Recebe a chave processada (a página navega)
 */
export function ProcessarReleaseModal({ onClose, onSucesso }) {
  const [valor, setValor] = useState('');
  const [erroCampo, setErroCampo] = useState(undefined);
  const [erroApi, setErroApi] = useState(null);
  const [enviando, setEnviando] = useState(false);
  const enviandoRef = useRef(false);
  const dialogRef = useRef(null);

  useEffect(() => {
    dialogRef.current?.focus();
  }, []);

  useEffect(() => {
    if (enviando) return undefined;
    const aoTeclar = (event) => {
      if (event.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', aoTeclar);
    return () => window.removeEventListener('keydown', aoTeclar);
  }, [enviando, onClose]);

  const handleChange = (event) => {
    setValor(event.target.value);
    if (erroCampo) setErroCampo(undefined);
    if (erroApi) setErroApi(null);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (enviandoRef.current) return;

    const chave = valor.trim().toUpperCase();
    if (!chave) {
      setErroCampo(UI_STRINGS.PROCESS_MODAL_KEY_REQUIRED);
      return;
    }

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
        className="w-full max-w-md rounded-xl border border-surface-variant/50 bg-surface-container-lowest p-md shadow-[0px_4px_12px_rgba(27,67,50,0.08)] outline-none"
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
          <InputField
            id="chave-release"
            name="chave"
            label={UI_STRINGS.PROCESS_MODAL_KEY_LABEL}
            placeholder={UI_STRINGS.PROCESS_MODAL_KEY_PLACEHOLDER}
            value={valor}
            onChange={handleChange}
            error={erroCampo}
            required
            disabled={enviando}
          />

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
            <Button type="submit" isLoading={enviando}>
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