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
const { listarQuadroReleases } = await import('../src/services/releases.service.js');
const { colunaDoCartao, cartaoProcessa } = await import('../src/utils/releases.js');
const { saveSession } = await import('../src/utils/session.js');
const originalFetch = globalThis.fetch;

beforeEach(() => {
  const storage = new Map();
  globalThis.localStorage = {
    getItem: key => storage.get(key) ?? null,
    setItem: (key, value) => storage.set(key, value),
    removeItem: key => storage.delete(key),
  };
  globalThis.window = new EventTarget();
  saveSession({ token: 'tk', expiraEm: new Date(Date.now() + 60000).toISOString(), usuario: { id: '1' } });
});
afterEach(() => { globalThis.fetch = originalFetch; });

test('listarQuadroReleases chama GET /jira/releases com Bearer', async () => {
  const chamadas = [];
  globalThis.fetch = async (url, options) => {
    chamadas.push({ url, method: options.method, auth: options.headers.Authorization });
    return Response.json({ cartoes: [], avisoJira: null });
  };
  assert.deepEqual(await listarQuadroReleases(), { cartoes: [], avisoJira: null });
  assert.deepEqual(chamadas, [{ url: 'http://localhost:8080/api/v1/jira/releases', method: 'GET', auth: 'Bearer tk' }]);
});

test('cada status cai na coluna certa do quadro', () => {
  const coluna = (status) => colunaDoCartao({ status });
  assert.equal(coluna('nao_processada'), 'jira');
  assert.equal(coluna('aguardando_revisao'), 'revisao');
  assert.equal(coluna('processando'), 'revisao');
  assert.equal(coluna('aprovado'), 'aprovadas');
  assert.equal(coluna('falhou'), 'falharam');
  assert.equal(coluna('desconhecido'), 'revisao');
});

test('só Release sem comunicado ou que falhou dispara a IA ao clicar', () => {
  assert.equal(cartaoProcessa({ status: 'nao_processada' }), true);
  assert.equal(cartaoProcessa({ status: 'falhou' }), true);
  assert.equal(cartaoProcessa({ status: 'aguardando_revisao' }), false);
  assert.equal(cartaoProcessa({ status: 'aprovado' }), false);
});
