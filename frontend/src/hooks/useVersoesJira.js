// Regras puras (sem React) do fluxo "Processar Release" (F-17).

const semAcento = (texto) =>
  String(texto ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

/** Único lugar que conhece o formato de uma versão vinda de GET /jira/versoes. */
export function normalizarVersao(v = {}) {
  return {
    nome: v.nome ?? v.name ?? '',
    dataLancamento: v.dataLancamento ?? v.releaseDate ?? null,
    lancada: Boolean(v.lancada ?? v.released),
    statusRelease: v.statusRelease ?? v.status ?? null,
  };
}

export function normalizarVersoes(dados) {
  const lista = Array.isArray(dados) ? dados : dados?.versoes;
  return Array.isArray(lista) ? lista.map(normalizarVersao).filter((v) => v.nome) : [];
}

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

export const listaDeHistorias = (dados) => (Array.isArray(dados) ? dados : dados?.historias ?? []);

/** Total, contagem por tipo e quantas têm Release Note dedicada. */
export function resumirHistorias(dados) {
  const historias = listaDeHistorias(dados);
  const porTipo = { historia: 0, bug: 0, tarefa: 0, outros: 0 };
  let comReleaseNote = 0;
  for (const h of historias) {
    porTipo[tipoDaHistoria(h.tipoIssue)] += 1;
    if (h.possuiReleaseNoteDedicada) comReleaseNote += 1;
  }
  return { total: historias.length, porTipo, comReleaseNote };
}