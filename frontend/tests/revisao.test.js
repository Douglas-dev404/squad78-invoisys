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
const releases = await import('../src/services/releases.service.js');
const revisao = await import('../src/utils/revisao.js');
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

const capturar = (status = 200, corpo = {}) => {
  const chamadas = [];
  globalThis.fetch = async (url, options) => {
    chamadas.push({ url, method: options.method, body: options.body ? JSON.parse(options.body) : undefined, auth: options.headers.Authorization });
    return new Response(status === 204 ? null : JSON.stringify(corpo), { status, headers: { 'Content-Type': 'application/json' } });
  };
  return chamadas;
};

const item = (incluido = true) => ({ id: 'i1', incluido });

test('acoesDaVersao: aguardando_revisao oferece aprovar, reprovar, reprocessar e edição', () => {
  const acoes = revisao.acoesDaVersao({ status: 'aguardando_revisao', itens: [item()] }, 'aguardando_revisao');
  assert.deepEqual(
    { aprovar: acoes.aprovar, reprovar: acoes.reprovar, reabrir: acoes.reabrir, reprocessar: acoes.reprocessar, exportar: acoes.exportar, editarItens: acoes.editarItens },
    { aprovar: true, reprovar: true, reabrir: false, reprocessar: true, exportar: false, editarItens: true },
  );
  assert.equal(acoes.aprovarDesabilitadoMotivo, null);
  assert.equal(acoes.edicaoBloqueadaMotivo, null);
});

test('acoesDaVersao: aprovado só reabre e exporta, sem edição nem reprocessar', () => {
  const acoes = revisao.acoesDaVersao({ status: 'aprovado', itens: [item()] }, 'aprovado');
  assert.deepEqual(
    { aprovar: acoes.aprovar, reprovar: acoes.reprovar, reabrir: acoes.reabrir, reprocessar: acoes.reprocessar, exportar: acoes.exportar, editarItens: acoes.editarItens },
    { aprovar: false, reprovar: false, reabrir: true, reprocessar: false, exportar: true, editarItens: false },
  );
  assert.equal(acoes.edicaoBloqueadaMotivo, 'Versão aprovada. Reabra para editar.');
});

test('acoesDaVersao: reprovado só reprocessa (domínio recusa reabrir e aprovar)', () => {
  const acoes = revisao.acoesDaVersao({ status: 'reprovado', itens: [item()] }, 'aguardando_revisao');
  assert.deepEqual(
    { aprovar: acoes.aprovar, reprovar: acoes.reprovar, reabrir: acoes.reabrir, reprocessar: acoes.reprocessar, exportar: acoes.exportar, editarItens: acoes.editarItens },
    { aprovar: false, reprovar: false, reabrir: false, reprocessar: true, exportar: false, editarItens: false },
  );
  assert.match(acoes.edicaoBloqueadaMotivo, /Reprocesse/);
});

test('acoesDaVersao: sem item incluído desabilita aprovar com o motivo; pipeline falhou esconde aprovar', () => {
  const semItem = revisao.acoesDaVersao({ status: 'aguardando_revisao', itens: [item(false)] });
  assert.equal(semItem.aprovar, true);
  assert.equal(semItem.aprovarDesabilitadoMotivo, 'Inclua pelo menos um item.');
  assert.equal(revisao.acoesDaVersao({ status: 'aguardando_revisao', itens: [item()] }, 'falhou').aprovar, false);
});

test('substituirItem troca só o item devolvido pela API e preserva a ordem', () => {
  const lista = [{ id: 'a', textoFinal: 'A' }, { id: 'b', textoFinal: 'B' }];
  const nova = revisao.substituirItem(lista, { id: 'b', textoFinal: 'B editado', textoEditadoManualmente: 'B editado' });
  assert.deepEqual(nova.map(i => i.textoFinal), ['A', 'B editado']);
  assert.equal(lista[1].textoFinal, 'B', 'não muta a lista original');
});

test('nomeArquivoMarkdown e validações de texto', () => {
  assert.equal(revisao.nomeArquivoMarkdown('INV-123', 'cliente'), 'INV-123-cliente.md');
  assert.equal(revisao.nomeArquivoMarkdown('2026.10 / hotfix', 'cliente'), '2026.10-hotfix-cliente.md');
  assert.equal(revisao.nomeArquivoMarkdown('a\\b:c', 'interno'), 'a-b-c-interno.md');
  assert.equal(revisao.textoItemValido('   '), false);
  assert.equal(revisao.textoItemValido(' ok '), true);
  assert.equal(revisao.erroDoCampo({ errors: { texto: ['Texto vazio.', 'Outro.'] } }, 'texto'), 'Texto vazio. Outro.');
  assert.equal(revisao.erroDoCampo({ errors: { Motivo: ['Obrigatório.'] } }, 'motivo'), 'Obrigatório.');
  assert.equal(revisao.erroDoCampo({}, 'texto'), null);
});

test('services de item: rota, método e corpo do contrato, com Bearer', async () => {
  const chamadas = capturar(200, { id: 'i1' });
  await releases.editarItem('2026.10.0', 'i1', 'novo');
  await releases.excluirItem('2026.10.0', 'i1', '  interno  ');
  await releases.excluirItem('2026.10.0', 'i1', '   ');
  await releases.reincluirItem('2026.10.0', 'i1');
  assert.deepEqual(chamadas.map(c => [c.method, c.url.replace(/^.*\/api\/v1/, ''), c.body]), [
    ['PATCH', '/releases/2026.10.0/itens/i1', { texto: 'novo' }],
    ['POST', '/releases/2026.10.0/itens/i1/excluir', { motivo: 'interno' }],
    ['POST', '/releases/2026.10.0/itens/i1/excluir', {}],
    ['POST', '/releases/2026.10.0/itens/i1/reincluir', undefined],
  ]);
  assert.ok(chamadas.every(c => c.auth === 'Bearer tk'));
});

test('services de versão e exportação: público no corpo, nunca o nome de quem revisou', async () => {
  const chamadas = capturar(204);
  await releases.aprovarVersao('REL/1', 'cliente');
  await releases.reprovarVersao('REL/1', 'cliente', 'Texto confuso');
  await releases.reabrirVersao('REL/1', 'cliente');
  capturar(201, { id: 'e1', conteudo: '# Título' });
  const exportado = await releases.exportarComunicado('REL/1', 'cliente');
  assert.deepEqual(chamadas.map(c => [c.method, c.url.replace(/^.*\/api\/v1/, ''), c.body]), [
    ['POST', '/releases/REL%2F1/aprovar', { publico: 'cliente' }],
    ['POST', '/releases/REL%2F1/reprovar', { publico: 'cliente', motivo: 'Texto confuso' }],
    ['POST', '/releases/REL%2F1/reabrir', { publico: 'cliente' }],
  ]);
  assert.equal(exportado.conteudo, '# Título');
});

test('409 da API chega com o detail para a UI mostrar', async () => {
  capturar(409, { detail: 'A versão cliente está aprovada — reabra antes de editar.' });
  await assert.rejects(releases.editarItem('X', 'i1', 'texto'), err => {
    assert.equal(err.status, 409);
    assert.equal(revisao.mensagemDeErro(err), 'A versão cliente está aprovada — reabra antes de editar.');
    return true;
  });
});
