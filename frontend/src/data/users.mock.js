export const ROLE_LABELS = {
  admin: 'Administrador',
  reviewer: 'Revisor',
  viewer: 'Visualizador',
};

export const usersMock = [
  {
    id: 'usr_1',
    name: 'Ana Silva',
    initials: 'AS',
    email: 'ana.silva@invoisys.com',
    avatarUrl: null,
    role: 'admin',
    roleLabel: ROLE_LABELS.admin,
    status: 'active',
    permissions: { admin: true, reviewer: true, viewer: false },
    activity: [
      {
        id: 'act_1',
        description: 'Aprovou formalização #TSK-092',
        date: '2026-11-05T14:32:00-03:00',
      },
      { id: 'act_2', description: 'Login no sistema', date: '2026-11-05T08:15:00-03:00' },
      {
        id: 'act_3',
        description: 'Alterou configurações de perfil',
        date: '2026-11-04T16:45:00-03:00',
      },
    ],
  },
  {
    id: 'usr_2',
    name: 'Carlos Eduardo',
    initials: 'CE',
    email: 'carlos.ed@invoisys.com',
    avatarUrl: null,
    role: 'reviewer',
    roleLabel: ROLE_LABELS.reviewer,
    status: 'active',
    permissions: { admin: false, reviewer: true, viewer: true },
    activity: [
      {
        id: 'act_4',
        description: 'Revisou formalização #TSK-088',
        date: '2026-11-05T11:20:00-03:00',
      },
      { id: 'act_5', description: 'Login no sistema', date: '2026-11-05T09:02:00-03:00' },
    ],
  },
  {
    id: 'usr_3',
    name: 'Mariana Rios',
    email: 'mariana.r@invoisys.com',
    avatarUrl: null,
    initials: 'MR',
    role: 'viewer',
    roleLabel: ROLE_LABELS.viewer,
    status: 'inactive',
    permissions: { admin: false, reviewer: false, viewer: true },
    activity: [
      {
        id: 'act_6',
        description: 'Conta desativada por administrador',
        date: '2026-08-12T10:00:00-03:00',
      },
      { id: 'act_7', description: 'Login no sistema', date: '2026-08-10T08:40:00-03:00' },
    ],
  },
];
