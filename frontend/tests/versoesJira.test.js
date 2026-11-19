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
const { carregarVersoes } = await import('../src/hooks/useVersoesJira.js');
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

test('useVersoesJira: carrega GET /jira/versoes com Bearer e normaliza', async () => {
  let chamada;
  globalThis.fetch = async (url, options) => {
    chamada = { url, method: options.method, auth: options.headers.Authorization };
    return Response.json([{ nome: '2026.10.0', dataLancamento: '2026-10-15', lancada: true, statusRelease: 'aguardando_revisao' }]);
  };
  const r = await carregarVersoes();
  assert.deepEqual(chamada, { url: 'http://localhost:8080/api/v1/jira/versoes', method: 'GET', auth: 'Bearer tk' });
  assert.equal(r.erro, null);
  assert.deepEqual(r.versoes, [{ nome: '2026.10.0', dataLancamento: '2026-10-15', lancada: true, statusRelease: 'aguardando_revisao' }]);
});

test('useVersoesJira: 502 do Jira vira mensagem da API, sem rejeitar', async () => {
  globalThis.fetch = async () => Response.json({ detail: 'Jira indisponível.' }, { status: 502 });
  assert.deepEqual(await carregarVersoes(), { versoes: [], erro: 'Jira indisponível.' });
});

test('useVersoesJira: lista vazia é respeitada', async () => {
  globalThis.fetch = async () => Response.json([]);
  assert.deepEqual(await carregarVersoes(), { versoes: [], erro: null });
});

test('useVersoesJira: cancelamento propaga em vez de virar erro de tela', async () => {
  globalThis.fetch = (_url, { signal }) =>
    new Promise((_ok, reject) => signal.addEventListener('abort', () => reject(signal.reason), { once: true }));
  const controller = new AbortController();
  const pendente = carregarVersoes({ signal: controller.signal });
  controller.abort();
  await assert.rejects(pendente, { name: 'AbortError' });
});

test('listarHistorias: codifica a chave e usa GET', async () => {
  let chamada;
  globalThis.fetch = async (url, options) => {
    chamada = { url, method: options.method };
    return Response.json([]);
  };
  await listarHistorias('2026.10 / hotfix');
  assert.equal(chamada.url, 'http://localhost:8080/api/v1/releases/2026.10%20%2F%20hotfix/historias');
  assert.equal(chamada.method, 'GET');
});

test('normalizarVersoes aceita lista, objeto com versoes e ignora lixo', () => {
  assert.deepEqual(util.normalizarVersoes({ versoes: [{ name: 'A', released: true }] }), [
    { nome: 'A', dataLancamento: null, lancada: true, statusRelease: null },
  ]);
  assert.deepEqual(util.normalizarVersoes(null), []);
  assert.deepEqual(util.normalizarVersoes([{}]), []);
});

test('filtrarVersoes ignora acento e maiúsculas', () => {
  const lista = [{ nome: '2026.10.0' }, { nome: 'Hotfix-Março' }];
  assert.deepEqual(util.filtrarVersoes(lista, 'marco').map((v) => v.nome), ['Hotfix-Março']);
  assert.equal(util.filtrarVersoes(lista, '  ').length, 2);
});

test('resumirHistorias conta por tipo e Release Note dedicada', () => {
  const resumo = util.resumirHistorias([
    { chave: 'A-1', tipoIssue: 'História', possuiReleaseNoteDedicada: true },
    { chave: 'A-2', tipoIssue: 'Bug' },
    { chave: 'A-3', tipoIssue: 'Tarefa', possuiReleaseNoteDedicada: true },
    { chave: 'A-4', tipoIssue: 'Épico' },
  ]);
  assert.deepEqual(resumo, { total: 4, porTipo: { historia: 1, bug: 1, tarefa: 1, outros: 1 }, comReleaseNote: 2 });
  assert.equal(util.resumirHistorias([]).total, 0);
});

test('formatarDataLancamento não recua o dia no fuso do Brasil', () => {
  assert.equal(util.formatarDataLancamento('2026-10-15'), '15/10/2026');
  assert.equal(util.formatarDataLancamento(null), '—');
});