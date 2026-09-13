import { usersMock } from '../data/users.mock';

let mockDatabase = usersMock.map((user) => ({ ...user }));

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
