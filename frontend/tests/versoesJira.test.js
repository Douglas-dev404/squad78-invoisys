import { test, beforeEach, afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';

registerHooks({
  load(url, context, nextLoad) {
    const result = nextLoad(url, context);
    return url.endsWith('/config/api.config.js')
      ? { ...result, source: String(result.source).replace('import.meta.env.VITE_API_BASE_URL', 'undefined') }
      : result;
  },
});
const { listarVersoesJira } = await import('../src/services/jira.service.js');
const { listarHistorias } = await import('../src/services/releases.service.js');
const util = await import('../src/utils/versoesJira.js');
const { saveSession } = await import('../src/utils/session.js');
const originalFetch = globalThis.fetch;

beforeEach(() => {
  const storage = new Map();
  globalThis.localStorage = {
    getItem: (key) => storage.get(key) ?? null,
    setItem: (key, value) => storage.set(key, value),
    removeItem: (key) => storage.delete(key),
  };
  globalThis.window = new EventTarget();
  saveSession({ token: 'tk', expiraEm: new Date(Date.now() + 60000).toISOString(), usuario: { id: '1' } });
});
afterEach(() => { globalThis.fetch = originalFetch; });

const versaoJira = {
  nome: '2026.10.0',
  descricao: 'Release de outubro',
  dataLancamento: '2026-10-15',
  lancada: true,
  statusInvoisys: 'aguardando_revisao',
};

test('listarVersoesJira: GET /jira/versoes com Bearer devolve a lista do backend como veio', async () => {
  let chamada;
  globalThis.fetch = async (url, options) => {
    chamada = { url, method: options.method, auth: options.headers.Authorization };
    return Response.json([versaoJira, { ...versaoJira, nome: '2026.11.0', lancada: false, statusInvoisys: null }]);
  };
  const versoes = await listarVersoesJira();
  assert.deepEqual(chamada, { url: 'http://localhost:8080/api/v1/jira/versoes', method: 'GET', auth: 'Bearer tk' });
  assert.deepEqual(versoes.map((v) => [v.nome, v.statusInvoisys]), [['2026.10.0', 'aguardando_revisao'], ['2026.11.0', null]]);
});

test('listarVersoesJira: 502 do Jira chega com o detail para a UI mostrar', async () => {
  globalThis.fetch = async () => Response.json({ detail: 'Jira indisponível.' }, { status: 502 });
  await assert.rejects(listarVersoesJira(), { status: 502, detail: 'Jira indisponível.' });
});

test('listarVersoesJira: cancelamento propaga como AbortError', async () => {
  globalThis.fetch = (_url, { signal }) =>
    new Promise((_ok, reject) => signal.addEventListener('abort', () => reject(signal.reason), { once: true }));
  const controller = new AbortController();
  const pendente = listarVersoesJira({ signal: controller.signal });
  controller.abort();
  await assert.rejects(pendente, { name: 'AbortError' });
});

test('listarHistorias: codifica a chave e usa GET', async () => {
  let chamada;
  globalThis.fetch = async (url, options) => {
    chamada = { url, method: options.method };
    return Response.json({ chaveJira: '2026.10 / hotfix', status: 'pendente', totalHistorias: 0, historias: [] });
  };
  await listarHistorias('2026.10 / hotfix');
  assert.equal(chamada.url, 'http://localhost:8080/api/v1/releases/2026.10%20%2F%20hotfix/historias');
  assert.equal(chamada.method, 'GET');
});

test('filtrarVersoes ignora acento e maiúsculas', () => {
  const lista = [{ nome: '2026.10.0' }, { nome: 'Hotfix-Março' }];
  assert.deepEqual(util.filtrarVersoes(lista, 'marco').map((v) => v.nome), ['Hotfix-Março']);
  assert.equal(util.filtrarVersoes(lista, '  ').length, 2);
});

test('resumirHistorias conta por tipo e Release Note dedicada', () => {
  const historia = (chave, tipoIssue, possuiReleaseNoteDedicada = false) => ({ chave, titulo: chave, tipoIssue, possuiReleaseNoteDedicada });
  const release = {
    chaveJira: '2026.10.0',
    status: 'pendente',
    totalHistorias: 4,
    historias: [historia('A-1', 'História', true), historia('A-2', 'Bug'), historia('A-3', 'Tarefa', true), historia('A-4', 'Épico')],
  };
  assert.deepEqual(util.resumirHistorias(release), { total: 4, porTipo: { historia: 1, bug: 1, tarefa: 1, outros: 1 }, comReleaseNote: 2 });
  assert.deepEqual(util.listaDeHistorias(release).map((h) => h.chave), ['A-1', 'A-2', 'A-3', 'A-4']);
  assert.equal(util.resumirHistorias({ ...release, totalHistorias: 0, historias: [] }).total, 0);
  assert.equal(util.resumirHistorias(null).total, 0);
});

test('formatarDataLancamento não recua o dia no fuso do Brasil', () => {
  assert.equal(util.formatarDataLancamento('2026-10-15'), '15/10/2026');
  assert.equal(util.formatarDataLancamento(null), '—');
});
