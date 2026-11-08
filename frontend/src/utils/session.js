const SESSION_KEY = 'invoisys.session';

export function getSession() {
  try {
    const session = JSON.parse(localStorage.getItem(SESSION_KEY));
    if (!session) return null;
    const expiresAt = Date.parse(session.expiraEm);
    if (!session.token || !session.usuario || !Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
      clearSession();
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

export function saveSession({ token, expiraEm, usuario }) {
  try {
    localStorage.setItem(SESSION_KEY, JSON.stringify({ token, expiraEm, usuario }));
  } catch {
    return;
  }
}

export function clearSession() {
  try {
    localStorage.removeItem(SESSION_KEY);
  } catch {
    return;
  }
}
