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
const servico = await import('../src/services/integracoes.service.js');
const util = await import('../src/utils/integracoes.js');
const exec = await import('../src/utils/execucaoIa.js');
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

const BASE = 'http://localhost:8080/api/v1';

test('serviço: PUT /integracoes/jira envia o corpo com Bearer e devolve a visão nova', async () => {
  let chamada;
  globalThis.fetch = async (url, options) => {
    chamada = { url, method: options.method, auth: options.headers.Authorization, body: JSON.parse(options.body) };
    return Response.json({ jira: { baseUrl: 'https://x.atlassian.net', tokenConfigurado: true } });
  };
  const visao = await servico.salvarJira({ baseUrl: 'https://x.atlassian.net', apiToken: 's3' });
  assert.deepEqual(chamada, {
    url: `${BASE}/integracoes/jira`,
    method: 'PUT',
    auth: 'Bearer tk',
    body: { baseUrl: 'https://x.atlassian.net', apiToken: 's3' },
  });
  assert.equal(visao.jira.tokenConfigurado, true);
});

test('serviço: 403 de quem não é admin chega com status para a UI', async () => {
  globalThis.fetch = async () => new Response(null, { status: 403 });
  await assert.rejects(servico.obterIntegracoes(), { status: 403 });
});

test('serviço: 422 traz os erros por campo', async () => {
  globalThis.fetch = async () => Response.json({ errors: { BaseUrl: ['Informe a URL com https://'] } }, { status: 422 });
  await assert.rejects(servico.salvarIa({ provedor: 'X' }), (erro) => {
    assert.equal(erro.status, 422);
    assert.deepEqual(util.errosPorCampo(erro.errors), { baseUrl: 'Informe a URL com https://' });
    return true;
  });
});

test('serviço: eventos ao vivo pedem a partir do último id e filtram por Release', async () => {
  const urls = [];
  globalThis.fetch = async (url) => {
    urls.push(url);
    return Response.json({ ultimoId: 7, eventos: [] });
  };
  await servico.buscarEventosAoVivo({ depois: 5 });
  await servico.buscarEventosAoVivo({ depois: 0, release: '2026.10.0' });
  assert.deepEqual(urls, [`${BASE}/execucoes/ao-vivo?depois=5`, `${BASE}/execucoes/ao-vivo?depois=0&release=2026.10.0`]);
});

test('payloadJira: token em branco vira undefined e some do JSON (mantém o atual)', () => {
  const corpo = util.payloadJira({ baseUrl: ' https://x.atlassian.net ', email: 'a@b.com ', projeto: 'INV', apiToken: '   ' });
  assert.equal(corpo.apiToken, undefined);
  assert.equal(JSON.stringify(corpo).includes('apiToken'), false);
  assert.equal(corpo.baseUrl, 'https://x.atlassian.net');
});

test('jiraAlterado: só habilita salvar quando algo mudou, token digitado conta', () => {
  const jira = { baseUrl: 'https://x.atlassian.net', email: 'a@b.com', projeto: 'INV' };
  const form = util.formJiraInicial(jira);
  assert.equal(util.jiraAlterado(form, jira), false);
  assert.equal(util.jiraAlterado({ ...form, projeto: 'ABC' }, jira), true);
  assert.equal(util.jiraAlterado({ ...form, apiToken: 'novo' }, jira), true);
});

test('IA: cada provedor guarda o próprio modelo e trocar de provedor conta como alteração', () => {
  const ia = {
    provedor: 'OpenAi',
    openAi: { modelo: 'gpt-4o-mini', chaveConfigurada: true },
    openRouter: { modelo: 'openai/gpt-4o-mini', chaveConfigurada: false },
  };
  const form = util.formIaInicial(ia);
  assert.deepEqual(form, { provedor: 'OpenAi', modelo: 'gpt-4o-mini', apiKey: '' });
  assert.equal(util.iaAlterada(form, ia), false);
  assert.equal(util.iaAlterada({ ...form, provedor: 'OpenRouter' }, ia), true);
  assert.equal(util.dadosDoProvedor(ia, 'OpenRouter').modelo, 'openai/gpt-4o-mini');
  assert.equal(util.payloadIa({ ...form, apiKey: ' sk-1 ' }).apiKey, 'sk-1');
});

test('acrescentarEventos: não repete id, mantém a ordem e guarda só os mais recentes', () => {
  const ev = (id) => ({ id, tipo: 'etapa', mensagem: `e${id}` });
  const base = [ev(1), ev(2)];
  assert.equal(exec.acrescentarEventos(base, []), base);
  assert.equal(exec.acrescentarEventos(base, [ev(2)]), base);
  assert.deepEqual(exec.acrescentarEventos(base, [ev(2), ev(3)]).map((e) => e.id), [1, 2, 3]);
  assert.deepEqual(exec.acrescentarEventos(base, [ev(3), ev(4), ev(5)], 3).map((e) => e.id), [3, 4, 5]);
});

test('situacaoDaRelease: rodando até o evento final; eventos sem Release caem em Geral', () => {
  const eventos = [
    { id: 1, release: '2026.10.0', tipo: 'pipeline_iniciado' },
    { id: 2, release: '2026.10.0', tipo: 'ia_chamada' },
    { id: 3, release: '2026.11.0', tipo: 'pipeline_falhou' },
    { id: 4, release: null, tipo: 'etapa' },
  ];
  assert.equal(exec.situacaoDaRelease(eventos, '2026.10.0'), 'rodando');
  assert.equal(exec.situacaoDaRelease(eventos, '2026.11.0'), 'falhou');
  assert.equal(exec.situacaoDaRelease(eventos, 'Geral'), 'rodando');
  assert.equal(exec.situacaoDaRelease(eventos, 'outra'), 'ociosa');
  assert.deepEqual(exec.releasesDosEventos(eventos), ['2026.10.0', '2026.11.0', 'Geral']);
});

test('formatarDuracaoMs e rotulo de tipo desconhecido', () => {
  assert.equal(exec.formatarDuracaoMs(850), '850 ms');
  assert.equal(exec.formatarDuracaoMs(2340), '2,3 s');
  assert.equal(exec.formatarDuracaoMs(null), '');
  assert.equal(exec.rotuloDoTipo('novo_tipo').label, 'novo_tipo');
  assert.equal(exec.rotuloDoTipo('ia_erro').label, 'Erro da IA');
});
