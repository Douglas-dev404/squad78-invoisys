// Regras da tela de Integrações (Jira e IA) sem React.

export const PROVEDORES_IA = [
  { value: 'OpenAi', label: 'OpenAI' },
  { value: 'OpenRouter', label: 'OpenRouter' },
];

const vazio = (valor) => !valor || !valor.trim();

/** Formulário do Jira a partir do que a API devolveu. O token nunca vem: o campo começa vazio. */
export const formJiraInicial = (jira) => ({
  baseUrl: jira?.baseUrl ?? '',
  email: jira?.email ?? '',
  projeto: jira?.projeto ?? '',
  apiToken: '',
});

/** Corpo do PUT /integracoes/jira. Token em branco = manter o atual (a API ignora campos vazios). */
export const payloadJira = (form) => ({
  baseUrl: form.baseUrl.trim(),
  email: form.email.trim(),
  projeto: form.projeto.trim(),
  apiToken: vazio(form.apiToken) ? undefined : form.apiToken.trim(),
});

export const jiraAlterado = (form, jira) =>
  form.baseUrl.trim() !== (jira?.baseUrl ?? '')
  || form.email.trim() !== (jira?.email ?? '')
  || form.projeto.trim() !== (jira?.projeto ?? '')
  || !vazio(form.apiToken);

/** O provedor escolhido guarda o próprio modelo e a própria chave. */
export const dadosDoProvedor = (ia, provedor) => (provedor === 'OpenAi' ? ia?.openAi : ia?.openRouter);

export const formIaInicial = (ia) => ({
  provedor: ia?.provedor ?? 'OpenAi',
  modelo: dadosDoProvedor(ia, ia?.provedor ?? 'OpenAi')?.modelo ?? '',
  apiKey: '',
});

export const payloadIa = (form) => ({
  provedor: form.provedor,
  modelo: form.modelo.trim(),
  apiKey: vazio(form.apiKey) ? undefined : form.apiKey.trim(),
});

export const iaAlterada = (form, ia) =>
  form.provedor !== (ia?.provedor ?? 'OpenAi')
  || form.modelo.trim() !== (dadosDoProvedor(ia, form.provedor)?.modelo ?? '')
  || !vazio(form.apiKey);

/** `errors` da API ({ baseUrl: ['...'] }) → { baseUrl: '...' }, aceitando PascalCase. */
export function errosPorCampo(errors = {}) {
  const porCampo = {};
  for (const [campo, mensagens] of Object.entries(errors)) {
    const chave = campo.charAt(0).toLowerCase() + campo.slice(1);
    porCampo[chave] = Array.isArray(mensagens) ? mensagens.join(' ') : String(mensagens);
  }
  return porCampo;
}
