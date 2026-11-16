import { useCallback, useEffect, useMemo, useState } from 'react';
import { deactivateUser, fetchUsers } from '../services/users.service';

const mensagem = (err, padrao) => err?.detail || err?.message || padrao;

/**
 * Lista de usuários (GET /usuarios), seleção e desativação.
 * @returns {{ users: Array, loading: boolean, error: string|null, refetch: Function,
 *   selectedUser: Object|null, selectUser: Function, clearSelection: Function,
 *   deactivate: Function, isSaving: boolean, saveError: string|null }}
 */
export function useUserManagement() {
  const [tentativa, setTentativa] = useState(0);
  const [resultado, setResultado] = useState(null);
  const [selectedUserId, setSelectedUserId] = useState(null);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState(null);

  useEffect(() => {
    let cancelado = false;

    fetchUsers()
      .then((users) => {
        if (!cancelado) setResultado({ tentativa, users, erro: null });
      })
      .catch((err) => {
        if (!cancelado) setResultado({ tentativa, users: [], erro: mensagem(err, 'Erro ao carregar usuários.') });
      });

    return () => {
      cancelado = true;
    };
  }, [tentativa]);

  const refetch = useCallback(() => setTentativa((n) => n + 1), []);

  const loading = resultado === null || resultado.tentativa !== tentativa;
  const users = useMemo(() => resultado?.users ?? [], [resultado]);

  const selectedUser = useMemo(
    () => users.find((user) => user.id === selectedUserId) || null,
    [users, selectedUserId]
  );

  const selectUser = useCallback((userId) => {
    setSelectedUserId(userId);
    setSaveError(null);
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedUserId(null);
    setSaveError(null);
  }, []);

  const deactivate = useCallback(async () => {
    if (!selectedUserId) return false;
    setIsSaving(true);
    setSaveError(null);
    try {
      await deactivateUser(selectedUserId);
      refetch();
      return true;
    } catch (err) {
      setSaveError(mensagem(err, 'Não foi possível desativar o usuário.'));
      return false;
    } finally {
      setIsSaving(false);
    }
  }, [selectedUserId, refetch]);

  return {
    users,
    loading,
    error: loading ? null : resultado.erro,
    refetch,
    selectedUser,
    selectUser,
    clearSelection,
    deactivate,
    isSaving,
    saveError,
  };
}
