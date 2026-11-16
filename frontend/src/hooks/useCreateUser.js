import { useCallback, useState } from 'react';
import { createUser } from '../services/users.service';
import { validateCreateUserForm } from '../utils/validators';

const EMAIL_DUPLICADO = 'Este e-mail já está cadastrado';

// Campo da API (camelCase do ValidacaoFiltro) → campo do formulário.
const CAMPO_DO_FORM = { nome: 'fullName', email: 'email', papel: 'role', senhaInicial: 'senhaInicial' };

function errosDosCampos(errors = {}) {
  const porCampo = {};
  for (const [campoApi, mensagens] of Object.entries(errors)) {
    const chave = campoApi.charAt(0).toLowerCase() + campoApi.slice(1);
    const campo = CAMPO_DO_FORM[chave];
    if (campo) porCampo[campo] = Array.isArray(mensagens) ? mensagens.join(' ') : String(mensagens);
  }
  return porCampo;
}

export function useCreateUser({ onSuccess } = {}) {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('');
  const [senhaInicial, setSenhaInicial] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState(null);

  const limparErro = (campo) => {
    if (fieldErrors[campo]) setFieldErrors((prev) => ({ ...prev, [campo]: undefined }));
  };

  const handleFullNameChange = (e) => {
    setFullName(e.target.value);
    limparErro('fullName');
  };

  const handleEmailChange = (e) => {
    setEmail(e.target.value);
    limparErro('email');
  };

  const handleRoleChange = (e) => {
    setRole(e.target.value);
    limparErro('role');
  };

  const handleSenhaInicialChange = (e) => {
    setSenhaInicial(e.target.value);
    limparErro('senhaInicial');
  };

  const clearSubmitError = useCallback(() => setSubmitError(null), []);

  const handleSubmit = async (e) => {
    e.preventDefault();

    const validation = validateCreateUserForm({ fullName, email, role, senhaInicial });
    if (!validation.isValid) {
      setFieldErrors(validation.errors);
      return;
    }

    setIsSubmitting(true);
    setSubmitError(null);
    try {
      const newUser = await createUser({ fullName, email, role, senhaInicial });
      onSuccess?.(newUser);
    } catch (err) {
      if (err?.status === 409) {
        setFieldErrors((prev) => ({ ...prev, email: EMAIL_DUPLICADO }));
      } else if (err?.status === 422 && Object.keys(errosDosCampos(err.errors)).length > 0) {
        setFieldErrors((prev) => ({ ...prev, ...errosDosCampos(err.errors) }));
      } else {
        setSubmitError(err?.detail || err?.message || 'Erro ao cadastrar usuário.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    fullName,
    email,
    role,
    senhaInicial,
    fieldErrors,
    isSubmitting,
    submitError,
    clearSubmitError,
    handleFullNameChange,
    handleEmailChange,
    handleRoleChange,
    handleSenhaInicialChange,
    handleSubmit,
  };
}
