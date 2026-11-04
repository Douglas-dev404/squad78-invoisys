import { test, afterEach } from 'node:test';
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
const { getHeroHighlights } = await import('../src/services/branding.service.js');
const { MOCK_HERO_HIGHLIGHTS } = await import('../src/data/branding.mock.js');
const originalFetch = globalThis.fetch;
const originalWarn = console.warn;
afterEach(() => { globalThis.fetch = originalFetch; console.warn = originalWarn; });

test('destaques usam endpoint público e convertem o contrato para a UI', async () => {
  globalThis.fetch = async (url, options) => {
    assert.equal(url, 'http://localhost:8080/api/v1/branding/highlights');
    assert.equal(options.headers.Authorization, undefined);
    return Response.json([{ id: '1', titulo: 'Do banco', descricao: 'Descrição', icone: 'task_alt' }]);
  };
  assert.deepEqual(await getHeroHighlights(), [{ id: '1', title: 'Do banco', description: 'Descrição', icon: 'task_alt' }]);
});

test('resposta vazia é respeitada', async () => {
  globalThis.fetch = async () => Response.json([]);
  assert.deepEqual(await getHeroHighlights(), []);
});

test('API indisponível retorna fallback e registra detail sem rejeitar', async () => {
  const warnings = [];
  console.warn = (...args) => warnings.push(args);
  globalThis.fetch = async () => Response.json({ detail: 'Indisponível' }, { status: 503 });
  assert.deepEqual(await getHeroHighlights(), MOCK_HERO_HIGHLIGHTS);
  assert.equal(warnings[0][1], 'Indisponível');
});
