import { useEffect, useId, useState } from 'react';
import { Link } from 'react-router-dom';
import { Alert } from '../Alert/Alert';
import { Button } from '../Button/Button';
import { BOTAO_SECUNDARIO, Dialogo } from '../Dialogo/Dialogo';
import { listarDestinatarios } from '../../services/destinatarios.service';
import { enviarRelease } from '../../services/exportacoes.service';
import { resumirEnvio, STATUS_ENVIO } from '../../utils/destinatarios';
import { mensagemDeErro, PUBLICO_LABEL } from '../../utils/revisao';

/**
 * Envio por e-mail da versão aprovada (#148): escolher entre os destinatários ativos do público,
 * confirmar e ver o resultado por destinatário, com "tentar de novo" só para os que falharam.
 * @param {Object} props
 * @param {string} props.chave
 * @param {{ publico: string, tituloExecutivo?: string }} props.versao
 * @param {Function} props.onClose
 */
export function EnvioEmailModal({ chave, versao, onClose }) {
  const publico = versao.publico;
  const nomePublico = PUBLICO_LABEL[publico] ?? publico;
  const idLista = useId();

  const [lista, setLista] = useState({ carregando: true, itens: [], erro: null });
  const [tentativaLista, setTentativaLista] = useState(0);
  const [selecionados, setSelecionados] = useState(() => new Set());
  const [etapa, setEtapa] = useState('escolher');
  const [erro, setErro] = useState(null);
  const [resultado, setResultado] = useState(null);

  useEffect(() => {
    const controller = new AbortController();
    listarDestinatarios({ publico }, { signal: controller.signal })
      .then((itens) => {
        if (!controller.signal.aborted) setLista({ carregando: false, itens, erro: null });
      })
      .catch((err) => {
        if (!controller.signal.aborted) {
          setLista({ carregando: false, itens: [], erro: mensagemDeErro(err, 'Não foi possível carregar os destinatários.') });
        }
      });
    return () => controller.abort();
  }, [publico, tentativaLista]);

  const enviando = etapa === 'enviando';
  const todosMarcados = lista.itens.length > 0 && selecionados.size === lista.itens.length;
  const quantidade = selecionados.size;
  const rotuloQuantidade = `${quantidade} destinatário${quantidade === 1 ? '' : 's'}`;

  const alternar = (id) =>
    setSelecionados((atual) => {
      const proximo = new Set(atual);
      if (proximo.has(id)) proximo.delete(id);
      else proximo.add(id);
      return proximo;
    });

  const alternarTodos = () =>
    setSelecionados(todosMarcados ? new Set() : new Set(lista.itens.map((d) => d.id)));

  const enviar = async (ids) => {
    setErro(null);
    setEtapa('enviando');
    try {
      const resposta = await enviarRelease(chave, { publico, destinatarioIds: ids });
      setResultado((anterior) => {
        if (!anterior) return resposta;
        const novos = new Map(resposta.envios.map((envio) => [envio.destinatarioId, envio]));
        return { ...resposta, envios: anterior.envios.map((envio) => novos.get(envio.destinatarioId) ?? envio) };
      });
      setEtapa('resultado');
    } catch (err) {
      setErro(mensagemDeErro(err, 'Não foi possível enviar o comunicado.'));
      setEtapa(resultado ? 'resultado' : 'confirmar');
    }
  };

  const resumo = resultado ? resumirEnvio(resultado.envios) : null;

  return (
    <Dialogo titulo="Enviar por e-mail" onClose={onClose} bloqueado={enviando} largura="max-w-xl">
      <div className="space-y-md">
        <p className="font-body-sm text-body-sm text-on-surface-variant">
          Versão <strong className="text-on-surface">{nomePublico}</strong> da Release{' '}
          <strong className="text-on-surface">{chave}</strong>. Assunto do e-mail:{' '}
          <strong className="text-on-surface">{versao.tituloExecutivo || `Release ${chave}`}</strong>
        </p>

        <Alert type="error" message={erro} onClose={enviando ? undefined : () => setErro(null)} />

        {etapa === 'escolher' && (
          <>
            {lista.carregando && (
              <p role="status" className="font-body-md text-body-md text-on-surface-variant">Carregando destinatários…</p>
            )}
            {lista.erro && (
              <div role="alert" className="flex flex-wrap items-center justify-between gap-sm rounded-lg bg-error-container p-sm">
                <p className="font-body-sm text-body-sm text-on-error-container">{lista.erro}</p>
                <button
                  type="button"
                  className={BOTAO_SECUNDARIO}
                  onClick={() => {
                    setLista({ carregando: true, itens: [], erro: null });
                    setTentativaLista((n) => n + 1);
                  }}
                >
                  Tentar de novo
                </button>
              </div>
            )}
            {!lista.carregando && !lista.erro && lista.itens.length === 0 && (
              <div role="status" className="rounded-lg border border-outline-variant p-md text-center">
                <p className="font-body-md text-body-md text-on-surface-variant">
                  Nenhum destinatário cadastrado para {nomePublico}.
                </p>
                <Link
                  to={`/destinatarios?publico=${encodeURIComponent(publico)}`}
                  className="mt-sm inline-block font-label-md text-label-md text-primary underline"
                >
                  Cadastrar destinatário
                </Link>
              </div>
            )}
            {lista.itens.length > 0 && (
              <fieldset className="space-y-xs">
                <legend className="sr-only">Destinatários do público {nomePublico}</legend>
                <label className="flex cursor-pointer items-center gap-sm border-b border-outline-variant/50 pb-xs font-label-md text-label-md text-on-surface">
                  <input
                    type="checkbox"
                    checked={todosMarcados}
                    onChange={alternarTodos}
                    aria-controls={idLista}
                    className="h-4 w-4 accent-primary"
                  />
                  Selecionar todos ({lista.itens.length})
                </label>
                <ul id={idLista} className="max-h-[40vh] space-y-xs overflow-y-auto">
                  {lista.itens.map((d) => (
                    <li key={d.id}>
                      <label className="flex cursor-pointer items-start gap-sm rounded-md p-xs hover:bg-surface-container">
                        <input
                          type="checkbox"
                          checked={selecionados.has(d.id)}
                          onChange={() => alternar(d.id)}
                          className="mt-1 h-4 w-4 accent-primary"
                        />
                        <span>
                          <span className="block font-body-md text-body-md text-on-surface">{d.nome}</span>
                          <span className="block font-body-sm text-body-sm text-on-surface-variant break-all">{d.email}</span>
                        </span>
                      </label>
                    </li>
                  ))}
                </ul>
              </fieldset>
            )}
            <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
              <button type="button" onClick={onClose} className={BOTAO_SECUNDARIO}>Cancelar</button>
              <Button onClick={() => setEtapa('confirmar')} disabled={quantidade === 0} icon="send">
                Enviar para {rotuloQuantidade}
              </Button>
            </div>
          </>
        )}

        {(etapa === 'confirmar' || etapa === 'enviando') && (
          <>
            <p className="font-body-md text-body-md text-on-surface">
              Enviar a versão {nomePublico} da Release {chave} para {rotuloQuantidade}?
            </p>
            {enviando && (
              <p role="status" className="font-body-sm text-body-sm text-on-surface-variant">Enviando… não feche esta janela.</p>
            )}
            <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
              <button type="button" onClick={() => setEtapa('escolher')} disabled={enviando} className={BOTAO_SECUNDARIO}>
                Voltar
              </button>
              <Button onClick={() => enviar([...selecionados])} isLoading={enviando}>Confirmar envio</Button>
            </div>
          </>
        )}

        {etapa === 'resultado' && resumo && (
          <>
            <p role="status" className="font-body-md text-body-md text-on-surface">
              {resumo.falhas.length === 0
                ? `Enviado para ${resumo.enviados.length} destinatário${resumo.enviados.length === 1 ? '' : 's'}.`
                : `${resumo.enviados.length} enviado(s), ${resumo.falhas.length} com falha.`}
            </p>
            <ListaDeEnvios envios={resultado.envios} />
            <div className="flex flex-col-reverse justify-end gap-sm sm:flex-row">
              <button type="button" onClick={onClose} className={BOTAO_SECUNDARIO}>Fechar</button>
              {resumo.falhas.length > 0 && (
                <Button onClick={() => enviar(resumo.idsComFalha)} icon="refresh">
                  Tentar de novo para os que falharam
                </Button>
              )}
            </div>
          </>
        )}
      </div>
    </Dialogo>
  );
}

/**
 * Lista "quem recebeu" de um envio, usada aqui e no histórico.
 * @param {{ envios: Array<{ destinatarioId: string, nome: string, email: string, status: string, erro?: string }> }} props
 */
export function ListaDeEnvios({ envios }) {
  return (
    <ul className="space-y-xs">
      {envios.map((envio) => {
        const status = STATUS_ENVIO[envio.status] ?? STATUS_ENVIO.falhou;
        return (
          <li key={envio.destinatarioId} className="flex items-start gap-sm rounded-md bg-surface-container-low p-xs">
            <span aria-hidden="true" className={`material-symbols-outlined text-[20px] ${status.className}`}>{status.icone}</span>
            <span className="min-w-0 flex-1">
              <span className="block font-body-sm text-body-sm text-on-surface">
                {envio.nome} <span className="text-on-surface-variant break-all">&lt;{envio.email}&gt;</span>
              </span>
              <span className={`block font-label-sm text-label-sm ${status.className}`}>
                {status.label}{envio.erro ? `: ${envio.erro}` : ''}
              </span>
            </span>
          </li>
        );
      })}
    </ul>
  );
}
