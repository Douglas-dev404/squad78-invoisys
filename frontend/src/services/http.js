import { API_BASE_URL, API_TIMEOUT_MS } from '../config/api.config.js';
import { clearSession, getSession } from '../utils/session.js';

export class ApiError extends Error {
  constructor(status, detail, errors = {}) {
    super(detail);
    this.name = 'ApiError';
    this.status = status;
    this.detail = detail;
    this.errors = errors;
  }
}

async function request(method, path, body, { auth = true, timeoutMs = API_TIMEOUT_MS, signal } = {}) {
  const controller = new AbortController();
  let timedOut = false;
  const abort = () => controller.abort(signal.reason);
  if (signal?.aborted) abort();
  else signal?.addEventListener('abort', abort, { once: true });
  const timer = setTimeout(() => {
    timedOut = true;
    controller.abort();
  }, timeoutMs);

  try {
    const headers = {};
    const session = auth ? getSession() : null;
    if (session) headers.Authorization = `Bearer ${session.token}`;
    if (body !== undefined) headers['Content-Type'] = 'application/json';

    const response = await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: controller.signal,
    });

    if (response.status === 401 && auth) {
      clearSession();
      window.dispatchEvent(new Event('invoisys:unauthorized'));
    }
    if (response.status === 204) return null;
    if (response.ok) return await response.json();

    let problem;
    let jsonBody = true;
    try {
      problem = await response.json();
    } catch (error) {
      if (controller.signal.aborted) throw error;
      jsonBody = false;
    }
    throw new ApiError(
      response.status,
      problem?.detail ?? problem?.title ?? (jsonBody ? 'Erro inesperado.' : `Erro na requisição (HTTP ${response.status}).`),
      problem?.errors ?? {},
    );
  } catch (error) {
    if (error instanceof ApiError) throw error;
    if (timedOut) throw new ApiError(0, 'A API demorou demais para responder.');
    if (controller.signal.aborted) throw error;
    if (error instanceof TypeError) throw new ApiError(0, 'Não foi possível conectar à API.');
    throw error;
  } finally {
    clearTimeout(timer);
    signal?.removeEventListener('abort', abort);
  }
}

export const api = {
  get: (path, opts) => request('GET', path, undefined, opts),
  post: (path, body, opts) => request('POST', path, body, opts),
  patch: (path, body, opts) => request('PATCH', path, body, opts),
  delete: (path, opts) => request('DELETE', path, undefined, opts),
};
