import { COLUNAS_QUADRO, RELEASE_STATUS, cartaoProcessa, colunaDoCartao, formatarData } from '../../utils/releases';

const formatadorDia = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' });

function Cartao({ cartao, onSelecionar }) {
  const selo = RELEASE_STATUS[cartao.status];
  const acao = cartaoProcessa(cartao) ? 'gerar o comunicado' : 'abrir a revisão';

  return (
    <li>
      <button
        type="button"
        onClick={() => onSelecionar(cartao)}
        aria-label={`Release ${cartao.chave}: ${acao}`}
        className="w-full rounded-lg border border-outline-variant/60 bg-surface-container-lowest p-sm text-left shadow-sm transition-colors hover:border-primary hover:bg-primary/5 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary"
      >
        <span className="block break-words font-label-md text-label-md font-semibold text-primary">{cartao.chave}</span>
        {selo && (
          <span className={`mt-1 inline-flex rounded-full px-2 py-0.5 text-label-sm font-semibold ${selo.className}`}>
            {selo.label}
          </span>
        )}
        {cartao.descricao && (
          <span className="mt-1 block text-body-sm text-on-surface-variant">{cartao.descricao}</span>
        )}
        <span className="mt-xs flex flex-wrap gap-x-sm gap-y-1 text-label-sm text-on-surface-variant">
          {cartao.dataLancamento && (
            <span>Lançamento: {formatadorDia.format(new Date(`${cartao.dataLancamento}T00:00:00Z`))}</span>
          )}
          {cartao.lancadaNoJira && <span>Lançada no Jira</span>}
          {!cartao.noJira && <span>Fora do projeto no Jira</span>}
          {cartao.processadaEm && <span>Gerada em {formatarData(cartao.processadaEm)}</span>}
        </span>
      </button>
    </li>
  );
}

/**
 * Kanban das Releases: uma coluna por etapa (No Jira → Em revisão → Aprovadas, e Falharam).
 * @param {Object} props
 * @param {Array} props.cartoes - vindos de GET /jira/releases
 * @param {Function} props.onSelecionar - recebe o cartão clicado
 */
export function QuadroReleases({ cartoes, onSelecionar }) {
  return (
    <section aria-label="Quadro de Releases" className="grid grid-cols-1 gap-md md:grid-cols-2 xl:grid-cols-4">
      {COLUNAS_QUADRO.map((coluna) => {
        const daColuna = cartoes.filter((cartao) => colunaDoCartao(cartao) === coluna.id);
        return (
          <section
            key={coluna.id}
            aria-labelledby={`coluna-${coluna.id}`}
            className="flex min-h-[160px] flex-col rounded-xl border border-surface-variant/50 bg-surface-container-low p-sm"
          >
            <header className="mb-sm">
              <h3 id={`coluna-${coluna.id}`} className="flex items-center gap-xs font-label-md text-label-md font-semibold text-on-surface">
                {coluna.titulo}
                <span className="rounded-full bg-surface-container-high px-2 text-label-sm text-on-surface-variant">
                  {daColuna.length}
                </span>
              </h3>
              <p className="text-label-sm text-on-surface-variant">{coluna.descricao}</p>
            </header>
            {daColuna.length === 0 ? (
              <p className="m-auto py-md text-label-sm text-on-surface-variant">Nenhuma Release aqui.</p>
            ) : (
              <ul className="flex flex-col gap-xs overflow-y-auto xl:max-h-[65vh]">
                {daColuna.map((cartao) => (
                  <Cartao key={cartao.chave} cartao={cartao} onSelecionar={onSelecionar} />
                ))}
              </ul>
            )}
          </section>
        );
      })}
    </section>
  );
}
