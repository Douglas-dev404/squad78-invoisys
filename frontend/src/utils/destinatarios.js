import { PUBLICO_LABEL } from './revisao.js';
import { isValidEmail } from './validators.js';

export const NOME_MAX = 200;

export const DESTINATARIOS_TEXTOS = {
  TITULO: 'Destinatários',
  SUBTITULO: 'E-mails que recebem os comunicados aprovados. Cada e-mail pertence a um público.',
  NOVO: 'Novo destinatário',
  VAZIO: 'Nenhum destinatário cadastrado.',
  VAZIO_FILTRO: 'Nenhum destinatário para este filtro.',
  LIMPAR: 'Limpar filtros',
  ERRO_LISTA: 'Não foi possível carregar os destinatários.',
  NOME_OBRIGATORIO: 'Nome é obrigatório.',
  NOME_LONGO: `Nome deve ter no máximo ${NOME_MAX} caracteres.`,
  EMAIL_OBRIGATORIO: 'E-mail é obrigatório.',
  EMAIL_INVALIDO: 'E-mail inválido.',
  PUBLICO_OBRIGATORIO: 'Escolha o público.',
  CONFIRMAR_DESATIVAR: (nome) =>
    `${nome} deixa de aparecer na lista de envio. Os envios já feitos continuam no histórico.`,
};

/** Filtros da tela vindos da URL (`?publico=cliente&inativos=1`); valor desconhecido vira "todos". */
export function lerFiltrosDestinatarios(params) {
  const publico = (params.get('publico') ?? '').toLowerCase();
  return {
    publico: Object.hasOwn(PUBLICO_LABEL, publico) ? publico : '',
    inativos: params.get('inativos') === '1',
  };
}

/** Mesmas regras do validator da API, para não chamar a API com o que ela recusaria com 422. */
export function validarDestinatario({ nome, email, publico }) {
  const erros = {};
  const nomeLimpo = (nome ?? '').trim();
  if (!nomeLimpo) erros.nome = DESTINATARIOS_TEXTOS.NOME_OBRIGATORIO;
  else if (nomeLimpo.length > NOME_MAX) erros.nome = DESTINATARIOS_TEXTOS.NOME_LONGO;

  if (!(email ?? '').trim()) erros.email = DESTINATARIOS_TEXTOS.EMAIL_OBRIGATORIO;
  else if (!isValidEmail(email)) erros.email = DESTINATARIOS_TEXTOS.EMAIL_INVALIDO;

  if (!Object.hasOwn(PUBLICO_LABEL, publico ?? '')) erros.publico = DESTINATARIOS_TEXTOS.PUBLICO_OBRIGATORIO;

  return { valido: Object.keys(erros).length === 0, erros };
}

/** Separa o resultado do envio para a tela e devolve os ids que falharam, para "tentar de novo". */
export function resumirEnvio(envios = []) {
  const enviados = envios.filter((envio) => envio.status === 'enviado');
  const falhas = envios.filter((envio) => envio.status !== 'enviado');
  return { enviados, falhas, idsComFalha: falhas.map((envio) => envio.destinatarioId) };
}

export const STATUS_ENVIO = {
  enviado: { label: 'Enviado', icone: 'check_circle', className: 'text-green-700' },
  falhou: { label: 'Falhou', icone: 'cancel', className: 'text-error' },
};
