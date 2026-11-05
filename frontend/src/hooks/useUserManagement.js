import { useState, useCallback, useMemo } from 'react';
import { useAsyncResource } from './useAsyncResource';
import {
  fetchUsers,
  updateUserPermissions,
  toggleUserStatus,
} from '../services/users.service';

export function useUserManagement() {
  const resource = useAsyncResource(fetchUsers);
  const [updates, setUpdates] = useState({});
  const users = useMemo(() => (resource.data || []).map((user) => updates[user.id] || user),
    [resource.data, updates]);
  const { loading, error, clearError, refetch } = resource;

  const [selectedUserId, setSelectedUserId] = useState(null);
  const [draftPermissions, setDraftPermissions] = useState(null);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState(null);

  const selectedUser = useMemo(
    () => users.find((user) => user.id === selectedUserId) || null,
    [users, selectedUserId]
  );

  const selectUser = useCallback((userId) => {
    setSelectedUserId(userId);
    setDraftPermissions({ ...users.find((user) => user.id === userId)?.permissions });
    setSaveError(null);
  }, [users]);

  const clearSelection = useCallback(() => {
    setSelectedUserId(null);
    setDraftPermissions(null);
    setSaveError(null);
  }, []);

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
      setUpdates((current) => ({ ...current, [updatedUser.id]: updatedUser }));
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
      setUpdates((current) => ({ ...current, [updatedUser.id]: updatedUser }));
    } catch (err) {
      setSaveError(err.message || 'Erro ao atualizar status do usuário');
    } finally {
      setIsSaving(false);
    }
  }, [selectedUserId]);

  return {
    users,
    loading,
    error,
    clearError,
    refetch,
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
