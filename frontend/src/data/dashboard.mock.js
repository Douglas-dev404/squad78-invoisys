export const dashboardKPIMock = {
  formalizedCards: {
    value: 142,
    trend: '+12%',
    trendLabel: 'esta semana',
    icon: 'check_circle',
  },
  inReview: {
    value: 38,
    trend: '5 urgentes',
    trendLabel: '',
    icon: 'pending_actions',
  },
  sentToClient: {
    value: 89,
    trend: 'Todos no prazo',
    trendLabel: '',
    icon: 'send',
  },
};

export const dashboardChartMock = {
  weekDays: ['Seg', 'Ter', 'Qua', 'Qui', 'Sex'],
  data: [
    { day: 'Seg', formalized: 50, inReview: 30 },
    { day: 'Ter', formalized: 70, inReview: 40 },
    { day: 'Qua', formalized: 85, inReview: 20 },
    { day: 'Qui', formalized: 60, inReview: 50 },
    { day: 'Sex', formalized: 40, inReview: 10 },
  ],
};

export const dashboardCardsMock = [
  {
    id: 'INV-4029',
    jiraId: 'INV-4029',
    title: 'Atualização do Gateway de Pagamento',
    responsible: 'Ana Silva',
    status: 'Em Revisão',
    statusColor: 'secondary-container',
    date: 'Hoje, 14:30',
    createdAt: new Date(),
  },
  {
    id: 'INV-4025',
    jiraId: 'INV-4025',
    title: 'Correção de Bug na Exportação PDF',
    responsible: 'Carlos Souza',
    status: 'Formalizado',
    statusColor: 'primary',
    date: 'Ontem, 16:45',
    createdAt: new Date(Date.now() - 86400000),
  },
  {
    id: 'INV-4018',
    jiraId: 'INV-4018',
    title: 'Implementação de SSO via Azure AD',
    responsible: 'Marcos Lima',
    status: 'Enviado Cliente',
    statusColor: 'tertiary-container',
    date: '02/Nov, 09:15',
    createdAt: new Date(Date.now() - 172800000),
  },
  {
    id: 'INV-4015',
    jiraId: 'INV-4015',
    title: 'Refatoração do Módulo de Relatórios',
    responsible: 'Ana Silva',
    status: 'Formalizado',
    statusColor: 'primary',
    date: '01/Nov, 11:20',
    createdAt: new Date(Date.now() - 259200000),
  },
];
