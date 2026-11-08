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
const auth = await import('../src/services/auth.service.js');
const { saveSession, getSession } = await import('../src/utils/session.js');
const originalFetch = globalThis.fetch;
const usuario = { id: '1', nome: 'Revisora', email: 'revisora@example.test', papel: 'release_manager', avatarUrl: null, criadoEm: '2026-11-04T12:00:00Z' };
beforeEach(() => {
  const storage = new Map();
  globalThis.localStorage = {
    getItem: key => storage.get(key) ?? null,
    setItem: (key, value) => storage.set(key, value),
    removeItem: key => storage.delete(key),
  };
  globalThis.window = new EventTarget();
});
afterEach(() => { globalThis.fetch = originalFetch; });

test('login envia senha, sem Bearer, e adapta a identidade retornada pela API', async () => {
  saveSession({ token: 'antigo', expiraEm: new Date(Date.now() + 60000).toISOString(), usuario });
  globalThis.fetch = async (url, options) => {
    assert.equal(url, 'http://localhost:8080/api/v1/auth/login');
    assert.deepEqual(JSON.parse(options.body), { email: usuario.email, senha: 'temporaria' });
    assert.equal(options.headers.Authorization, undefined);
    return Response.json({ usuario, token: 'novo', expiraEm: '2026-11-05T12:00:00Z' });
  };
  const response = await auth.login({ email: usuario.email, password: 'temporaria' });
  assert.deepEqual(response.usuario, { id: '1', name: 'Revisora', email: usuario.email, role: 'release_manager', avatarUrl: null, createdAt: usuario.criadoEm });
  assert.equal(response.token, 'novo');
});

test('logout elimina a sessão mesmo se a rede falhar', async () => {
  saveSession({ token: 'antigo', expiraEm: new Date(Date.now() + 60000).toISOString(), usuario });
  globalThis.fetch = async () => { throw new TypeError('rede'); };
  await assert.rejects(auth.logout());
  assert.equal(getSession(), null);
});

test('me usa Bearer e adapta o usuário do servidor', async () => {
  saveSession({ token: 'ativo', expiraEm: new Date(Date.now() + 60000).toISOString(), usuario });
  globalThis.fetch = async (url, options) => {
    assert.ok(url.endsWith('/auth/me'));
    assert.equal(options.headers.Authorization, 'Bearer ativo');
    return Response.json(usuario);
  };
  assert.equal((await auth.getCurrentUser()).name, usuario.nome);
});

test('recuperação e reenvio são públicos e preservam mensagem e validade da API', async () => {
  const calls = [];
  const response = { mensagem: 'Se cadastrado, enviaremos o link.', expiraEmMinutos: 30 };
  globalThis.fetch = async (url, options) => {
    calls.push(url);
    assert.equal(options.headers.Authorization, undefined);
    assert.deepEqual(JSON.parse(options.body), { email: usuario.email });
    return Response.json(response);
  };
  assert.deepEqual(await auth.requestPasswordReset({ email: usuario.email }), response);
  assert.deepEqual(await auth.resendPasswordReset({ email: usuario.email }), response);
  assert.ok(calls[0].endsWith('/auth/forgot-password'));
  assert.ok(calls[1].endsWith('/auth/resend-forgot-password'));
});
