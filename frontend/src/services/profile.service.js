import { profileMock } from '../data/profile.mock';

export async function fetchProfile() {
  // TODO: Substituir mock por chamada HTTP real ao backend FastAPI:
  // const response = await fetch(ENDPOINTS.PROFILE.ME, {
  //   headers: { Authorization: `Bearer ${localStorage.getItem('auth_token')}` },
  // });
  // if (!response.ok) throw new Error('Não foi possível carregar os dados do perfil.');
  // return response.json();

  return new Promise((resolve) => {
    setTimeout(() => {
      resolve(profileMock);
    }, 700);
  });
}
