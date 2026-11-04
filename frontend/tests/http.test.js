import { test, beforeEach, afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';

registerHooks({
  load(url, context, nextLoad) {
    const result = nextLoad(url, context);
    if (url.endsWith('/config/api.config.js')) {
      return { ...result, source: String(result.source).replace('import.meta.env.VITE_API_BASE_URL', 'undefined') };
    }
    return result;
  },
});
const { api, ApiError } = await import('../src/services/http.js');
const { getSession, saveSession, clearSession } = await import('../src/utils/session.js');
const originalFetch = globalThis.fetch;
let storage;
const session = () => ({ token: 'test-token', expiraEm: new Date(Date.now() + 60000).toISOString(), usuario: { id: 'test' } });

beforeEach(() => {
  storage = new Map();
  globalThis.localStorage = {
    getItem: key => storage.get(key) ?? null,
    setItem: (key, value) => storage.set(key, value),
    removeItem: key => storage.delete(key),
  };
  globalThis.window = new EventTarget();
});
afterEach(() => { globalThis.fetch = originalFetch; });

test('sessão: salvar, recuperar, expirar e remover', () => {
  const value = session();
  saveSession(value);
  assert.deepEqual(getSession(), value);
  assert.equal(storage.size, 1);
  saveSession({ ...value, expiraEm: '2000-01-01T00:00:00Z' });
  assert.equal(getSession(), null);
  assert.equal(storage.size, 0);
  saveSession(value);
  clearSession();
  assert.equal(getSession(), null);
});

test('sessão: JSON inválido e localStorage indisponível', () => {
  storage.set('invoisys.session', '{');
  assert.equal(getSession(), null);
  globalThis.localStorage = {
    getItem() { throw new Error('blocked'); },
    setItem() { throw new Error('blocked'); },
    removeItem() { throw new Error('blocked'); },
  };
  assert.doesNotThrow(() => saveSession(session()));
  assert.equal(getSession(), null);
  assert.doesNotThrow(clearSession);
});

test('métodos, URL, JSON e token; auth false omite token', async () => {
  saveSession(session());
  const calls = [];
  globalThis.fetch = async (url, options) => {
    calls.push({ url, ...options });
    return Response.json({ ok: true });
  };
  assert.deepEqual(await api.get('/releases'), { ok: true });
  await api.post('/auth/login', { email: 'test@example.com' }, { auth: false });
  await api.patch('/items/1', { texto: 'Revisado' });
  await api.delete('/items/1');
  assert.equal(calls[0].url, 'http://localhost:8080/api/v1/releases');
  assert.equal(calls[0].headers.Authorization, 'Bearer test-token');
  assert.equal(calls[1].headers.Authorization, undefined);
  assert.equal(calls[1].headers['Content-Type'], 'application/json');
  assert.equal(calls[1].body, JSON.stringify({ email: 'test@example.com' }));
  assert.deepEqual(calls.map(call => call.method), ['GET', 'POST', 'PATCH', 'DELETE']);
});

test('204 retorna null', async () => {
  globalThis.fetch = async () => new Response(null, { status: 204 });
  assert.equal(await api.post('/action', {}), null);
});

test('422 ProblemDetails preserva detail e errors', async () => {
  const errors = { texto: ['Obrigatório.'] };
  globalThis.fetch = async () => Response.json({ detail: 'Dados inválidos.', errors }, { status: 422 });
  await assert.rejects(api.patch('/items/1', {}), error => {
    assert.ok(error instanceof ApiError);
    assert.equal(error.status, 422);
    assert.equal(error.detail, 'Dados inválidos.');
    assert.deepEqual(error.errors, errors);
    return true;
  });
});

test('erro usa title ou mensagem genérica, inclusive 5xx não JSON', async () => {
  globalThis.fetch = async () => Response.json({ title: 'Indisponível' }, { status: 503 });
  await assert.rejects(api.get('/releases'), { detail: 'Indisponível', status: 503 });
  globalThis.fetch = async () => new Response('<html>Erro</html>', { status: 500 });
  await assert.rejects(api.get('/releases'), { detail: 'Erro na requisição (HTTP 500).', status: 500 });
});

test('401 limpa sessão e emite evento somente com auth true', async () => {
  let events = 0;
  window.addEventListener('invoisys:unauthorized', () => events++);
  globalThis.fetch = async () => Response.json({ title: 'Não autorizado' }, { status: 401 });
  saveSession(session());
  await assert.rejects(api.post('/auth/login', {}, { auth: false }), { status: 401 });
  assert.ok(getSession());
  assert.equal(events, 0);
  await assert.rejects(api.get('/releases'), { status: 401 });
  assert.equal(getSession(), null);
  assert.equal(events, 1);
});

test('falha de rede é ApiError status 0', async () => {
  globalThis.fetch = async () => { throw new TypeError('fetch failed'); };
  await assert.rejects(api.get('/releases'), { status: 0, detail: 'Não foi possível conectar à API.' });
});

function abortableFetch(_url, { signal }) {
  return new Promise((_resolve, reject) => {
    if (signal.aborted) reject(signal.reason);
    else signal.addEventListener('abort', () => reject(signal.reason), { once: true });
  });
}

test('timeout 1ms tem mensagem específica', async () => {
  globalThis.fetch = abortableFetch;
  await assert.rejects(api.get('/releases', { timeoutMs: 1 }), { status: 0, detail: 'A API demorou demais para responder.' });
});

test('cancelamento externo funciona antes e durante a requisição', async () => {
  globalThis.fetch = abortableFetch;
  const controller = new AbortController();
  const request = api.get('/releases', { signal: controller.signal });
  controller.abort();
  await assert.rejects(request, { name: 'AbortError' });
  await assert.rejects(api.get('/releases', { signal: controller.signal }), { name: 'AbortError' });
});

test('HTTP real: 422, 204, timeout e conexão recusada', async () => {
  const { createServer } = await import('node:http');
  const server = createServer((req, res) => {
    if (req.url === '/slow') return;
    if (req.url === '/empty') { res.writeHead(204).end(); return; }
    res.writeHead(422, { 'Content-Type': 'application/problem+json' });
    res.end(JSON.stringify({ detail: 'Texto obrigatório.', errors: { texto: ['Obrigatório.'] } }));
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  globalThis.fetch = (url, opts) => originalFetch(base + new URL(url).pathname.replace('/api/v1', ''), opts);
  try {
    await assert.rejects(api.get('/invalid'), { status: 422, detail: 'Texto obrigatório.' });
    assert.equal(await api.get('/empty'), null);
    await assert.rejects(api.get('/slow', { timeoutMs: 20 }), { status: 0, detail: 'A API demorou demais para responder.' });
  } finally {
    server.closeAllConnections();
    await new Promise(resolve => server.close(resolve));
  }
  await assert.rejects(api.get('/offline'), { status: 0, detail: 'Não foi possível conectar à API.' });
});
