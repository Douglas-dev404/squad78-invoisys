import { useEffect, useRef, useState } from 'react';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { BOTAO_SECUNDARIO, Dialogo } from '../Dialogo/Dialogo';
import { formatarData } from '../../utils/releases';
import { FORMATOS_EXPORTACAO, nomeArquivoExportacao } from '../../utils/revisao';

/**
 * Prévia da exportação (#72, #89). Markdown é mostrado como texto puro; HTML vai num
 * `<iframe sandbox>` sem permissão nenhuma (sem script, sem formulário, origem isolada).
 * @param {Object} props
 * @param {string} props.chave chave da Release (nome do arquivo)
 * @param {string} props.publico
 * @param {{ conteudo: string, formato?: string, geradoPor?: string, geradoEm?: string }|null} props.exportacao
 * @param {boolean} props.exportando
 * @param {string|null} props.erro mensagem da API (ex.: 409 "aprove a versão antes de exportar")
 * @param {Function} props.onClose
 * @param {Function} [props.onFormatoChange] recebe "markdown" | "html" e exporta de novo; sem ele, não há seletor
 * @param {string} [props.textoCarregando] texto enquanto `exportando` (o histórico só busca, não gera)
 */
export function ExportacaoModal({
  chave,
  publico,
  exportacao,
  exportando,
  erro,
  onClose,
  onFormatoChange,
  textoCarregando,
}) {
  const [copiado, setCopiado] = useState(false);
  const [erroCopia, setErroCopia] = useState(null);
  const [formatoEscolhido, setFormatoEscolhido] = useState('markdown');
  const temporizador = useRef(null);

  useEffect(() => () => window.clearTimeout(temporizador.current), []);

  const conteudo = exportacao?.conteudo ?? '';
  const formato = exportacao?.formato ?? 'markdown';
  const infoFormato = FORMATOS_EXPORTACAO[formato] ?? FORMATOS_EXPORTACAO.markdown;
  const infoEscolhido = FORMATOS_EXPORTACAO[formatoEscolhido] ?? FORMATOS_EXPORTACAO.markdown;

  const escolherFormato = (novo) => {
    if (novo === formatoEscolhido || exportando) return;
    setFormatoEscolhido(novo);
    onFormatoChange(novo);
  };

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
    const url = URL.createObjectURL(new Blob([conteudo], { type: infoFormato.tipo }));
    const link = document.createElement('a');
    link.href = url;
    link.download = nomeArquivoExportacao(chave, publico, formato);
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  };

  return (
    <Dialogo titulo="Prévia da exportação" onClose={onClose} bloqueado={exportando} largura="max-w-3xl">
      <div className="space-y-md">
        {onFormatoChange && (
          <fieldset className="flex flex-wrap items-center gap-sm">
            <legend className="sr-only">Formato da exportação</legend>
            <span aria-hidden="true" className="font-label-md text-label-md text-on-surface-variant">Formato:</span>
            {Object.entries(FORMATOS_EXPORTACAO).map(([valor, { label }]) => (
              <label
                key={valor}
                className={`cursor-pointer rounded-full border px-sm py-xs font-label-md text-label-md transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary ${
                  formatoEscolhido === valor
                    ? 'border-primary bg-primary text-on-primary'
                    : 'border-outline-variant text-on-surface-variant hover:bg-surface-container'
                } ${exportando ? 'cursor-not-allowed opacity-60' : ''}`}
              >
                <input
                  type="radio"
                  name="formato-exportacao"
                  value={valor}
                  checked={formatoEscolhido === valor}
                  disabled={exportando}
                  onChange={() => escolherFormato(valor)}
                  className="sr-only"
                />
                {label}
              </label>
            ))}
          </fieldset>
        )}

        {exportando && (
          <p role="status" className="font-body-md text-body-md text-on-surface-variant">
            {textoCarregando ?? `Gerando o ${infoEscolhido.label}…`}
          </p>
        )}

        <Alert type="error" message={erro} />

        {exportacao && (
          <>
            {formato === 'html' ? (
              <iframe
                title="Prévia do HTML exportado"
                sandbox=""
                srcDoc={conteudo}
                className="h-[50vh] w-full rounded-lg border border-outline-variant bg-white"
              />
            ) : (
              <pre
                aria-label="Conteúdo em Markdown"
                tabIndex={0}
                className="max-h-[50vh] overflow-auto whitespace-pre-wrap break-words rounded-lg border border-outline-variant bg-surface-container p-md font-mono text-body-sm text-on-surface"
              >
                {conteudo}
              </pre>
            )}
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
              <Button onClick={baixar} icon="download">Baixar .{infoFormato.extensao}</Button>
            </>
          )}
        </div>
      </div>
    </Dialogo>
  );
}
