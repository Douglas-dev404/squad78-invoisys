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
  PROCESS_MODAL_TITLE: 'Processar Release',
  PROCESS_MODAL_KEY_LABEL: 'Chave da Release (fixVersion do Jira)',
  PROCESS_MODAL_KEY_PLACEHOLDER: 'INV-123',
  PROCESS_MODAL_KEY_REQUIRED: 'Informe a chave da Release.',
  PROCESS_MODAL_SUBMIT: 'Processar',
  PROCESS_MODAL_SUBMITTING: 'Gerando comunicado…',
  PROCESS_MODAL_CANCEL: 'Cancelar',
  PROCESS_MODAL_WAIT: 'Isso pode levar até 2 minutos. Não feche esta janela.',
  PROCESS_MODAL_GENERIC_ERROR: 'Não foi possível processar a Release.',
};

export const MIN_PASSWORD_LENGTH = 6;
