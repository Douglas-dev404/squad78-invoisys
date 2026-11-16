export const PAGE_SIZE = 10;

export const RELEASE_STATUS = {
  pendente: { label: 'Pendente', className: 'bg-surface-container-high text-on-surface-variant' },
  processando: { label: 'Processando', className: 'bg-blue-100 text-blue-800' },
  aguardando_revisao: { label: 'Aguardando revisão', className: 'bg-amber-100 text-amber-800' },
  aprovado: { label: 'Aprovada', className: 'bg-green-100 text-green-800' },
  falhou: { label: 'Falhou', className: 'bg-error-container text-on-error-container' },
};

export const RELEASE_FILTER_TABS = [
  { value: '', label: 'Todas' },
  { value: 'aguardando_revisao', label: 'Aguardando revisão' },
  { value: 'aprovado', label: 'Aprovadas' },
  { value: 'falhou', label: 'Falharam' },
  { value: 'pendente', label: 'Pendentes' },
];

const formatadorData = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

export function formatarData(iso) {
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return '—';
  return formatadorData.format(data).replace(',', '');
}

export const COLUNAS_QUADRO = [
  {
    id: 'jira',
    titulo: 'No Jira',
    descricao: 'Ainda sem comunicado. Clique para gerar.',
    status: ['nao_processada'],
  },
  {
    id: 'revisao',
    titulo: 'Em revisão',
    descricao: 'Comunicado gerado, aguardando revisão humana.',
    status: ['pendente', 'processando', 'aguardando_revisao'],
  },
  { id: 'aprovadas', titulo: 'Aprovadas', descricao: 'Prontas para exportar.', status: ['aprovado'] },
  { id: 'falharam', titulo: 'Falharam', descricao: 'Clique para tentar de novo.', status: ['falhou'] },
];

/** Em que coluna o cartão cai, pelo status no InvoiSys. */
export const colunaDoCartao = (cartao) =>
  COLUNAS_QUADRO.find((coluna) => coluna.status.includes(cartao.status))?.id ?? 'revisao';

/** Cartão sem comunicado (ou que falhou) dispara a IA; os demais abrem a tela da Release. */
export const cartaoProcessa = (cartao) => cartao.status === 'nao_processada' || cartao.status === 'falhou';
