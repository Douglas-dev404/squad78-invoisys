const semAcento = (texto) =>
  String(texto ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

export function filtrarVersoes(versoes, busca) {
  const termo = semAcento(busca).trim();
  return termo ? versoes.filter((v) => semAcento(v.nome).includes(termo)) : versoes;
}

/** Formata em UTC para o dia não recuar no fuso do Brasil. */
export function formatarDataLancamento(data) {
  const d = new Date(data);
  return data && !Number.isNaN(d.getTime()) ? d.toLocaleDateString('pt-BR', { timeZone: 'UTC' }) : '—';
}

export const ROTULO_TIPO = { historia: 'História', bug: 'Bug', tarefa: 'Tarefa', outros: 'Outros' };

export function tipoDaHistoria(tipoIssue) {
  const tipo = semAcento(tipoIssue);
  if (tipo === 'historia' || tipo === 'story') return 'historia';
  if (tipo === 'bug') return 'bug';
  if (tipo === 'tarefa' || tipo === 'task') return 'tarefa';
  return 'outros';
}

export const listaDeHistorias = (release) => release?.historias ?? [];

/** Total, contagem por tipo e quantas têm Release Note dedicada. */
export function resumirHistorias(release) {
  const historias = listaDeHistorias(release);
  const porTipo = { historia: 0, bug: 0, tarefa: 0, outros: 0 };
  let comReleaseNote = 0;
  for (const h of historias) {
    porTipo[tipoDaHistoria(h.tipoIssue)] += 1;
    if (h.possuiReleaseNoteDedicada) comReleaseNote += 1;
  }
  return { total: historias.length, porTipo, comReleaseNote };
}
