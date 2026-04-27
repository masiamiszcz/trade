import { useState } from 'react';
import { useDeleteUser } from '../../../hooks/admin/useDeleteUser';
import './DeleteUserModal.css';

export interface DeleteUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  userId: string;
  userName: string;
  userEmail: string;
}

export const DeleteUserModal = ({
  isOpen,
  onClose,
  onSuccess,
  userId,
  userName,
  userEmail,
}: DeleteUserModalProps) => {
  const { deleteUser, loading, error, clearError } = useDeleteUser();
  const [isConfirmed, setIsConfirmed] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  if (!isOpen) {
    return null;
  }

  const handleConfirmDelete = async () => {
    if (!isConfirmed) {
      console.warn('⚠️ Must confirm checkbox');
      return;
    }

    setIsDeleting(true);
    try {
      console.log('🔴 Initiating deletion of user:', userId);
      await deleteUser(userId);

      console.log('✅ Deletion successful, closing modal');
      setIsConfirmed(false);
      setIsDeleting(false);
      onSuccess();
      onClose();
    } catch (err) {
      console.error('❌ Deletion failed:', err);
      setIsDeleting(false);
    }
  };

  const handleClose = () => {
    setIsConfirmed(false);
    clearError();
    onClose();
  };

  return (
    <div className="delete-user-modal-overlay" onClick={handleClose}>
      <div
        className="delete-user-modal"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header">
          <h3>🔴 Usuń Użytkownika</h3>
          <button
            className="modal-close-btn"
            onClick={handleClose}
            disabled={isDeleting}
            aria-label="Close modal"
          >
            ✕
          </button>
        </div>

        <div className="modal-body">
          <div className="warning-box">
            <p className="warning-title">⚠️ Ta akcja jest nieodwracalna!</p>
            <p>Użytkownik będzie zaznaczony jako usunięty i nie będzie mógł się zalogować.</p>
          </div>

          <div className="user-details">
            <p>
              <strong>Nazwa użytkownika:</strong> {userName}
            </p>
            <p>
              <strong>Email:</strong> {userEmail}
            </p>
            <p>
              <strong>ID:</strong> <code>{userId.substring(0, 8)}...</code>
            </p>
          </div>

          <div className="confirmation-section">
            <label htmlFor="confirm-checkbox" className="checkbox-label">
              <input
                id="confirm-checkbox"
                type="checkbox"
                checked={isConfirmed}
                onChange={(e) => setIsConfirmed(e.target.checked)}
                disabled={isDeleting}
              />
              <span>
                Rozumiem, że ta akcja jest <strong>nieodwracalna</strong> i potwierdzam usunięcie
              </span>
            </label>
          </div>

          {error && (
            <div className="error-message">
              <p>❌ {error}</p>
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button
            className="btn btn-cancel"
            onClick={handleClose}
            disabled={isDeleting}
          >
            Anuluj
          </button>
          <button
            className="btn btn-delete"
            onClick={handleConfirmDelete}
            disabled={!isConfirmed || isDeleting}
          >
            {isDeleting ? '⏳ Usuwanie...' : '🔴 Usuń Użytkownika'}
          </button>
        </div>
      </div>
    </div>
  );
};
