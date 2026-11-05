const dateTime = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
});

export function formatDateTime(value) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : dateTime.format(date).replace(', ', ' ');
}
export function formatNumber(value) {
  return new Intl.NumberFormat('pt-BR').format(value ?? 0);
}
