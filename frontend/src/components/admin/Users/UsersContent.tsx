import React from 'react';
import React, { useState } from 'react';
import { useAdminUsers } from '../../../hooks/admin/useAdminUsers';
import { AdminUser } from '../../../types/admin';
import './UsersContent.css';

export const UsersContent: React.FC = () => {
  const { users, loading, error, changeUserRole } = useAdminUsers();
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null);
  const [newRole, setNewRole] = useState<'User' | 'Admin'>('User');
  const [modalOpen, setModalOpen] = useState(false);

  const handleChangeRole = (user: AdminUser) => {
    setSelectedUser(user);
    setNewRole(user.role === 'Admin' ? 'User' : 'Admin');
    setModalOpen(true);
  };

  const handleConfirm = async () => {
    if (!selectedUser) return;
    try {
      await changeUserRole(selectedUser.id, newRole);
      setModalOpen(false);
      setSelectedUser(null);
    } catch (err) {
      console.error('Error:', err);
    }
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
      <h2>👥 Użytkownicy</h2>

      {error && <div className="error-banner">{error}</div>}

      <div className="users-table-wrapper">
        <table className="users-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nazwa Użytkownika</th>
              <th>Email</th>
              <th>Rola</th>
              <th>Data Rejestracji</th>
              <th>Ostatnie Logowanie</th>
              <th>Akcje</th>
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
                  <td className="username-cell">{user.username}</td>
                  <td>{user.email}</td>
                  <td>
                    <span className={`role-badge role-${user.role.toLowerCase()}`}>
                      {user.role}
                    </span>
                  </td>
                  <td>{new Date(user.createdAt).toLocaleDateString('pl-PL')}</td>
                  <td>
                    {user.lastLogin
                      ? new Date(user.lastLogin).toLocaleString('pl-PL')
                      : 'Nigdy'}
                  </td>
                  <td>
                    <button
                      className="btn-change-role"
                      onClick={() => handleChangeRole(user)}
                    >
                      🔄 Zmień Role
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {modalOpen && selectedUser && (
        <div className="modal-overlay" onClick={() => setModalOpen(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>🔄 Zmiana Roli Użytkownika</h2>
            </div>
            <div className="modal-body">
              <p className="modal-description">
                Zmieniasz rolę dla użytkownika <strong>{selectedUser.username}</strong>
              </p>
              <div className="role-change">
                <div className="current-role">
                  <strong>Obecna rola:</strong>
                  <span className={`role-badge role-${selectedUser.role.toLowerCase()}`}>
                    {selectedUser.role}
                  </span>
                </div>
                <div className="arrow">→</div>
                <div className="new-role">
                  <strong>Nowa rola:</strong>
                  <span className={`role-badge role-${newRole.toLowerCase()}`}>
                    {newRole}
                  </span>
                </div>
              </div>
              <p className="warning">⚠️ Ta akcja będzie zarejestrowana w logach audytu</p>
            </div>
            <div className="modal-footer">
              <button className="btn-cancel" onClick={() => setModalOpen(false)}>
                Anuluj
              </button>
              <button className="btn-confirm" onClick={handleConfirm}>
                🔄 Zmień Role
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
