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
const {
  listarDestinatarios,
  cadastrarDestinatario,
  alterarDestinatario,
  desativarDestinatario,
} = await import('../src/services/destinatarios.service.js');
const { enviarRelease } = await import('../src/services/exportacoes.service.js');
const { lerFiltrosDestinatarios, validarDestinatario, resumirEnvio } = await import('../src/utils/destinatarios.js');
const { saveSession } = await import('../src/utils/session.js');
const originalFetch = globalThis.fetch;

let chamadas;

beforeEach(() => {
  const storage = new Map();
  globalThis.localStorage = {
    getItem: key => storage.get(key) ?? null,
    setItem: (key, value) => storage.set(key, value),
    removeItem: key => storage.delete(key),
  };
  globalThis.window = new EventTarget();
  saveSession({ token: 'tk', expiraEm: new Date(Date.now() + 60000).toISOString(), usuario: { id: '1' } });
  chamadas = [];
  globalThis.fetch = async (url, init = {}) => {
    chamadas.push({ url: new URL(url, 'http://local'), metodo: init.method, corpo: init.body ? JSON.parse(init.body) : undefined });
    return new Response(JSON.stringify([]), { status: 200, headers: { 'Content-Type': 'application/json' } });
  };
});
afterEach(() => { globalThis.fetch = originalFetch; });

test('listarDestinatarios: sem filtro não manda query; com filtro manda publico e incluirInativos', async () => {
  await listarDestinatarios();
  await listarDestinatarios({ publico: 'cliente', incluirInativos: true });
  assert.equal(chamadas[0].url.pathname.endsWith('/destinatarios'), true);
  assert.equal(chamadas[0].url.search, '');
  assert.equal(chamadas[1].url.searchParams.get('publico'), 'cliente');
  assert.equal(chamadas[1].url.searchParams.get('incluirInativos'), 'true');
});

test('cadastrar e alterar mandam nome e email com trim e o público', async () => {
  await cadastrarDestinatario({ nome: '  ACME ', email: ' a@acme.com ', publico: 'suporte' });
  await alterarDestinatario('id 1', { nome: 'B', email: 'b@acme.com', publico: 'cliente' });
  assert.equal(chamadas[0].metodo, 'POST');
  assert.deepEqual(chamadas[0].corpo, { nome: 'ACME', email: 'a@acme.com', publico: 'suporte' });
  assert.equal(chamadas[1].metodo, 'PUT');
  assert.equal(chamadas[1].url.pathname.endsWith('/destinatarios/id%201'), true);
});

test('desativar e enviarRelease usam POST nas rotas certas', async () => {
  await desativarDestinatario('abc');
  await enviarRelease('2026.10.0', { publico: 'cliente', destinatarioIds: ['x', 'y'] });
  assert.equal(chamadas[0].url.pathname.endsWith('/destinatarios/abc/desativar'), true);
  assert.equal(chamadas[1].url.pathname.endsWith('/releases/2026.10.0/enviar'), true);
  assert.deepEqual(chamadas[1].corpo, { publico: 'cliente', destinatarioIds: ['x', 'y'] });
});

test('lerFiltrosDestinatarios: ignora público desconhecido e lê inativos=1', () => {
  assert.deepEqual(lerFiltrosDestinatarios(new URLSearchParams('publico=CLIENTE&inativos=1')), { publico: 'cliente', inativos: true });
  assert.deepEqual(lerFiltrosDestinatarios(new URLSearchParams('publico=diretoria')), { publico: '', inativos: false });
});

test('validarDestinatario: exige nome, e-mail válido e público', () => {
  assert.equal(validarDestinatario({ nome: 'A', email: 'a@acme.com', publico: 'cliente' }).valido, true);
  const { valido, erros } = validarDestinatario({ nome: '  ', email: 'nao-e-email', publico: 'diretoria' });
  assert.equal(valido, false);
  assert.deepEqual(Object.keys(erros).sort(), ['email', 'nome', 'publico']);
  assert.equal(validarDestinatario({ nome: 'x'.repeat(201), email: 'a@b.com', publico: 'interno' }).erros.nome !== undefined, true);
});

test('resumirEnvio: separa enviados de falhas e devolve os ids para tentar de novo', () => {
  const { enviados, falhas, idsComFalha } = resumirEnvio([
    { destinatarioId: '1', status: 'enviado' },
    { destinatarioId: '2', status: 'falhou', erro: '503' },
  ]);
  assert.equal(enviados.length, 1);
  assert.equal(falhas.length, 1);
  assert.deepEqual(idsComFalha, ['2']);
});
