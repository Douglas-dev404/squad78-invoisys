import { useState, useEffect, useCallback, useMemo } from 'react';
import {
  fetchUsers,
  updateUserPermissions,
  toggleUserStatus,
} from '../services/users.service';

export function useUserManagement() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [selectedUserId, setSelectedUserId] = useState(null);
  const [draftPermissions, setDraftPermissions] = useState(null);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState(null);

  const loadUsers = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await fetchUsers();
      setUsers(data);
    } catch (err) {
      setError(err.message || 'Erro ao carregar usuários');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

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
    setDraftPermissions(null);
    setSaveError(null);
  }, []);

  useEffect(() => {
    if (selectedUser) {
      setDraftPermissions({ ...selectedUser.permissions });
    }
  }, [selectedUser]);

  const togglePermission = useCallback((key) => {
    setDraftPermissions((prev) => ({ ...prev, [key]: !prev[key] }));
  }, []);

  const savePermissions = useCallback(async () => {
    if (!selectedUserId || !draftPermissions) return;
    try {
      setIsSaving(true);
      setSaveError(null);
      const updatedUser = await updateUserPermissions(
        selectedUserId,
        draftPermissions
      );
      setUsers((prev) =>
        prev.map((user) => (user.id === updatedUser.id ? updatedUser : user))
      );
    } catch (err) {
      setSaveError(err.message || 'Erro ao salvar permissões');
    } finally {
      setIsSaving(false);
    }
  }, [selectedUserId, draftPermissions]);

  const toggleStatus = useCallback(async () => {
    if (!selectedUserId) return;
    try {
      setIsSaving(true);
      setSaveError(null);
      const updatedUser = await toggleUserStatus(selectedUserId);
      setUsers((prev) =>
        prev.map((user) => (user.id === updatedUser.id ? updatedUser : user))
      );
    } catch (err) {
      setSaveError(err.message || 'Erro ao atualizar status do usuário');
    } finally {
      setIsSaving(false);
    }
  }, [selectedUserId]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    users,
    loading,
    error,
    clearError,
    refetch: loadUsers,
    selectedUser,
    selectUser,
    clearSelection,
    draftPermissions,
    togglePermission,
    savePermissions,
    toggleStatus,
    isSaving,
    saveError,
  };
}
