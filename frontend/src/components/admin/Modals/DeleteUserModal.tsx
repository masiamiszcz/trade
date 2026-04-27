import React, { useState } from 'react';
import { useDeleteUser } from '../../../hooks/admin/useDeleteUser';
import './DeleteUserModal.css';

interface DeleteUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  userId: string;
  userName: string;
  onSuccess?: () => void;
}

export const DeleteUserModal: React.FC<DeleteUserModalProps> = ({
  isOpen,
  onClose,
  userId,
  userName,
  onSuccess
}) => {
  const { deleteUser, loading, error } = useDeleteUser();
  const [reason, setReason] = useState('');
  const [confirmChecked, setConfirmChecked] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!confirmChecked) {
      alert('Proszę potwierdź, że chcesz usunąć to konto');
      return;
    }

    if (!reason.trim()) {
      alert('Proszę podaj powód usunięcia');
      return;
    }

    const success = await deleteUser(userId, reason);
    if (success) {
      setReason('');
      setConfirmChecked(false);
      onClose();
      onSuccess?.();
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content delete-user-modal" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h3>🗑️ Usuń Użytkownika</h3>
          <button className="btn-close" onClick={onClose}>✕</button>
        </div>

        <div className="warning-box">
          ⚠️ <strong>Uwaga:</strong> Ta operacja spowoduje utworzenie prośby o usunięcie konta. Wymaga ona zatwierdzenia przez administratora!
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Użytkownik:</label>
            <p className="user-info">{userName} (ID: {userId.substring(0, 8)})</p>
          </div>

          <div className="form-group">
            <label htmlFor="reason">Powód usunięcia:</label>
            <textarea
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Podaj powód usunięcia konta..."
              rows={4}
              disabled={loading}
            />
          </div>

          <div className="form-group checkbox-group">
            <label>
              <input
                type="checkbox"
                checked={confirmChecked}
                onChange={(e) => setConfirmChecked(e.target.checked)}
                disabled={loading}
              />
              <strong>Potwierdzam, że chcę usunąć to konto użytkownika</strong>
            </label>
          </div>

          {error && <div className="error-message">{error}</div>}

          <div className="modal-actions">
            <button
              type="button"
              className="btn-cancel"
              onClick={onClose}
              disabled={loading}
            >
              Anuluj
            </button>
            <button
              type="submit"
              className="btn-delete-confirm"
              disabled={loading || !reason.trim() || !confirmChecked}
            >
              {loading ? 'Przetwarzanie...' : 'Usuń Konto'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
