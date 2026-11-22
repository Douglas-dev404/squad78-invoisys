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
const { listarExportacoes } = await import('../src/services/exportacoes.service.js');
const { lerFiltros, BUSCA_MAX } = await import('../src/utils/exportacoes.js');
const { saveSession } = await import('../src/utils/session.js');
const originalFetch = globalThis.fetch;

let urls;

beforeEach(() => {
  const storage = new Map();
  globalThis.localStorage = {
    getItem: key => storage.get(key) ?? null,
    setItem: (key, value) => storage.set(key, value),
    removeItem: key => storage.delete(key),
  };
  globalThis.window = new EventTarget();
  saveSession({ token: 'tk', expiraEm: new Date(Date.now() + 60000).toISOString(), usuario: { id: '1' } });
  urls = [];
  globalThis.fetch = async (url) => {
    urls.push(new URL(url, 'http://local'));
    return new Response(JSON.stringify({ itens: [], total: 0, pagina: 1, tamanho: 20 }), { status: 200, headers: { 'Content-Type': 'application/json' } });
  };
});
afterEach(() => { globalThis.fetch = originalFetch; });

test('listarExportacoes: manda pagina, tamanho, publico e busca com trim', async () => {
  await listarExportacoes({ pagina: 3, tamanho: 10, publico: 'cliente', busca: '  INV-1 ' });
  const [url] = urls;
  assert.equal(url.pathname.endsWith('/exportacoes'), true);
  assert.deepEqual(Object.fromEntries(url.searchParams), { pagina: '3', tamanho: '10', publico: 'cliente', busca: 'INV-1' });
});

test('listarExportacoes: omite publico e busca vazios e usa a página 1 de 20 por padrão', async () => {
  await listarExportacoes({ publico: '', busca: '   ' });
  assert.deepEqual(Object.fromEntries(urls[0].searchParams), { pagina: '1', tamanho: '20' });
});

test('lerFiltros: sem parâmetros volta para a página 1 sem filtro', () => {
  assert.deepEqual(lerFiltros(new URLSearchParams()), { pagina: 1, publico: '', busca: '' });
});

test('lerFiltros: página inválida vira 1', () => {
  for (const pagina of ['0', '-2', 'abc', '']) {
    assert.equal(lerFiltros(new URLSearchParams({ pagina })).pagina, 1);
  }
  assert.equal(lerFiltros(new URLSearchParams({ pagina: '4' })).pagina, 4);
});

test('lerFiltros: público desconhecido é ignorado e maiúsculas são aceitas', () => {
  assert.equal(lerFiltros(new URLSearchParams({ publico: 'diretoria' })).publico, '');
  assert.equal(lerFiltros(new URLSearchParams({ publico: 'Cliente' })).publico, 'cliente');
  assert.equal(lerFiltros(new URLSearchParams({ publico: 'SUPORTE' })).publico, 'suporte');
});

test('lerFiltros: busca com trim e cortada no limite da API', () => {
  assert.equal(lerFiltros(new URLSearchParams({ busca: '  INV ' })).busca, 'INV');
  assert.equal(lerFiltros(new URLSearchParams({ busca: 'x'.repeat(250) })).busca.length, BUSCA_MAX);
});
