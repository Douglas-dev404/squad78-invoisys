import { useEffect, useRef, useState } from 'react';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { BOTAO_SECUNDARIO, Dialogo } from '../Dialogo/Dialogo';
import { formatarData } from '../../utils/releases';
import { nomeArquivoMarkdown } from '../../utils/revisao';

/**
 * Prévia da exportação em Markdown (#72). O conteúdo é mostrado como texto puro,
 * nunca renderizado como HTML (evita XSS).
 * @param {Object} props
 * @param {string} props.chave chave da Release (nome do arquivo)
 * @param {string} props.publico
 * @param {{ conteudo: string, geradoPor?: string, geradoEm?: string }|null} props.exportacao
 * @param {boolean} props.exportando
 * @param {string|null} props.erro mensagem da API (ex.: 409 "aprove a versão antes de exportar")
 * @param {Function} props.onClose
 * @param {string} [props.textoCarregando] mensagem enquanto `exportando`
 */
export function ExportacaoModal({ chave, publico, exportacao, exportando, erro, onClose, textoCarregando = 'Gerando o Markdown…' }) {
  const [copiado, setCopiado] = useState(false);
  const [erroCopia, setErroCopia] = useState(null);
  const temporizador = useRef(null);

  useEffect(() => () => window.clearTimeout(temporizador.current), []);

  const conteudo = exportacao?.conteudo ?? '';

  const copiar = async () => {
    setErroCopia(null);
    try {
      await navigator.clipboard.writeText(conteudo);
      setCopiado(true);
      window.clearTimeout(temporizador.current);
      temporizador.current = window.setTimeout(() => setCopiado(false), 2000);
    } catch {
      setErroCopia('Não foi possível copiar. Selecione o texto e copie manualmente.');
    }
  };

  const baixar = () => {
    const url = URL.createObjectURL(new Blob([conteudo], { type: 'text/markdown;charset=utf-8' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = nomeArquivoMarkdown(chave, publico);
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  };

  return (
    <Dialogo titulo="Prévia da exportação" onClose={onClose} bloqueado={exportando} largura="max-w-3xl">
      <div className="space-y-md">
        {exportando && (
          <p role="status" className="font-body-md text-body-md text-on-surface-variant">
            {textoCarregando}
          </p>
        )}

        <Alert type="error" message={erro} />

        {exportacao && (
          <>
            <pre
              aria-label="Conteúdo em Markdown"
              tabIndex={0}
              className="max-h-[50vh] overflow-auto whitespace-pre-wrap break-words rounded-lg border border-outline-variant bg-surface-container p-md font-mono text-body-sm text-on-surface"
            >
              {conteudo}
            </pre>
            <Alert type="error" message={erroCopia} onClose={() => setErroCopia(null)} />
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              Gerado por {exportacao.geradoPor ?? '—'} em {exportacao.geradoEm ? formatarData(exportacao.geradoEm) : '—'}
            </p>
          </>
        )}

        <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
          <button type="button" onClick={onClose} disabled={exportando} className={BOTAO_SECUNDARIO}>
            Fechar
          </button>
          {exportacao && (
            <>
              <button type="button" onClick={copiar} className={BOTAO_SECUNDARIO} aria-live="polite">
                {copiado ? 'Copiado!' : 'Copiar'}
              </button>
              <Button onClick={baixar} icon="download">Baixar .md</Button>
            </>
          )}
        </div>
      </div>
    </Dialogo>
  );
}
