import { useState } from 'react';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { BOTAO_PERIGO, BOTAO_SECUNDARIO, BOTAO_TEXTO, Dialogo } from '../Dialogo/Dialogo';
import { REVISAO_TEXTOS, textoFinalDoItem, textoItemValido } from '../../utils/revisao';

/**
 * Ações de revisão de um item do comunicado (#70): editar, excluir e reincluir.
 * A tela de detalhe (#69) desenha o card e passa o conteúdo dele em `children`;
 * durante a edição, o `children` dá lugar ao editor.
 *
 * @example
 * const revisao = useRevisaoItem({ chave, onItemAtualizado: (i) => setItens((l) => substituirItem(l, i)) });
 * const acoes = acoesDaVersao(versao, release.status);
 * <ItemRevisavel item={item} podeEditar={acoes.editarItens} revisao={revisao}>
 *   <p>{item.textoFinal}</p>
 * </ItemRevisavel>
 *
 * @param {Object} props
 * @param {{ id: string, textoFinal: string, incluido: boolean }} props.item
 * @param {boolean} props.podeEditar `acoesDaVersao(...).editarItens`; falso esconde os três botões
 * @param {ReturnType<import('../../hooks/useRevisaoItem').useRevisaoItem>} props.revisao
 * @param {React.ReactNode} [props.children]
 */
export function ItemRevisavel({ item, podeEditar, revisao, children }) {
  const [editando, setEditando] = useState(false);
  const [rascunho, setRascunho] = useState('');
  const [erroLocal, setErroLocal] = useState(null);
  const [excluindo, setExcluindo] = useState(false);
  const [ultimaAcao, setUltimaAcao] = useState(null);

  const { salvando, erro, erroTexto } = revisao.estadoDo(item.id);

  const abrirEditor = () => {
    setRascunho(textoFinalDoItem(item));
    setErroLocal(null);
    revisao.limparErro(item.id);
    setEditando(true);
  };

  const cancelar = () => {
    setEditando(false);
    setErroLocal(null);
    revisao.limparErro(item.id);
  };

  const salvar = async (texto = rascunho) => {
    if (!textoItemValido(texto)) {
      setErroLocal(REVISAO_TEXTOS.TEXTO_VAZIO);
      return;
    }
    setUltimaAcao({ tipo: 'editar', texto });
    if (await revisao.editar(item.id, texto)) setEditando(false);
  };

  const excluir = async (motivo) => {
    setUltimaAcao({ tipo: 'excluir', motivo });
    if (await revisao.excluir(item.id, motivo)) setExcluindo(false);
  };

  const reincluir = () => {
    setUltimaAcao({ tipo: 'reincluir' });
    revisao.reincluir(item.id);
  };

  const tentarDeNovo = () => {
    if (ultimaAcao?.tipo === 'editar') salvar(ultimaAcao.texto);
    else if (ultimaAcao?.tipo === 'excluir') excluir(ultimaAcao.motivo);
    else if (ultimaAcao?.tipo === 'reincluir') reincluir();
  };

  const aoTeclarNoEditor = (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      cancelar();
    } else if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      salvar();
    }
  };

  const idEditor = `editor-item-${item.id}`;
  const erroDoEditor = erroLocal || erroTexto;

  return (
    <div className="space-y-sm" aria-busy={salvando}>
      {editando ? (
        <div className="space-y-xs" onKeyDown={aoTeclarNoEditor}>
          <label htmlFor={idEditor} className="sr-only">Texto do item</label>
          <textarea
            id={idEditor}
            value={rascunho}
            onChange={(event) => {
              setRascunho(event.target.value);
              if (erroLocal) setErroLocal(null);
            }}
            disabled={salvando}
            rows={4}
            autoFocus
            aria-invalid={Boolean(erroDoEditor)}
            aria-describedby={`${idEditor}-ajuda`}
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest p-sm font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/30 disabled:opacity-70"
          />
          <div id={`${idEditor}-ajuda`} className="flex items-center justify-between gap-sm text-body-sm font-body-sm">
            <span className={erroDoEditor ? 'text-error' : 'text-on-surface-variant'} role={erroDoEditor ? 'alert' : undefined}>
              {erroDoEditor ?? 'Ctrl+Enter salva · Esc cancela'}
            </span>
            <span className="text-on-surface-variant tabular-nums">{rascunho.length} caracteres</span>
          </div>
          <div className="flex justify-end gap-sm">
            <button type="button" onClick={cancelar} disabled={salvando} className={BOTAO_SECUNDARIO}>
              Cancelar
            </button>
            <Button onClick={() => salvar()} isLoading={salvando}>Salvar</Button>
          </div>
        </div>
      ) : (
        children
      )}

      {podeEditar && !editando && (
        <div className="flex flex-wrap justify-end gap-xs">
          {item.incluido ? (
            <>
              <button type="button" onClick={abrirEditor} disabled={salvando} className={BOTAO_TEXTO}>
                Editar
              </button>
              <button
                type="button"
                onClick={() => setExcluindo(true)}
                disabled={salvando}
                className={`${BOTAO_TEXTO} text-error hover:bg-error/5`}
              >
                Excluir
              </button>
            </>
          ) : (
            <button type="button" onClick={reincluir} disabled={salvando} className={BOTAO_TEXTO}>
              {salvando ? 'Reincluindo…' : 'Reincluir'}
            </button>
          )}
        </div>
      )}

      {erro && !excluindo && (
        <div role="alert" className="flex items-center justify-between gap-sm rounded-lg border border-error/20 bg-error-container p-sm text-body-sm font-body-sm text-on-error-container">
          <span>{erro}</span>
          {ultimaAcao && (
            <button type="button" onClick={tentarDeNovo} disabled={salvando} className="shrink-0 font-label-md underline">
              Tentar de novo
            </button>
          )}
        </div>
      )}

      {excluindo && (
        <ExcluirItemDialogo
          salvando={salvando}
          erro={erro}
          onCancelar={() => {
            setExcluindo(false);
            revisao.limparErro(item.id);
          }}
          onConfirmar={excluir}
        />
      )}
    </div>
  );
}

function ExcluirItemDialogo({ salvando, erro, onCancelar, onConfirmar }) {
  const [motivo, setMotivo] = useState('');

  return (
    <Dialogo titulo="Excluir item" onClose={onCancelar} bloqueado={salvando}>
      <form
        noValidate
        className="space-y-md"
        onSubmit={(event) => {
          event.preventDefault();
          onConfirmar(motivo);
        }}
      >
        <p className="font-body-md text-body-md text-on-surface-variant">
          O item sai do comunicado, mas continua na lista (riscado) e pode ser reincluído.
        </p>
        <div className="space-y-xs">
          <label htmlFor="motivo-exclusao" className="font-label-md text-label-md text-on-surface">
            Motivo (opcional)
          </label>
          <input
            id="motivo-exclusao"
            value={motivo}
            onChange={(event) => setMotivo(event.target.value)}
            disabled={salvando}
            maxLength={500}
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest p-sm font-body-md text-body-md focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/30"
          />
        </div>
        <Alert type="error" message={erro} />
        <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
          <button type="button" onClick={onCancelar} disabled={salvando} className={BOTAO_SECUNDARIO}>
            Cancelar
          </button>
          <button type="submit" disabled={salvando} className={BOTAO_PERIGO}>
            {salvando ? 'Excluindo…' : 'Excluir item'}
          </button>
        </div>
      </form>
    </Dialogo>
  );
}
