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