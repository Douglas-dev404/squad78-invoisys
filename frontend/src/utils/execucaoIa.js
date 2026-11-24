// Regras da tela "Execução da IA" (ao vivo) sem React.

export const LIMITE_EVENTOS = 500;
export const INTERVALO_POLLING_MS = 1000;

export const TIPO_EVENTO = {
  pipeline_iniciado: { label: 'Início', icone: 'play_circle', cor: 'text-primary' },
  etapa: { label: 'Etapa', icone: 'arrow_right_alt', cor: 'text-on-surface-variant' },
  ia_chamada: { label: 'Pergunta à IA', icone: 'north_east', cor: 'text-primary' },
  ia_resposta: { label: 'Resposta da IA', icone: 'south_west', cor: 'text-green-700' },
  ia_erro: { label: 'Erro da IA', icone: 'error', cor: 'text-error' },
  pipeline_concluido: { label: 'Concluído', icone: 'check_circle', cor: 'text-green-700' },
  pipeline_falhou: { label: 'Falhou', icone: 'cancel', cor: 'text-error' },
};

export const rotuloDoTipo = (tipo) => TIPO_EVENTO[tipo] ?? { label: tipo, icone: 'info', cor: 'text-on-surface-variant' };

/** Junta os eventos novos aos que já estão na tela, sem repetir id e guardando só os mais recentes. */
export function acrescentarEventos(atuais, novos, limite = LIMITE_EVENTOS) {
  if (!novos?.length) return atuais;
  const vistos = new Set(atuais.map((evento) => evento.id));
  const unicos = novos.filter((evento) => !vistos.has(evento.id));
  if (unicos.length === 0) return atuais;
  const juntos = [...atuais, ...unicos];
  return juntos.length > limite ? juntos.slice(juntos.length - limite) : juntos;
}

/** Release do evento para o filtro da tela; eventos sem Release caem em "Geral". */
export const releaseDoEvento = (evento) => evento.release || 'Geral';

export const releasesDosEventos = (eventos) => [...new Set(eventos.map(releaseDoEvento))];

export function formatarDuracaoMs(ms) {
  if (ms == null) return '';
  if (ms < 1000) return `${ms} ms`;
  return `${(ms / 1000).toFixed(1).replace('.', ',')} s`;
}

/** A Release está "rodando" se o último evento dela não foi concluído nem falha. */
export function situacaoDaRelease(eventos, release) {
  const dela = eventos.filter((evento) => releaseDoEvento(evento) === release);
  const ultimo = dela[dela.length - 1];
  if (!ultimo) return 'ociosa';
  if (ultimo.tipo === 'pipeline_concluido') return 'concluida';
  if (ultimo.tipo === 'pipeline_falhou') return 'falhou';
  return 'rodando';
}
