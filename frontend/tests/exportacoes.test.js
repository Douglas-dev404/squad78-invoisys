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
const { listarExportacoes, buscarExportacao } = await import('../src/services/exportacoes.service.js');
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

const capturar = (corpo = {}) => {
  const chamadas = [];
  globalThis.fetch = async (url, options) => {
    chamadas.push({ url, method: options.method, auth: options.headers.Authorization });
    return Response.json(corpo);
  };
  return chamadas;
};

test('listarExportacoes manda paginação e filtros na query, com Bearer', async () => {
  const pagina = { itens: [], total: 0, pagina: 2, tamanho: 10 };
  const chamadas = capturar(pagina);
  assert.deepEqual(await listarExportacoes({ pagina: 2, tamanho: 10, publico: 'cliente', busca: 'INV 1&2' }), pagina);
  assert.equal(chamadas[0].url, 'http://localhost:8080/api/v1/exportacoes?pagina=2&tamanho=10&publico=cliente&busca=INV+1%262');
  assert.equal(chamadas[0].method, 'GET');
  assert.equal(chamadas[0].auth, 'Bearer tk');
});

test('listarExportacoes sem filtro não manda query vazia', async () => {
  const chamadas = capturar({ itens: [], total: 0, pagina: 1, tamanho: 20 });
  await listarExportacoes();
  assert.equal(chamadas[0].url, 'http://localhost:8080/api/v1/exportacoes');
});

test('buscarExportacao pede o detalhe pelo id', async () => {
  const chamadas = capturar({ id: 'abc', conteudo: '# Título' });
  assert.equal((await buscarExportacao('abc')).conteudo, '# Título');
  assert.equal(chamadas[0].url, 'http://localhost:8080/api/v1/exportacoes/abc');
});
