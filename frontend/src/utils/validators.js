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
