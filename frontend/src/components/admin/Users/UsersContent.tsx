import { useEffect, useState } from 'react';
import { useGetUsers } from '../../../hooks/admin/useGetUsers';
import { useAdminAuth } from '../../../hooks/admin/useAdminAuth';
import { AdminInviteModal } from '../Modals/AdminInviteModal';
import './UsersContent.css';

export const UsersContent = () => {
  const { users, loading, error, fetchUsers } = useGetUsers();
  const { isSuperAdmin } = useAdminAuth();
  const [inviteModalOpen, setInviteModalOpen] = useState(false);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

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
            </tr>
          </thead>
          <tbody>
            {users.length === 0 ? (
              <tr>
                <td colSpan={7} className="empty-state">
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
    </div>
  );
};
