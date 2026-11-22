import { useEffect, useId, useRef } from 'react';

export const BOTAO_SECUNDARIO =
  'rounded-lg border border-primary px-md py-sm text-center font-label-md text-label-md text-primary transition-colors hover:bg-primary/5 disabled:cursor-not-allowed disabled:opacity-60';

export const BOTAO_PERIGO =
  'rounded-lg bg-error px-md py-sm text-center font-label-md text-label-md text-on-error transition-colors hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60';

export const BOTAO_TEXTO =
  'rounded-md px-sm py-xs font-label-md text-label-md text-primary transition-colors hover:bg-primary/5 disabled:cursor-not-allowed disabled:opacity-50';

const FOCAVEIS =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Caixa de diálogo modal (mesmo visual do "Processar Release").
 * Esc e clique fora fecham, exceto enquanto `bloqueado` (chamada em andamento).
 * @param {Object} props
 * @param {string} props.titulo
 * @param {Function} props.onClose
 * @param {boolean} [props.bloqueado=false]
 * @param {string} [props.largura='max-w-md'] classe Tailwind de largura máxima
 * @param {React.ReactNode} props.children
 */
export function Dialogo({ titulo, onClose, bloqueado = false, largura = 'max-w-md', children }) {
  const idTitulo = useId();
  const caixaRef = useRef(null);

  useEffect(() => {
    const anterior = document.activeElement;
    caixaRef.current?.focus();
    return () => anterior?.focus?.();
  }, []);

  const prenderFoco = (event) => {
    if (event.key !== 'Tab') return;
    const caixa = caixaRef.current;
    const itens = [...caixa.querySelectorAll(FOCAVEIS)];
    if (itens.length === 0) {
      event.preventDefault();
      caixa.focus();
      return;
    }
    const primeiro = itens[0];
    const ultimo = itens[itens.length - 1];
    const ativo = document.activeElement;
    if (event.shiftKey && (ativo === primeiro || ativo === caixa)) {
      event.preventDefault();
      ultimo.focus();
    } else if (!event.shiftKey && ativo === ultimo) {
      event.preventDefault();
      primeiro.focus();
    }
  };

  useEffect(() => {
    if (bloqueado) return undefined;
    const aoTeclar = (event) => {
      if (event.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', aoTeclar);
    return () => window.removeEventListener('keydown', aoTeclar);
  }, [bloqueado, onClose]);

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-black/40 p-gutter"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !bloqueado) onClose();
      }}
    >
      <div
        ref={caixaRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby={idTitulo}
        onKeyDown={prenderFoco}
        className={`w-full ${largura} max-h-[90vh] overflow-y-auto rounded-xl border border-surface-variant/50 bg-surface-container-lowest p-md shadow-[0px_4px_12px_rgba(27,67,50,0.08)] outline-none`}
      >
        <div className="mb-md flex items-start justify-between gap-sm">
          <h3 id={idTitulo} className="font-headline-sm text-headline-sm text-primary">
            {titulo}
          </h3>
          <button
            type="button"
            onClick={onClose}
            disabled={bloqueado}
            aria-label="Fechar"
            className="text-on-surface-variant transition-colors hover:text-on-surface disabled:cursor-not-allowed disabled:opacity-40"
          >
            <span aria-hidden="true" className="material-symbols-outlined">close</span>
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
