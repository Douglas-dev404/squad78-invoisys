import { useState } from 'react';
import { Alert } from '../../components/Alert/Alert';
import { Button } from '../../components/Button/Button';
import { BOTAO_PERIGO, BOTAO_SECUNDARIO, BOTAO_TEXTO, Dialogo } from '../../components/Dialogo/Dialogo';
import { DestinatarioModal } from '../../components/DestinatarioModal/DestinatarioModal';
import { Layout } from '../../components/Layout/Layout';
import { useDestinatarios } from '../../hooks/useDestinatarios';
import { DESTINATARIOS_TEXTOS } from '../../utils/destinatarios';
import { mensagemDeErro, PUBLICO_LABEL } from '../../utils/revisao';

const CARD_CLASS =
  'rounded-xl border border-surface-variant/50 bg-surface-container-lowest shadow-[0px_4px_12px_rgba(27,67,50,0.08)]';

function Filtros({ publico, inativos, onPublico, onInativos }) {
  return (
    <div className="mb-md flex flex-col gap-sm sm:flex-row sm:items-center sm:justify-between">
      <div role="group" aria-label="Filtrar por público" className="flex flex-wrap gap-xs">
        {[['', 'Todos'], ...Object.entries(PUBLICO_LABEL)].map(([valor, label]) => (
          <button
            key={valor || 'todos'}
            type="button"
            aria-pressed={publico === valor}
            onClick={() => onPublico(valor)}
            className={`rounded-full border px-sm py-xs font-label-md text-label-md transition-colors ${
              publico === valor
                ? 'border-primary bg-primary text-on-primary'
                : 'border-outline-variant text-on-surface-variant hover:bg-surface-container'
            }`}
          >
            {label}
          </button>
        ))}
      </div>
      <label className="flex cursor-pointer items-center gap-xs font-body-sm text-body-sm text-on-surface-variant">
        <input
          type="checkbox"
          checked={inativos}
          onChange={(event) => onInativos(event.target.checked)}
          className="h-4 w-4 accent-primary"
        />
        Mostrar inativos
      </label>
    </div>
  );
}

function Tabela({ itens, onEditar, onSituacao }) {
  return (
    <div className={`${CARD_CLASS} overflow-x-auto`}>
      <table className="w-full text-left">
        <caption className="sr-only">Destinatários de e-mail</caption>
        <thead className="border-b border-surface-variant bg-surface-container-low">
          <tr className="font-label-md text-label-md text-on-surface-variant">
            <th scope="col" className="px-md py-sm">Nome</th>
            <th scope="col" className="px-md py-sm">E-mail</th>
            <th scope="col" className="px-md py-sm">Público</th>
            <th scope="col" className="px-md py-sm">Situação</th>
            <th scope="col" className="px-md py-sm text-right">Ações</th>
          </tr>
        </thead>
        <tbody>
          {itens.map((d) => (
            <tr key={d.id} className="border-b border-surface-variant/50 last:border-0">
              <td className="px-md py-sm font-body-md text-body-md text-on-surface">{d.nome}</td>
              <td className="px-md py-sm font-body-sm text-body-sm text-on-surface-variant break-all">{d.email}</td>
              <td className="px-md py-sm">
                <span className="rounded-full bg-secondary-container px-sm py-0.5 font-label-sm text-label-sm text-on-secondary-container">
                  {PUBLICO_LABEL[d.publico] ?? d.publico}
                </span>
              </td>
              <td className="px-md py-sm font-body-sm text-body-sm">
                {d.ativo ? (
                  <span className="text-green-700">Ativo</span>
                ) : (
                  <span className="text-on-surface-variant">Inativo</span>
                )}
              </td>
              <td className="px-md py-sm">
                <div className="flex justify-end gap-xs">
                  <button type="button" onClick={() => onEditar(d)} className={BOTAO_TEXTO} aria-label={`Editar ${d.nome}`}>
                    Editar
                  </button>
                  <button
                    type="button"
                    onClick={() => onSituacao(d)}
                    className={BOTAO_TEXTO}
                    aria-label={`${d.ativo ? 'Desativar' : 'Reativar'} ${d.nome}`}
                  >
                    {d.ativo ? 'Desativar' : 'Reativar'}
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function DestinatariosPage() {
  const lista = useDestinatarios();
  const [edicao, setEdicao] = useState(null);
  const [desativando, setDesativando] = useState(null);
  const [ocupado, setOcupado] = useState(false);
  const [erroAcao, setErroAcao] = useState(null);

  const mudarSituacao = async (destinatario) => {
    setErroAcao(null);
    setOcupado(true);
    try {
      await lista.mudarSituacao(destinatario);
      setDesativando(null);
    } catch (err) {
      setErroAcao(mensagemDeErro(err, 'Não foi possível alterar a situação.'));
    } finally {
      setOcupado(false);
    }
  };

  const pedirSituacao = (destinatario) => {
    if (destinatario.ativo) {
      setErroAcao(null);
      setDesativando(destinatario);
    } else {
      mudarSituacao(destinatario);
    }
  };

  let conteudo;
  if (lista.carregando) {
    conteudo = (
      <section role="status" aria-live="polite" className={`${CARD_CLASS} space-y-sm p-md`}>
        {[...Array(4)].map((_, i) => (
          <div key={i} className="h-10 animate-pulse rounded-lg bg-surface-container" />
        ))}
        <span className="sr-only">Carregando destinatários…</span>
      </section>
    );
  } else if (lista.erro) {
    conteudo = (
      <div
        role="alert"
        className="flex flex-col items-start gap-sm rounded-xl border border-error bg-error-container p-md sm:flex-row sm:items-center sm:justify-between"
      >
        <p className="font-body-md text-body-md text-on-error-container">{lista.erro}</p>
        <Button onClick={lista.recarregar}>Tentar de novo</Button>
      </div>
    );
  } else if (lista.itens.length === 0) {
    conteudo = (
      <section role="status" className={`${CARD_CLASS} p-xl text-center`}>
        <p className="font-body-md text-body-md text-on-surface-variant">
          {lista.temFiltro ? DESTINATARIOS_TEXTOS.VAZIO_FILTRO : DESTINATARIOS_TEXTOS.VAZIO}
        </p>
        <div className="mt-md flex justify-center">
          {lista.temFiltro ? (
            <button type="button" onClick={lista.limparFiltros} className={BOTAO_SECUNDARIO}>
              {DESTINATARIOS_TEXTOS.LIMPAR}
            </button>
          ) : (
            <Button onClick={() => setEdicao({ destinatario: null })} icon="add">{DESTINATARIOS_TEXTOS.NOVO}</Button>
          )}
        </div>
      </section>
    );
  } else {
    conteudo = (
      <Tabela
        itens={lista.itens}
        onEditar={(destinatario) => setEdicao({ destinatario })}
        onSituacao={pedirSituacao}
      />
    );
  }

  return (
    <Layout>
      <div className="mx-auto w-full max-w-[1280px]">
        <header className="mb-lg flex flex-col gap-sm sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="mb-xs font-headline-lg text-headline-lg text-on-surface">{DESTINATARIOS_TEXTOS.TITULO}</h2>
            <p className="max-w-2xl text-body-sm text-on-surface-variant">{DESTINATARIOS_TEXTOS.SUBTITULO}</p>
          </div>
          <Button onClick={() => setEdicao({ destinatario: null })} icon="add">{DESTINATARIOS_TEXTOS.NOVO}</Button>
        </header>

        <Filtros
          publico={lista.publico}
          inativos={lista.inativos}
          onPublico={lista.setPublico}
          onInativos={lista.setInativos}
        />
        {!desativando && <div className="mb-md"><Alert type="error" message={erroAcao} onClose={() => setErroAcao(null)} /></div>}
        {conteudo}
      </div>

      {edicao && (
        <DestinatarioModal
          destinatario={edicao.destinatario}
          publicoInicial={lista.publico}
          onSalvar={lista.salvar}
          onClose={() => setEdicao(null)}
        />
      )}

      {desativando && (
        <Dialogo titulo="Desativar destinatário" onClose={() => setDesativando(null)} bloqueado={ocupado}>
          <div className="space-y-md">
            <p className="font-body-md text-body-md text-on-surface-variant">
              {DESTINATARIOS_TEXTOS.CONFIRMAR_DESATIVAR(desativando.nome)}
            </p>
            <Alert type="error" message={erroAcao} />
            <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
              <button type="button" onClick={() => setDesativando(null)} disabled={ocupado} className={BOTAO_SECUNDARIO}>
                Cancelar
              </button>
              <button type="button" onClick={() => mudarSituacao(desativando)} disabled={ocupado} className={BOTAO_PERIGO}>
                Desativar
              </button>
            </div>
          </div>
        </Dialogo>
      )}
    </Layout>
  );
}
