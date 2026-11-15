/**
 * Utilitários de validação de formulários.
 */

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Verifica se um endereço de e-mail possui formato válido.
 * @param {string} email
 * @returns {boolean}
 */
export function isValidEmail(email) {
  if (!email || typeof email !== 'string') return false;
  return EMAIL_REGEX.test(email.trim());
}

/**
 * Valida os campos do formulário de login.
 * @param {Object} values
 * @param {string} values.email
 * @param {string} values.password
 * @returns {{ isValid: boolean, errors: { email?: string, password?: string } }}
 */
export function validateLoginForm({ email, password }) {
  const errors = {};

  if (!email || !email.trim()) {
    errors.email = 'O e-mail corporativo é obrigatório.';
  } else if (!isValidEmail(email)) {
    errors.email = 'Informe um endereço de e-mail corporativo válido.';
  }

  if (!password) {
    errors.password = 'A senha é obrigatória.';
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  };
}

/**
 * Valida os campos do formulário de cadastro de novo usuário.
 * @param {Object} values
 * @param {string} values.fullName
 * @param {string} values.email
 * @param {string} values.role
 * @returns {{ isValid: boolean, errors: { fullName?: string, email?: string, role?: string } }}
 */
export const SENHA_INICIAL_MINIMO = 8;

export function validateCreateUserForm({ fullName, email, role, senhaInicial }) {
  const errors = {};

  if (!fullName || !fullName.trim()) {
    errors.fullName = 'O nome completo é obrigatório.';
  }

  if (!email || !email.trim()) {
    errors.email = 'O e-mail corporativo é obrigatório.';
  } else if (!isValidEmail(email)) {
    errors.email = 'Informe um endereço de e-mail corporativo válido.';
  }

  if (!role) {
    errors.role = 'Selecione o papel do usuário.';
  }

  if (!senhaInicial) {
    errors.senhaInicial = 'A senha inicial é obrigatória.';
  } else if (senhaInicial.length < SENHA_INICIAL_MINIMO) {
    errors.senhaInicial = `A senha inicial deve ter pelo menos ${SENHA_INICIAL_MINIMO} caracteres.`;
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  };
}
