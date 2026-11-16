// Popula um Jira Cloud de testes com Releases (fixVersions), histórias e subtarefas "Release Note"
// no formato que o InvoiSys lê. Uso e variáveis: scripts/jira-teste/README.md.
// Node 22+, sem dependências. Pode rodar mais de uma vez: o que já existe é reaproveitado.

const env = process.env;
const baseUrl = (env.Jira__BaseUrl ?? '').replace(/\/+$/, '');
const email = env.Jira__Email ?? '';
const token = env.Jira__ApiToken ?? '';
const projetoChave = (env.Jira__ProjetoChave || 'INV').toUpperCase();
const simulacao = process.argv.includes('--simular');

if (!baseUrl || !email || !token) {
  console.error('Defina Jira__BaseUrl, Jira__Email e Jira__ApiToken (veja scripts/jira-teste/README.md).');
  process.exit(1);
}

const autorizacao = `Basic ${Buffer.from(`${email}:${token}`).toString('base64')}`;

async function jira(metodo, caminho, corpo) {
  if (simulacao && metodo !== 'GET') {
    console.log(`  [simulação] ${metodo} ${caminho}`);
    return {};
  }
  const resposta = await fetch(`${baseUrl}${caminho}`, {
    method: metodo,
    headers: {
      Authorization: autorizacao,
      Accept: 'application/json',
      ...(corpo ? { 'Content-Type': 'application/json' } : {}),
    },
    body: corpo ? JSON.stringify(corpo) : undefined,
  });
  if (resposta.status === 204) return {};
  const texto = await resposta.text();
  const json = texto ? JSON.parse(texto) : {};
  if (!resposta.ok) {
    const erro = new Error(`${metodo} ${caminho} → ${resposta.status}: ${JSON.stringify(json.errors ?? json.errorMessages ?? json)}`);
    erro.status = resposta.status;
    throw erro;
  }
  return json;
}

// ---------- ADF (formato de texto rico do Jira) ----------
const texto = (t) => ({ type: 'text', text: t });
const paragrafo = (t) => ({ type: 'paragraph', content: [texto(t)] });
const lista = (itens) => ({
  type: 'bulletList',
  content: itens.map((t) => ({ type: 'listItem', content: [paragrafo(t)] })),
});
const tabela = (linhas) => ({
  type: 'table',
  content: linhas.map((celulas, i) => ({
    type: 'tableRow',
    content: celulas.map((c) => ({ type: i === 0 ? 'tableHeader' : 'tableCell', content: [paragrafo(c)] })),
  })),
});
const doc = (...blocos) => ({
  type: 'doc',
  version: 1,
  content: blocos.map((b) => (typeof b === 'string' ? paragrafo(b) : b)),
});

// ---------- Dados de teste ----------
const RELEASES = [
  {
    nome: '2026.10.0',
    descricao: 'Release pequena de outubro',
    dataLancamento: '2026-10-28',
    lancada: true,
    historias: [
      { tipo: 'bug', resumo: 'Corrigir arredondamento do ICMS-ST na NF-e', descricao: doc('O valor do ICMS-ST era arredondado antes da soma dos itens, gerando diferença de centavos e rejeição 532 da SEFAZ. Ajustado o cálculo em CalculoIcmsStService para arredondar só no total.') },
      { tipo: 'historia', resumo: 'Permitir reenvio de NFC-e em contingência offline', descricao: doc('Notas emitidas em contingência offline agora entram numa fila e são transmitidas automaticamente quando a conexão volta. Novo job ReenvioContingenciaJob a cada 5 minutos.'), releaseNote: 'Notas de consumidor (NFC-e) emitidas sem internet agora são enviadas sozinhas assim que a conexão volta, sem precisar reenviar uma a uma.' },
      { tipo: 'historia', resumo: 'Exibir chave de acesso no PDF do DANFE simplificado', descricao: doc('Inclusão da chave de acesso com 44 dígitos e QR Code no layout do DANFE simplificado (etiqueta).') },
    ],
  },
  {
    nome: '2026.11.0',
    descricao: 'Release de novembro (média)',
    dataLancamento: '2026-11-27',
    lancada: false,
    historias: [
      { tipo: 'historia', resumo: 'Emissão de NFC-e em lote', descricao: doc('Novo endpoint POST /nfce/lote aceita até 500 notas por requisição.', lista(['Validação de schema por nota', 'Retorno individual por nota no lote', 'Limite configurável por cliente'])), releaseNote: 'Agora é possível emitir até 500 notas fiscais de consumidor (NFC-e) de uma só vez, com o resultado de cada nota mostrado separadamente.' },
      { tipo: 'historia', resumo: 'Lote de NFC-e: acompanhar status no painel', descricao: doc('Tela de acompanhamento do lote com status por nota (autorizada, rejeitada, pendente), atualizada a cada 10 segundos.') },
      { tipo: 'bug', resumo: 'Corrigir alíquota de ICMS interestadual MG → SP', descricao: doc('A tabela de alíquotas interestaduais estava com 12% para MG → SP em produtos importados, quando o correto é 4% (Resolução 13/2012).', tabela([['Origem', 'Destino', 'Antes', 'Depois'], ['MG', 'SP', '12%', '4%'], ['MG', 'RJ', '12%', '4%']])) },
      { tipo: 'historia', resumo: 'Download em massa de XML de NF-e recebidas', descricao: doc('Filtro por período e CNPJ emitente; gera um ZIP com os XMLs e envia o link por e-mail quando passar de 1.000 notas.'), releaseNote: 'Você pode baixar de uma vez os XMLs das notas fiscais recebidas em um período. Para volumes grandes, o link do arquivo chega por e-mail.' },
      { tipo: 'historia', resumo: 'Ajuste de performance na consulta de manifestação do destinatário', descricao: doc('Índice novo em manifestacao(cnpj_destinatario, data_emissao). Consulta caiu de 8 s para 300 ms em clientes grandes.') },
      { tipo: 'historia', resumo: 'Atualizar biblioteca de assinatura digital', descricao: doc('Atualização interna da biblioteca de assinatura XML para a versão 4.2, sem mudança de comportamento para o usuário.') },
    ],
  },
  {
    nome: '2026.12.0',
    descricao: 'Release de dezembro (grande)',
    dataLancamento: '2026-12-18',
    lancada: false,
    historias: [
      { tipo: 'historia', resumo: 'Emissão de NFS-e no padrão nacional', descricao: doc('Integração com o emissor nacional da NFS-e (ADN), incluindo DPS, eventos de cancelamento e substituição.', lista(['Envio da DPS', 'Consulta de NFS-e por chave', 'Cancelamento e substituição'])), releaseNote: 'Começamos a emitir a nota fiscal de serviço (NFS-e) no padrão nacional, com cancelamento e substituição direto pelo sistema.' },
      { tipo: 'historia', resumo: 'NFS-e nacional: cadastro de municípios conveniados', descricao: doc('Tabela de municípios aderentes ao padrão nacional, atualizada a partir do portal da NFS-e.') },
      { tipo: 'historia', resumo: 'NFS-e nacional: tela de emissão', descricao: doc('Formulário de emissão com cálculo automático de ISS e retenções federais.') },
      { tipo: 'bug', resumo: 'Corrigir rejeição 225 em NF-e com mais de 990 itens', descricao: doc('Notas com mais de 990 itens eram rejeitadas por falha de schema no grupo det. Ajustada a paginação dos itens na montagem do XML.') },
      { tipo: 'bug', resumo: 'Data de saída aparecendo com fuso errado no DANFE', descricao: doc('dhSaiEnt era gravado em UTC e impresso sem conversão, aparecendo 3 horas adiantado.') },
      { tipo: 'historia', resumo: 'Alertas de certificado digital vencendo', descricao: doc('E-mail e aviso no painel 30, 15 e 5 dias antes do vencimento do certificado A1.'), releaseNote: 'O sistema agora avisa por e-mail e no painel quando o seu certificado digital estiver perto de vencer, com 30, 15 e 5 dias de antecedência.' },
      { tipo: 'historia', resumo: 'Relatório de notas rejeitadas por motivo', descricao: doc('Novo relatório agrupando rejeições da SEFAZ por código e mês, com exportação em CSV.') },
      { tipo: 'historia', resumo: 'Login com autenticação em dois fatores', descricao: doc('Segundo fator por aplicativo autenticador (TOTP), opcional por usuário e obrigatório para administradores.') },
      { tipo: 'historia', resumo: 'Migração do banco de dados para PostgreSQL 16', descricao: doc('Atualização da infraestrutura do banco de dados. Janela de manutenção de 30 minutos no dia 18/12, às 23h.') },
      { tipo: 'historia', resumo: 'Melhorar mensagens de erro na importação de planilha de produtos', descricao: doc('Mensagens passam a indicar a linha e a coluna do problema em vez de "erro ao importar".') },
    ],
  },
];

// ---------- Passos ----------
async function garantirProjeto() {
  try {
    const projeto = await jira('GET', `/rest/api/3/project/${projetoChave}`);
    console.log(`Projeto ${projetoChave} já existe (id ${projeto.id}).`);
    return projeto;
  } catch (erro) {
    if (erro.status !== 404) throw erro;
  }
  const eu = await jira('GET', '/rest/api/3/myself');
  console.log(`Criando projeto ${projetoChave} (Kanban, gerenciado pela empresa)…`);
  const criado = await jira('POST', '/rest/api/3/project', {
    key: projetoChave,
    name: 'InvoiSys Testes',
    projectTypeKey: 'software',
    projectTemplateKey: 'com.pyxis.greenhopper.jira:gh-simplified-kanban-classic',
    leadAccountId: eu.accountId,
    assigneeType: 'UNASSIGNED',
  });
  return simulacao ? { id: '0', key: projetoChave } : jira('GET', `/rest/api/3/project/${criado.key ?? projetoChave}`);
}

async function garantirTipoReleaseNote(projeto) {
  const tipos = await jira('GET', '/rest/api/3/issuetype');
  let tipo = tipos.find((t) => t.name === 'Release Note' && t.subtask);
  if (!tipo) {
    console.log('Criando tipo de subtarefa "Release Note"…');
    tipo = await jira('POST', '/rest/api/3/issuetype', {
      name: 'Release Note',
      description: 'Texto para o comunicado de Release (lido pelo InvoiSys).',
      type: 'subtask',
    });
  } else {
    console.log('Tipo de subtarefa "Release Note" já existe.');
  }

  const esquemas = await jira('GET', `/rest/api/3/issuetypescheme/project?projectId=${projeto.id}`);
  const esquema = esquemas.values?.[0]?.issueTypeScheme;
  if (esquema && !esquema.isDefault && tipo.id) {
    try {
      await jira('PUT', `/rest/api/3/issuetypescheme/${esquema.id}/issuetype`, { issueTypeIds: [tipo.id] });
      console.log(`"Release Note" adicionado ao esquema de tipos do projeto.`);
    } catch (erro) {
      if (erro.status !== 400) throw erro;
    }
  }
  return tipo;
}

async function tiposDoProjeto() {
  const meta = await jira('GET', `/rest/api/3/issue/createmeta/${projetoChave}/issuetypes?maxResults=50`);
  const tipos = meta.issueTypes ?? meta.values ?? [];
  const padrao = tipos.filter((t) => !t.subtask && !/epic|épico/i.test(t.name));
  const achar = (nomes) => padrao.find((t) => nomes.some((n) => t.name.toLowerCase() === n));
  const historia = achar(['story', 'história', 'historia']) ?? achar(['task', 'tarefa']) ?? padrao[0];
  const bug = achar(['bug']) ?? historia;
  if (!historia) throw new Error(`O projeto ${projetoChave} não tem tipo de item padrão (História/Tarefa).`);
  console.log(`Tipos usados: história="${historia.name}", bug="${bug.name}".`);
  return { historia, bug };
}

async function garantirVersoes(projeto) {
  const existentes = await jira('GET', `/rest/api/3/project/${projetoChave}/versions`);
  const porNome = new Map(existentes.map((v) => [v.name, v]));
  for (const release of RELEASES) {
    if (porNome.has(release.nome)) {
      console.log(`Release ${release.nome} já existe.`);
      continue;
    }
    console.log(`Criando Release ${release.nome}…`);
    const versao = await jira('POST', '/rest/api/3/version', {
      name: release.nome,
      description: release.descricao,
      projectId: Number(projeto.id),
      releaseDate: release.dataLancamento,
      released: release.lancada,
    });
    porNome.set(release.nome, versao);
  }
  return porNome;
}

async function quantasIssues(versao) {
  const jql = encodeURIComponent(`project = ${projetoChave} AND fixVersion = "${versao}"`);
  const r = await jira('GET', `/rest/api/3/search/jql?jql=${jql}&maxResults=1&fields=summary`);
  return r.issues?.length ?? 0;
}

async function criarHistorias(tipos, tipoReleaseNote) {
  for (const release of RELEASES) {
    if ((await quantasIssues(release.nome)) > 0) {
      console.log(`Release ${release.nome} já tem histórias; pulando.`);
      continue;
    }
    console.log(`Criando ${release.historias.length} histórias em ${release.nome}…`);
    for (const h of release.historias) {
      const criada = await jira('POST', '/rest/api/3/issue', {
        fields: {
          project: { key: projetoChave },
          summary: h.resumo,
          issuetype: { id: (h.tipo === 'bug' ? tipos.bug : tipos.historia).id },
          description: h.descricao,
          labels: ['seed-invoisys'],
          fixVersions: [{ name: release.nome }],
        },
      });
      if (h.releaseNote && tipoReleaseNote?.id) {
        await jira('POST', '/rest/api/3/issue', {
          fields: {
            project: { key: projetoChave },
            parent: { key: criada.key },
            summary: `Release Note — ${h.resumo}`,
            issuetype: { id: tipoReleaseNote.id },
            description: doc(h.releaseNote),
          },
        });
      }
      console.log(`  ${criada.key ?? '(simulação)'} ${h.resumo}${h.releaseNote ? ' + Release Note' : ''}`);
    }
  }
}

try {
  console.log(`Jira: ${baseUrl} · projeto ${projetoChave}${simulacao ? ' · SIMULAÇÃO (nada é gravado)' : ''}`);
  const projeto = await garantirProjeto();
  const tipoReleaseNote = await garantirTipoReleaseNote(projeto);
  const tipos = await tiposDoProjeto();
  await garantirVersoes(projeto);
  await criarHistorias(tipos, tipoReleaseNote);
  console.log(`\nPronto. Configure a API com Jira__ProjetoChave=${projetoChave} e abra a tela Releases.`);
} catch (erro) {
  console.error(`\nFalhou: ${erro.message}`);
  if (/fixVersions/.test(erro.message)) {
    console.error('O campo "Fix versions" não está na tela de criação do projeto. Em Configurações do projeto → Telas, adicione "Fix versions" e rode de novo.');
  }
  if (erro.status === 401 || erro.status === 403) {
    console.error('Confira o e-mail e o API token, e se a conta é administradora do site (criar projeto e tipo de item exige).');
  }
  process.exit(1);
}
