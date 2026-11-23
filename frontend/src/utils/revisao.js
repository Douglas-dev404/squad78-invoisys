// Regras da revisão humana (#70, #71, #72) sem React, para a tela de detalhe (#69) só montar.

export const PUBLICO_LABEL = {
  cliente: 'Cliente',
  comercial: 'Comercial',
  suporte: 'Suporte',
  interno: 'Interno',
};

export const VERSAO_STATUS = {
  aguardando_revisao: { label: 'Aguardando revisão', className: 'bg-amber-100 text-amber-800' },
  aprovado: { label: 'Aprovada', className: 'bg-green-100 text-green-800' },
  reprovado: { label: 'Reprovada', className: 'bg-error-container text-on-error-container' },
};

export const REVISAO_TEXTOS = {
  TEXTO_VAZIO: 'O texto não pode ficar vazio.',
  APROVAR_SEM_ITEM: 'Inclua pelo menos um item.',
  EDICAO_BLOQUEADA_APROVADA: 'Versão aprovada. Reabra para editar.',
  EDICAO_BLOQUEADA_REPROVADA: 'Versão reprovada. Reprocesse com a IA para gerar uma nova versão.',
  MOTIVO_OBRIGATORIO: 'Informe o motivo da reprovação.',
  CONFIRMAR_APROVAR:
    'Ao aprovar, o texto desta versão não poderá ser editado nem reprocessado até você reabrir. Confirmar?',
  CONFIRMAR_REABRIR: 'A versão volta para revisão e o texto pode ser editado de novo. Confirmar?',
  CONFIRMAR_REPROCESSAR:
    'A IA vai gerar o comunicado de novo. Isso substitui o texto gerado, inclusive suas edições.',
  ERRO_GENERICO: 'Não foi possível concluir a ação.',
};

/**
 * O que a versão permite agora. Espelha o domínio para a UI nunca oferecer o que a API recusaria com 409.
 * @param {{ status: string, itens?: Array<{ incluido: boolean }> }} versao
 * @param {string} [statusRelease] status do pipeline; "falhou" bloqueia aprovar
 */
export function acoesDaVersao(versao, statusRelease) {
  const status = versao?.status;
  const aguardando = status === 'aguardando_revisao';
  const temItemIncluido = (versao?.itens ?? []).some((item) => item.incluido);
  const pipelineOk = statusRelease !== 'falhou';

  return {
    aprovar: aguardando && pipelineOk,
    aprovarDesabilitadoMotivo: aguardando && !temItemIncluido ? REVISAO_TEXTOS.APROVAR_SEM_ITEM : null,
    reprovar: aguardando,
    reabrir: status === 'aprovado',
    reprocessar: aguardando || status === 'reprovado',
    exportar: status === 'aprovado',
    editarItens: aguardando,
    edicaoBloqueadaMotivo:
      status === 'aprovado'
        ? REVISAO_TEXTOS.EDICAO_BLOQUEADA_APROVADA
        : status === 'reprovado'
          ? REVISAO_TEXTOS.EDICAO_BLOQUEADA_REPROVADA
          : null,
  };
}

/** Texto do item que vale: o editado à mão, se houver; senão o da IA. */
export const textoFinalDoItem = (item) => item?.textoFinal ?? item?.textoEditadoManualmente ?? item?.texto ?? '';

export const textoItemValido = (texto) => typeof texto === 'string' && texto.trim().length > 0;

/** Troca, numa lista de itens, o item que voltou da API (mesmo id), sem refazer o GET da Release. */
export const substituirItem = (itens, atualizado) =>
  itens.map((item) => (item.id === atualizado.id ? { ...item, ...atualizado } : item));

export const FORMATOS_EXPORTACAO = {
  markdown: { label: 'Markdown', extensao: 'md', tipo: 'text/markdown;charset=utf-8' },
  html: { label: 'HTML', extensao: 'html', tipo: 'text/html;charset=utf-8' },
};

/** Ex.: ("INV-123", "cliente", "html") → "INV-123-cliente.html". Tira caracteres que quebram nome de arquivo. */
export function nomeArquivoExportacao(chave, publico, formato = 'markdown') {
  const limpar = (valor) => String(valor ?? '').trim().replace(/[/\\:*?"<>|\s]+/g, '-');
  const extensao = FORMATOS_EXPORTACAO[formato]?.extensao ?? 'txt';
  return `${limpar(chave)}-${limpar(publico)}.${extensao}`;
}

export const mensagemDeErro = (erro, padrao = REVISAO_TEXTOS.ERRO_GENERICO) =>
  erro?.detail || erro?.message || padrao;

/** Primeira mensagem de `errors.<campo>` de um 422 (a API devolve arrays). */
export function erroDoCampo(erro, campo) {
  const valor = erro?.errors?.[campo] ?? erro?.errors?.[campo[0].toUpperCase() + campo.slice(1)];
  return Array.isArray(valor) ? valor.join(' ') : valor ?? null;
}
