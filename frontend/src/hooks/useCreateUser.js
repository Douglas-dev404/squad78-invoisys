import { useState, useCallback } from 'react';
import { createUser } from '../services/users.service';
import { validateCreateUserForm } from '../utils/validators';

const INITIAL_EXTRA_PERMISSIONS = {
  systemLogs: false,
  apiSettings: false,
  teamManagement: false,
  exportReports: false,
};

export function useCreateUser({ onSuccess } = {}) {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('');
  const [extraPermissions, setExtraPermissions] = useState(
    INITIAL_EXTRA_PERMISSIONS
  );
  const [fieldErrors, setFieldErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState(null);

  const handleFullNameChange = (e) => {
    setFullName(e.target.value);
    if (fieldErrors.fullName) {
      setFieldErrors((prev) => ({ ...prev, fullName: undefined }));
    }
  };

  const handleEmailChange = (e) => {
    setEmail(e.target.value);
    if (fieldErrors.email) {
      setFieldErrors((prev) => ({ ...prev, email: undefined }));
    }
  };

  const handleRoleChange = (e) => {
    setRole(e.target.value);
    if (fieldErrors.role) {
      setFieldErrors((prev) => ({ ...prev, role: undefined }));
    }
  };

  const toggleExtraPermission = useCallback((key) => {
    setExtraPermissions((prev) => ({ ...prev, [key]: !prev[key] }));
  }, []);

  const clearSubmitError = useCallback(() => {
    setSubmitError(null);
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();

    const validation = validateCreateUserForm({ fullName, email, role });
    if (!validation.isValid) {
      setFieldErrors(validation.errors);
      return;
    }

    try {
      setIsSubmitting(true);
      setSubmitError(null);
      const newUser = await createUser({
        fullName,
        email,
        role,
        extraPermissions,
      });
      onSuccess?.(newUser);
    } catch (err) {
      setSubmitError(err.message || 'Erro ao cadastrar usuário.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    fullName,
    email,
    role,
    extraPermissions,
    fieldErrors,
    isSubmitting,
    submitError,
    clearSubmitError,
    handleFullNameChange,
    handleEmailChange,
    handleRoleChange,
    toggleExtraPermission,
    handleSubmit,
  };
}
