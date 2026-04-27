import { useEffect, useState } from 'react';
import { useGetUsers } from '../../../hooks/admin/useGetUsers';
import { useAdminAuth } from '../../../hooks/admin/useAdminAuth';
import { AdminInviteModal } from '../Modals/AdminInviteModal';
import { BlockUserModal } from '../Modals/BlockUserModal';
import { DeleteUserModal } from '../Modals/DeleteUserModal';
import './UsersContent.css';

interface SelectedUser {
  id: string;
  userName: string;
  isBlocked: boolean;
}

export const UsersContent = () => {
  const { users, loading, error, fetchUsers } = useGetUsers();
  const { isSuperAdmin } = useAdminAuth();
  const [inviteModalOpen, setInviteModalOpen] = useState(false);
  const [blockModalOpen, setBlockModalOpen] = useState(false);
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<SelectedUser | null>(null);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const handleBlockClick = (user: any) => {
    setSelectedUser({
      id: user.id,
      userName: user.userName,
      isBlocked: user.isBlocked
    });
    setBlockModalOpen(true);
  };

  const handleDeleteClick = (user: any) => {
    setSelectedUser({
      id: user.id,
      userName: user.userName,
      isBlocked: user.isBlocked
    });
    setDeleteModalOpen(true);
  };

  const handleBlockSuccess = () => {
    setBlockModalOpen(false);
    setSelectedUser(null);
    fetchUsers();
  };

  const handleDeleteSuccess = () => {
    setDeleteModalOpen(false);
    setSelectedUser(null);
    fetchUsers();
  };

  if (loading) {
    return (
      <div className="users-content">
        <h2>👥 Użytkownicy</h2>
        <div className="loading">Ładowanie...</div>
      </div>
    );
  }

  return (
    <div className="users-content">
      {/* Header with title and invite button */}
      <div className="content-header">
        <h2>👥 Użytkownicy</h2>
        {/* ✨ Button visible ONLY to Super Admin */}
        {isSuperAdmin && (
          <button 
            className="btn-add-admin"
            onClick={() => setInviteModalOpen(true)}
            title="Zaproś nowego administratora (dostępne tylko dla Super Admin)"
          >
            ➕ Dodaj Admina
          </button>
        )}
      </div>

      {error && <div className="error-banner">{error}</div>}

      <div className="users-table-wrapper">
        <table className="users-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nazwa Użytkownika</th>
              <th>Email</th>
              <th>Imię i Nazwisko</th>
              <th>Rola</th>
              <th>Status</th>
              <th>Data Rejestracji</th>
              <th>Akcje</th>
            </tr>
          </thead>
          <tbody>
            {users.length === 0 ? (
              <tr>
                <td colSpan={8} className="empty-state">
                  Brak użytkowników
                </td>
              </tr>
            ) : (
              users.map((user) => (
                <tr key={user.id}>
                  <td className="id-cell">{user.id.substring(0, 8)}</td>
                  <td className="username-cell">{user.userName}</td>
                  <td>{user.email}</td>
                  <td>{user.firstName} {user.lastName}</td>
                  <td>
                    <span className={`role-badge role-${user.role.toLowerCase()}`}>
                      {user.role}
                    </span>
                  </td>
                  <td>
                    <span className={`status-badge status-${user.status.toLowerCase()}`}>
                      {user.status}
                    </span>
                  </td>
                  <td>{new Date(user.createdAtUtc).toLocaleDateString('pl-PL')}</td>
                  <td className="actions-cell">
                    {/* Hide block/unblock button for deleted users */}
                    {user.status.toLowerCase() !== 'deleted' && (
                      <>
                        {user.isBlocked ? (
                          <button
                            className="btn-action btn-unblock"
                            onClick={() => handleBlockClick(user)}
                            title="Odblokuj użytkownika"
                          >
                            🔓 Odblokuj
                          </button>
                        ) : (
                          <button
                            className="btn-action btn-block"
                            onClick={() => handleBlockClick(user)}
                            title="Zablokuj użytkownika"
                          >
                            🔒 Zablokuj
                          </button>
                        )}
                        <button
                          className="btn-action btn-delete"
                          onClick={() => handleDeleteClick(user)}
                          title="Usuń użytkownika"
                        >
                          🗑️ Usuń
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Role change modal */}
      {/* Admin invitation modal */}
      <AdminInviteModal
        isOpen={inviteModalOpen}
        onClose={() => setInviteModalOpen(false)}
        onSuccess={(token) => {
          console.log('Invitation token:', token);
          setInviteModalOpen(false);
        }}
      />

      {/* Block/Unblock User Modal */}
      {selectedUser && (
        <BlockUserModal
          isOpen={blockModalOpen}
          onClose={() => {
            setBlockModalOpen(false);
            setSelectedUser(null);
          }}
          userId={selectedUser.id}
          userName={selectedUser.userName}
          isCurrentlyBlocked={selectedUser.isBlocked}
          onSuccess={handleBlockSuccess}
        />
      )}

      {/* Delete User Modal */}
      {selectedUser && (
        <DeleteUserModal
          isOpen={deleteModalOpen}
          onClose={() => {
            setDeleteModalOpen(false);
            setSelectedUser(null);
          }}
          userId={selectedUser.id}
          userName={selectedUser.userName}
          onSuccess={handleDeleteSuccess}
        />
      )}
    </div>
  );
};
