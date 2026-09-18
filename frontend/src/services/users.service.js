import { usersMock, ROLE_LABELS } from '../data/users.mock';

let mockDatabase = usersMock.map((user) => ({ ...user }));
let nextUserId = mockDatabase.length + 1;

const ROLE_BASE_PERMISSIONS = {
  admin: { admin: true, reviewer: true, viewer: true },
  reviewer: { admin: false, reviewer: true, viewer: true },
  viewer: { admin: false, reviewer: false, viewer: true },
};

function getInitials(fullName) {
  return fullName
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() || '')
    .join('');
}

export async function fetchUsers() {
  // TODO: Substituir mock por chamada HTTP real ao backend FastAPI:
  // const response = await fetch(ENDPOINTS.USERS.LIST);
  // if (!response.ok) throw new Error('Não foi possível carregar os usuários.');
  // return response.json();

  return new Promise((resolve) => {
    setTimeout(() => {
      resolve(mockDatabase.map((user) => ({ ...user })));
    }, 700);
  });
}

export async function updateUserPermissions(userId, permissions) {
  // TODO: Substituir mock por chamada HTTP real PATCH ao backend FastAPI:
  // const response = await fetch(ENDPOINTS.USERS.PERMISSIONS(userId), {
  //   method: 'PATCH',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify({ permissions }),
  // });
  // if (!response.ok) throw new Error('Não foi possível salvar as permissões.');
  // return response.json();

  return new Promise((resolve, reject) => {
    setTimeout(() => {
      const user = mockDatabase.find((u) => u.id === userId);
      if (!user) {
        reject(new Error('Usuário não encontrado.'));
        return;
      }
      user.permissions = { ...permissions };
      resolve({ ...user });
    }, 600);
  });
}

export async function createUser({ fullName, email, role, extraPermissions }) {
  // TODO: Substituir mock por chamada HTTP real POST ao backend FastAPI:
  // const response = await fetch(ENDPOINTS.USERS.CREATE, {
  //   method: 'POST',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify({ fullName, email, role, extraPermissions }),
  // });
  // if (!response.ok) {
  //   const errorData = await response.json().catch(() => ({}));
  //   throw new Error(errorData.detail || 'Não foi possível cadastrar o usuário.');
  // }
  // return response.json();

  return new Promise((resolve, reject) => {
    setTimeout(() => {
      const emailAlreadyExists = mockDatabase.some(
        (user) => user.email.toLowerCase() === email.toLowerCase()
      );
      if (emailAlreadyExists) {
        reject(new Error('Já existe um usuário cadastrado com este e-mail.'));
        return;
      }

      const id = `usr_${nextUserId++}`;
      const newUser = {
        id,
        name: fullName.trim(),
        email: email.trim(),
        avatarUrl: null,
        initials: getInitials(fullName),
        role,
        roleLabel: ROLE_LABELS[role] || role,
        status: 'active',
        permissions: ROLE_BASE_PERMISSIONS[role] || {
          admin: false,
          reviewer: false,
          viewer: false,
        },
        extraPermissions: extraPermissions || {},
        activity: [
          {
            id: `act_${id}_created`,
            description: 'Usuário cadastrado no sistema',
            date: 'Agora',
          },
        ],
      };

      mockDatabase.push(newUser);
      resolve({ ...newUser });
    }, 800);
  });
}

export async function toggleUserStatus(userId) {
  // TODO: Substituir mock por chamada HTTP real PATCH ao backend FastAPI:
  // const response = await fetch(ENDPOINTS.USERS.STATUS(userId), {
  //   method: 'PATCH',
  //   headers: { 'Content-Type': 'application/json' },
  //   body: JSON.stringify({ status: newStatus }),
  // });
  // if (!response.ok) throw new Error('Não foi possível atualizar o status do usuário.');
  // return response.json();

  return new Promise((resolve, reject) => {
    setTimeout(() => {
      const user = mockDatabase.find((u) => u.id === userId);
      if (!user) {
        reject(new Error('Usuário não encontrado.'));
        return;
      }
      user.status = user.status === 'active' ? 'inactive' : 'active';
      resolve({ ...user });
    }, 600);
  });
}
