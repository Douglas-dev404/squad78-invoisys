export const ROLE_LABELS = {
  admin: 'Admin',
  reviewer: 'Revisor',
  viewer: 'Visualizador',
};

export const usersMock = [
  {
    id: 'usr_1',
    name: 'Ana Silva',
    email: 'ana.silva@invoisys.com',
    avatarUrl:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuDgKUw1oNphtd-FUXn__WGtONn_vsV_Z49hn5HE_jEgCrHP_mxxuBpE8rmJ9OIjlIrQH-QvzM0FMbypxybMkhLzBnE3LVSiVBQ2vdeIS2WXTAp3-9tNuJBirptzUTRQw6MU8APPjpbviig6-qYYlJhXVxz1NzsvTsmwe0KX-zHHQUJdhjXoB85RNX4TC4B5U4X5FX5bJSTLi0K3sST62vnChDlnS3zsgo3KEFXZXGPkJIadQdJ4DcgI8A',
    role: 'admin',
    roleLabel: ROLE_LABELS.admin,
    status: 'active',
    permissions: { admin: true, reviewer: true, viewer: false },
    activity: [
      {
        id: 'act_1',
        description: 'Aprovou formalização #TSK-092',
        date: 'Hoje, 14:32',
      },
      { id: 'act_2', description: 'Login no sistema', date: 'Hoje, 08:15' },
      {
        id: 'act_3',
        description: 'Alterou configurações de perfil',
        date: 'Ontem, 16:45',
      },
    ],
  },
  {
    id: 'usr_2',
    name: 'Carlos Eduardo',
    email: 'carlos.ed@invoisys.com',
    avatarUrl:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuBYVfAVInmklAz9Nqd7A6nXLpan3iu8zTVz_xXWSM3zlw3Md01lGm_D1634BqRgi2Bud4Kx0zGDEmIbxJQg8vhaajn54vpLsis8mkQ_VIVk8nMeGKf-Wgmtk-T8eY21exCZcY3KgqAsTjC2iyPpelhDIhctADoOd_PtmLU_PqCS1AejSdi0_m6dLZVwHwivwkkCWHFS6bkq4om4VCk7YXek6I7qiNA3vf72NY-1oB9bnVL-nmt4fW9qVA',
    role: 'reviewer',
    roleLabel: ROLE_LABELS.reviewer,
    status: 'active',
    permissions: { admin: false, reviewer: true, viewer: true },
    activity: [
      {
        id: 'act_4',
        description: 'Revisou formalização #TSK-088',
        date: 'Hoje, 11:20',
      },
      { id: 'act_5', description: 'Login no sistema', date: 'Hoje, 09:02' },
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
        date: '12/08, 10:00',
      },
      { id: 'act_7', description: 'Login no sistema', date: '10/08, 08:40' },
    ],
  },
];
