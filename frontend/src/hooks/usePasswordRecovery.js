import { useState, useCallback } from 'react';
import {
  requestPasswordReset as requestPasswordResetService,
  resendPasswordReset as resendPasswordResetService,
} from '../services/auth.service';

/**
 * Custom hook para o fluxo de solicitação e reenvio de recuperação de senha.
 * Expõe { loading, error, isSuccess, message, isResending, resendSuccess,
 *          requestReset, resendReset, resetState, clearError }.
 */
export function usePasswordRecovery() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [message, setMessage] = useState(null);
  const [isResending, setIsResending] = useState(false);
  const [resendSuccess, setResendSuccess] = useState(false);

  const requestReset = useCallback(async ({ email }) => {
    setLoading(true);
    setError(null);
    setIsSuccess(false);
    setMessage(null);

    try {
      const response = await requestPasswordResetService({ email });
      setIsSuccess(true);
      setMessage(response.message);
      return response;
    } catch (err) {
      const errMsg =
        err instanceof Error
          ? err.message
          : 'Não foi possível processar a solicitação de recuperação.';
      setError(errMsg);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const resendReset = useCallback(async ({ email }) => {
    setIsResending(true);
    setResendSuccess(false);
    setError(null);

    try {
      await resendPasswordResetService({ email });
      setResendSuccess(true);
    } catch (err) {
      const errMsg =
        err instanceof Error
          ? err.message
          : 'Não foi possível reenviar o link de recuperação.';
      setError(errMsg);
    } finally {
      setIsResending(false);
    }
  }, []);

  const resetState = useCallback(() => {
    setLoading(false);
    setError(null);
    setIsSuccess(false);
    setMessage(null);
    setIsResending(false);
    setResendSuccess(false);
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    loading,
    error,
    isSuccess,
    message,
    isResending,
    resendSuccess,
    requestReset,
    resendReset,
    resetState,
    clearError,
  };
}
