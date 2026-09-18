import { useUserManagement } from '../../hooks/useUserManagement';
import { Layout } from '../../components/Layout/Layout';
import { UserListItem } from '../../components/UserListItem/UserListItem';
import { UserProfilePanel } from '../../components/UserProfilePanel/UserProfilePanel';
import { AlertError } from '../../components/AlertError/AlertError';

export function UserManagementPage({
  onLogout,
  onProfileClick,
  onNavigate,
  onCreateUser,
}) {
  const {
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
  } = useUserManagement();

  return (
    <Layout
      onLogout={onLogout}
      onProfileClick={onProfileClick}
      activeScreen="user-management"
      onNavigate={onNavigate}
      fullBleed
    >
      {/* Left Side: User List */}
      <section className="flex-1 p-gutter overflow-y-auto flex flex-col gap-md">
        <div className="flex flex-col lg:flex-row lg:justify-between lg:items-end gap-sm mb-sm">
          <div className="min-w-0">
            <h2 className="font-headline-lg text-headline-lg text-primary">
              Gestão de Usuários
            </h2>
            <p className="font-body-sm text-body-sm text-on-surface-variant mt-xs">
              Gerencie acessos, permissões e histórico da equipe.
            </p>
          </div>
          <button
            onClick={onCreateUser}
            className="bg-primary text-on-primary font-label-md text-label-md py-sm px-md rounded-lg flex items-center justify-center gap-xs hover:bg-primary/90 transition-colors shadow-sm flex-shrink-0 whitespace-nowrap self-start lg:self-auto"
          >
            <span className="material-symbols-outlined text-[18px]">
              person_add
            </span>
            Novo Usuário
          </button>
        </div>

        {error && (
          <AlertError message={error} onDismiss={clearError} onRetry={refetch} />
        )}

        {loading && (
          <div className="flex flex-col gap-xs">
            {[...Array(3)].map((_, i) => (
              <div
                key={i}
                className="h-20 bg-surface-container-lowest animate-pulse rounded-xl"
              />
            ))}
          </div>
        )}

        {!loading && !error && users.length === 0 && (
          <div className="flex-1 flex items-center justify-center text-center py-xl">
            <p className="font-body-md text-body-md text-on-surface-variant">
              Nenhum usuário cadastrado até o momento.
            </p>
          </div>
        )}

        {!loading && !error && users.length > 0 && (
          <div className="flex flex-col gap-xs">
            {users.map((user) => (
              <UserListItem
                key={user.id}
                user={user}
                isSelected={selectedUser?.id === user.id}
                onClick={() => selectUser(user.id)}
              />
            ))}
          </div>
        )}
      </section>

      {/* Right Side: Profile Details Panel (only when a user is selected) */}
      {selectedUser && (
        <UserProfilePanel
          user={selectedUser}
          draftPermissions={draftPermissions}
          onTogglePermission={togglePermission}
          onSave={savePermissions}
          onToggleStatus={toggleStatus}
          onClose={clearSelection}
          isSaving={isSaving}
          saveError={saveError}
        />
      )}
    </Layout>
  );
}
