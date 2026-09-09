/**
 * Mock de informações institucionais e destaques visuais da plataforma Invoisys.
 * Permite simular retornos de API dinâmicos para a seção lateral (Hero) e cabeçalhos.
 */

export const MOCK_BRAND_INFO = {
  companyName: 'Invoisys',
  logoIcon: 'energy_savings_leaf',
  loginTitle: 'Bem-vindo de volta',
  loginSubtitle: 'Insira suas credenciais para acessar a plataforma de formalização.',
  forgotPasswordText: 'Esqueci minha senha',
  forgotPasswordUrl: '#',
};

export const MOCK_HERO_HIGHLIGHTS = [
  {
    id: 'hl_01',
    title: 'Gestão automatizada de tasks',
    description: 'Simplifique sua produtividade.',
    icon: 'task_alt',
    createdAt: '2026-02-01T08:00:00.000Z',
  },
  {
    id: 'hl_02',
    title: 'Release Notes com Inteligência Artificial',
    description: 'Converta histórias do Jira em comunicados claros em poucos segundos.',
    icon: 'auto_awesome',
    createdAt: '2026-02-10T14:20:00.000Z',
  },
  {
    id: 'hl_03',
    title: 'Governança e Aprovação Humana',
    description: 'Revisão obrigatória e controle granular antes de cada publicação oficial.',
    icon: 'verified_user',
    createdAt: '2026-02-15T11:45:00.000Z',
  },
];
