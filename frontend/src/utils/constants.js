/**
 * Constantes da interface gráfica e configurações visuais.
 */

export const BRAND_NAME = 'Invoisys';

export const BRAND_DEFAULT_ICON = 'energy_savings_leaf';

export const BRAND_LOGO_URL =
  'https://lh3.googleusercontent.com/aida/AEtjO1V_Kz1B-3aJ7QsfypFylkuKC1jKnYdKu5_nE1HHcuPFXyRTJVAcdUtmDLCtQyzck0j6T8L4oXVr4c4CnlaHvhNeLkeKGqacKPkvRUmCWJhOkRfS6JZdT289QuxySzdLMX0snc9rutkX40jokCjC6zfcgRRRqM3pLyAEUn4pn-BND4lA1TwaReJTFNWOdDigO40QfvpJ8EpZyVtZ0_4Ce60JCPXxoFiO09ViucsLTHCkLWo1ghO7ki5T6-hE';

export const UI_STRINGS = {
  // Login
  LOGIN_TITLE: 'Bem-vindo de volta',
  LOGIN_SUBTITLE: 'Insira suas credenciais para acessar a plataforma de formalização.',
  EMAIL_LABEL: 'E-mail Corporativo',
  EMAIL_PLACEHOLDER: 'nome@empresa.com.br',
  PASSWORD_LABEL: 'Senha',
  PASSWORD_PLACEHOLDER: '••••••••',
  SUBMIT_BUTTON_LABEL: 'Entrar',
  SUBMITTING_BUTTON_LABEL: 'Entrando...',
  FORGOT_PASSWORD_LABEL: 'Esqueci minha senha',
  EMPTY_HIGHLIGHTS_TITLE: 'Nenhum destaque disponível no momento',
  EMPTY_HIGHLIGHTS_SUBTITLE: 'Novas informações serão exibidas em breve.',
  ERROR_RETRY_BUTTON: 'Tentar novamente',

  // Recuperação de Senha
  RECOVERY_TITLE: 'Recuperar Senha',
  RECOVERY_SUBTITLE: 'Insira seu e-mail para receber as instruções de redefinição.',
  RECOVERY_EMAIL_LABEL: 'E-mail corporativo',
  RECOVERY_EMAIL_PLACEHOLDER: 'nome@empresa.com',
  RECOVERY_SUBMIT_LABEL: 'Enviar link de recuperação',
  RECOVERY_SUBMITTING_LABEL: 'Enviando instruções...',
  BACK_TO_LOGIN_LABEL: 'Voltar para o Login',
  COPYRIGHT_TEXT: '© 2024 Invoisys. Todos os direitos reservados.',

  // Confirmação de envio de e-mail
  EMAIL_SENT_TITLE: 'E-mail Enviado',
  EMAIL_SENT_SUBTITLE: 'Enviamos um link de recuperação para o seu e-mail corporativo. Verifique sua caixa de entrada e spam.',
  DIDNT_RECEIVE_EMAIL: 'Não recebeu o e-mail?',
  RESEND_LINK_LABEL: 'Reenviar link',
  RESENDING_LINK_LABEL: 'Reenviando...',
  RESEND_SUCCESS_MESSAGE: 'Novo link enviado! Verifique sua caixa de entrada.',

    // Dashboard
  DASHBOARD_LOAD_ERROR: 'Não foi possível carregar o Dashboard.',
  DASHBOARD_EMPTY: 'Processe sua primeira Release',
  DASHBOARD_EMPTY_HINT: 'Os indicadores aparecem assim que a primeira Release for processada.',
  DASHBOARD_EMPTY_BUTTON: 'Ir para Releases',
  DASHBOARD_QUALITY_TITLE: 'Qualidade da IA',

    // Releases
  RELEASES_TITLE: 'Releases',
  RELEASES_PROCESS_BUTTON: 'Processar Release',
  RELEASES_LOADING: 'Carregando Releases…',
  RELEASES_EMPTY: 'Nenhuma Release ainda. Clique em Processar Release.',
  RELEASES_LOAD_ERROR: 'Não foi possível carregar as Releases.',
  RELEASES_RETRY: 'Tentar de novo',
  RELEASES_COL_KEY: 'Chave',
  RELEASES_COL_STATUS: 'Status',
  RELEASES_COL_CREATED: 'Criada em',
  RELEASE_DETALHE_VOLTAR: '← Releases',
  RELEASE_DETALHE_CARREGANDO: 'Carregando Release…',
  RELEASE_DETALHE_NAO_ENCONTRADA: 'Release não encontrada',
  RELEASE_DETALHE_IR_PARA_LISTA: 'Voltar para a lista de Releases',
  RELEASE_DETALHE_ERRO: 'Não foi possível carregar a Release.',
  RELEASE_DETALHE_SEM_VERSOES: 'Nenhum comunicado gerado',
  RELEASE_DETALHE_ABAS: 'Comunicado por público',
  RELEASE_DETALHE_EDITADO: 'Editado',
  RELEASE_DETALHE_EXCLUIDO: 'Excluído',
  RELEASE_DETALHE_ORIGENS: 'Histórias de origem do item',
  RELEASE_DETALHE_HISTORIAS: 'Histórias de origem',
  RELEASE_DETALHE_SEM_HISTORIAS: 'Nenhuma história registrada.',
  RELEASE_DETALHE_RN_DEDICADA: 'Tem Release Note dedicada',
  RELEASE_DETALHE_EXECUCOES: 'Execuções',
  RELEASE_DETALHE_SEM_EXECUCOES: 'Nenhuma execução registrada.',
  RELEASE_DETALHE_EM_ANDAMENTO: 'em andamento',
  RELEASE_DETALHE_REPROVADA: 'Motivo da reprovação',
  PROCESS_MODAL_TITLE: 'Processar Release',
  PROCESS_MODAL_KEY_LABEL: 'Release do Jira',
  PROCESS_MODAL_KEY_PLACEHOLDER: 'Selecione uma Release',
  PROCESS_MODAL_KEY_REQUIRED: 'Selecione uma Release.',
  PROCESS_MODAL_VERSIONS_LOADING: 'Carregando Releases do Jira…',
  PROCESS_MODAL_VERSIONS_ERROR: 'Não foi possível carregar as Releases do Jira.',
  PROCESS_MODAL_VERSIONS_EMPTY: 'O projeto Jira não tem Releases disponíveis.',
  PROCESS_MODAL_STORIES_LOADING: 'Carregando histórias desta Release…',
  PROCESS_MODAL_STORIES_ERROR: 'Não foi possível carregar as histórias desta Release.',
  PROCESS_MODAL_STORIES_EMPTY: 'Esta Release não tem histórias disponíveis no Jira.',
  PROCESS_MODAL_STORIES_LABEL: 'Histórias da versão',
  PROCESS_MODAL_STORIES_SELECT_ALL: 'Selecionar todas',
  PROCESS_MODAL_STORIES_CLEAR_ALL: 'Limpar seleção',
  PROCESS_MODAL_STORIES_SELECTED: 'selecionadas',
  PROCESS_MODAL_STORIES_REQUIRED: 'Selecione ao menos uma história para processar.',
  PROCESS_MODAL_SUBMIT: 'Processar',
  PROCESS_MODAL_SUBMITTING: 'Gerando comunicado…',
  PROCESS_MODAL_CANCEL: 'Cancelar',
  PROCESS_MODAL_WAIT: 'Isso pode levar até 2 minutos. Não feche esta janela.',
  PROCESS_MODAL_GENERIC_ERROR: 'Não foi possível processar a Release.',
  JIRA_VERSOES_ERRO: 'Não foi possível carregar as versões do Jira.',
  PROCESS_MODAL_LIST_LABEL: 'Versões do Jira',
  PROCESS_MODAL_LIST_LOADING: 'Carregando versões do Jira…',
  PROCESS_MODAL_LIST_EMPTY: 'Nenhuma versão encontrada no Jira. Você pode digitar o nome manualmente.',
  PROCESS_MODAL_SEARCH_LABEL: 'Buscar versão',
  PROCESS_MODAL_SEARCH_PLACEHOLDER: 'Ex.: 2026.10',
  PROCESS_MODAL_SEARCH_EMPTY: 'Nenhuma versão corresponde à busca.',
  PROCESS_MODAL_RELEASED: 'Lançada',
  PROCESS_MODAL_MANUAL: 'Digitar manualmente',
  PROCESS_MODAL_BACK_TO_LIST: 'Escolher da lista',
  PROCESS_MODAL_SEE_DETAILS: 'Ver detalhes',
  PROCESS_MODAL_ALREADY_PROCESSED: 'Esta versão já foi processada. Processar de novo substitui o comunicado gerado.',
  PROCESS_MODAL_APPROVED_WARNING: 'Esta versão já está aprovada. Reabra a revisão na tela de detalhe antes de processar de novo.',
  PROCESS_MODAL_PREVIEW_TITLE: 'Prévia das histórias',
  PROCESS_MODAL_PREVIEW_LOADING: 'Carregando as histórias da versão…',
  PROCESS_MODAL_PREVIEW_ERROR: 'Não foi possível carregar as histórias da versão.',
  PROCESS_MODAL_NO_STORIES: 'Esta versão não tem histórias. Não há o que processar.',
};

export const MIN_PASSWORD_LENGTH = 6;

export const ROLE_LABELS = {
  admin: 'Administrador',
  release_manager: 'Gestor de Release',
};
