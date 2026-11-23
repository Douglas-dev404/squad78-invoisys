import { useState } from 'react';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { BOTAO_PERIGO, BOTAO_SECUNDARIO, Dialogo } from '../Dialogo/Dialogo';
import { ExportacaoModal } from '../ExportacaoModal/ExportacaoModal';
import { useExportacao } from '../../hooks/useExportacao';
import { useRevisaoVersao } from '../../hooks/useRevisaoVersao';
import { formatarData } from '../../utils/releases';
import { acoesDaVersao, REVISAO_TEXTOS } from '../../utils/revisao';

const DIALOGOS = {
  aprovar: { titulo: 'Aprovar versão', texto: REVISAO_TEXTOS.CONFIRMAR_APROVAR, botao: 'Aprovar' },
  reabrir: { titulo: 'Reabrir revisão', texto: REVISAO_TEXTOS.CONFIRMAR_REABRIR, botao: 'Reabrir' },
  reprocessar: {
    titulo: 'Reprocessar com IA',
    texto: REVISAO_TEXTOS.CONFIRMAR_REPROCESSAR,
    botao: 'Reprocessar',
    espera: 'Isso pode levar até 2 minutos. Não feche esta janela.',
  },
};

/**
 * Barra de ações da revisão de uma versão (#71) + exportação (#72), para o topo da aba do público
 * na tela de detalhe (#69). Só mostra o que o status permite:
 * aguardando_revisao → Aprovar, Reprovar, Reprocessar · aprovado → Reabrir, Exportar Markdown ·
 * reprovado → Reprocessar.
 *
 * @example
 * const { release, recarregar } = useRelease(chave);
 * <BarraRevisao chave={chave} versao={versaoDaAba} statusRelease={release.status} recarregar={recarregar} />
 *
 * @param {Object} props
 * @param {string} props.chave
 * @param {{ publico: string, status: string, itens: Array, revisadoPor?: string, revisadoEm?: string,
 *   motivoReprovacao?: string }} props.versao
 * @param {string} [props.statusRelease]
 * @param {Function} props.recarregar refaz o GET da Release depois de cada ação
 */
export function BarraRevisao({ chave, versao, chavesHistorias, statusRelease, recarregar }) {
  const [dialogo, setDialogo] = useState(null);
  const [exportacaoAberta, setExportacaoAberta] = useState(false);
  const revisao = useRevisaoVersao({ chave, publico: versao.publico, chavesHistorias, recarregar });
  const exportacao = useExportacao({ chave, publico: versao.publico });
  const acoes = acoesDaVersao(versao, statusRelease);
  const ocupado = revisao.executando !== null;

  const abrir = (nome) => {
    revisao.limparErro();
    setDialogo(nome);
  };

  const confirmar = async (nome, motivo) => {
    const ok =
      nome === 'reprovar' ? await revisao.reprovar(motivo) : await revisao[nome]();
    if (ok) setDialogo(null);
  };

  const exportar = () => {
    exportacao.limpar();
    setExportacaoAberta(true);
    exportacao.exportar();
  };

  return (
    <section aria-label="Ações da revisão" className="space-y-sm rounded-xl border border-outline-variant/40 bg-surface-container-low p-sm">
      <div className="flex flex-wrap items-center justify-end gap-sm">
        {acoes.reprocessar && (
          <button type="button" onClick={() => abrir('reprocessar')} disabled={ocupado} className={BOTAO_SECUNDARIO}>
            Reprocessar com IA
          </button>
        )}
        {acoes.reprovar && (
          <button type="button" onClick={() => abrir('reprovar')} disabled={ocupado} className={BOTAO_SECUNDARIO}>
            Reprovar
          </button>
        )}
        {acoes.reabrir && (
          <button type="button" onClick={() => abrir('reabrir')} disabled={ocupado} className={BOTAO_SECUNDARIO}>
            Reabrir
          </button>
        )}
        {acoes.exportar && (
          <Button onClick={exportar} disabled={ocupado} icon="download">Exportar Markdown</Button>
        )}
        {acoes.aprovar && (
          <span title={acoes.aprovarDesabilitadoMotivo ?? undefined}>
            <Button onClick={() => abrir('aprovar')} disabled={ocupado || Boolean(acoes.aprovarDesabilitadoMotivo)}>
              Aprovar
            </Button>
          </span>
        )}
      </div>

      {acoes.aprovarDesabilitadoMotivo && (
        <p className="text-right font-body-sm text-body-sm text-on-surface-variant">{acoes.aprovarDesabilitadoMotivo}</p>
      )}
      {versao.status === 'aprovado' && versao.revisadoPor && (
        <p className="font-body-sm text-body-sm text-on-surface-variant">
          Aprovado por <strong className="text-on-surface">{versao.revisadoPor}</strong>
          {versao.revisadoEm ? ` em ${formatarData(versao.revisadoEm)}` : ''}
        </p>
      )}
      {versao.status === 'reprovado' && versao.motivoReprovacao && (
        <div className="rounded-lg border border-error/20 bg-error-container p-sm text-body-sm font-body-sm text-on-error-container">
          <strong>Reprovada{versao.revisadoPor ? ` por ${versao.revisadoPor}` : ''}:</strong> {versao.motivoReprovacao}
        </div>
      )}
      {acoes.edicaoBloqueadaMotivo && (
        <p className="font-body-sm text-body-sm text-on-surface-variant">{acoes.edicaoBloqueadaMotivo}</p>
      )}

      {dialogo && dialogo !== 'reprovar' && (
        <Dialogo titulo={DIALOGOS[dialogo].titulo} onClose={() => setDialogo(null)} bloqueado={ocupado}>
          <div className="space-y-md">
            <p className="font-body-md text-body-md text-on-surface-variant">{DIALOGOS[dialogo].texto}</p>
            {ocupado && DIALOGOS[dialogo].espera && (
              <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">{DIALOGOS[dialogo].espera}</p>
            )}
            <Alert type="error" message={revisao.erro} />
            <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
              <button type="button" onClick={() => setDialogo(null)} disabled={ocupado} className={BOTAO_SECUNDARIO}>
                Cancelar
              </button>
              <Button onClick={() => confirmar(dialogo)} isLoading={ocupado}>{DIALOGOS[dialogo].botao}</Button>
            </div>
          </div>
        </Dialogo>
      )}

      {dialogo === 'reprovar' && (
        <ReprovarDialogo
          ocupado={ocupado}
          erro={revisao.erroMotivo ? null : revisao.erro}
          erroMotivo={revisao.erroMotivo}
          onCancelar={() => setDialogo(null)}
          onConfirmar={(motivo) => confirmar('reprovar', motivo)}
        />
      )}

      {exportacaoAberta && (
        <ExportacaoModal
          chave={chave}
          publico={versao.publico}
          exportacao={exportacao.resultado}
          exportando={exportacao.exportando}
          erro={exportacao.erro}
          onClose={() => setExportacaoAberta(false)}
        />
      )}
    </section>
  );
}

function ReprovarDialogo({ ocupado, erro, erroMotivo, onCancelar, onConfirmar }) {
  const [motivo, setMotivo] = useState('');
  const [erroLocal, setErroLocal] = useState(null);
  const erroDoCampo = erroLocal || erroMotivo;

  return (
    <Dialogo titulo="Reprovar versão" onClose={onCancelar} bloqueado={ocupado}>
      <form
        noValidate
        className="space-y-md"
        onSubmit={(event) => {
          event.preventDefault();
          if (!motivo.trim()) {
            setErroLocal(REVISAO_TEXTOS.MOTIVO_OBRIGATORIO);
            return;
          }
          onConfirmar(motivo);
        }}
      >
        <div className="space-y-xs">
          <label htmlFor="motivo-reprovacao" className="font-label-md text-label-md text-on-surface">
            Motivo da reprovação*
          </label>
          <textarea
            id="motivo-reprovacao"
            value={motivo}
            onChange={(event) => {
              setMotivo(event.target.value);
              if (erroLocal) setErroLocal(null);
            }}
            disabled={ocupado}
            rows={4}
            required
            aria-invalid={Boolean(erroDoCampo)}
            aria-describedby={erroDoCampo ? 'motivo-reprovacao-erro' : undefined}
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest p-sm font-body-md text-body-md focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/30"
          />
          {erroDoCampo && (
            <p id="motivo-reprovacao-erro" role="alert" className="font-body-sm text-body-sm text-error">{erroDoCampo}</p>
          )}
        </div>
        <Alert type="error" message={erro} />
        <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
          <button type="button" onClick={onCancelar} disabled={ocupado} className={BOTAO_SECUNDARIO}>
            Cancelar
          </button>
          <button type="submit" disabled={ocupado} className={BOTAO_PERIGO}>
            {ocupado ? 'Reprovando…' : 'Reprovar'}
          </button>
        </div>
      </form>
    </Dialogo>
  );
}
