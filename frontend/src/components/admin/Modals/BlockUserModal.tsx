import React, { useState } from 'react';
import { useBlockUser } from '../../../hooks/admin/useBlockUser';
import './BlockUserModal.css';

interface BlockUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  userId: string;
  userName: string;
  isCurrentlyBlocked: boolean;
  onSuccess?: () => void;
}

export const BlockUserModal: React.FC<BlockUserModalProps> = ({
  isOpen,
  onClose,
  userId,
  userName,
  isCurrentlyBlocked,
  onSuccess
}) => {
  const { blockUser, unblockUser, loading, error } = useBlockUser();
  const [reason, setReason] = useState('');
  const [duration, setDuration] = useState(48); // default 48h
  const [isPermanent, setIsPermanent] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!reason.trim()) {
      alert('Proszę podaj powód blokady');
      return;
    }

    if (isCurrentlyBlocked) {
      // Unblock
      const success = await unblockUser(userId, reason);
      if (success) {
        setReason('');
        onClose();
        onSuccess?.();
      }
    } else {
      // Block
      const durationMs = isPermanent ? 0 : duration * 60 * 60 * 1000; // convert hours to ms
      const success = await blockUser(userId, reason, durationMs);
      if (success) {
        setReason('');
        setDuration(48);
        setIsPermanent(false);
        onClose();
        onSuccess?.();
      }
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content block-user-modal" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h3>
            {isCurrentlyBlocked ? '🔓 Odblokuj Użytkownika' : '🔒 Zablokuj Użytkownika'}
          </h3>
          <button className="btn-close" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Użytkownik:</label>
            <p className="user-info">{userName} (ID: {userId.substring(0, 8)})</p>
          </div>

          <div className="form-group">
            <label htmlFor="reason">Powód:</label>
            <textarea
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder={isCurrentlyBlocked ? "Powód odblokowania..." : "Powód blokady..."}
              rows={4}
              disabled={loading}
            />
          </div>

          {!isCurrentlyBlocked && (
            <>
              <div className="form-group checkbox-group">
                <label>
                  <input
                    type="checkbox"
                    checked={isPermanent}
                    onChange={(e) => setIsPermanent(e.target.checked)}
                    disabled={loading}
                  />
                  Blokada permanentna (bez ograniczenia czasowego)
                </label>
              </div>

              {!isPermanent && (
                <div className="form-group">
                  <label htmlFor="duration">Czas blokady (godziny):</label>
                  <input
                    type="number"
                    id="duration"
                    value={duration}
                    onChange={(e) => setDuration(Math.max(1, parseInt(e.target.value) || 1))}
                    min="1"
                    max="8760"
                    placeholder="Domyślnie 48h"
                    disabled={loading}
                  />
                  <small>Min: 1h, Max: 1 rok (8760h)</small>
                </div>
              )}
            </>
          )}

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
              className={isCurrentlyBlocked ? "btn-unblock-confirm" : "btn-block-confirm"}
              disabled={loading || !reason.trim()}
            >
              {loading ? 'Przetwarzanie...' : (isCurrentlyBlocked ? 'Odblokuj' : 'Zablokuj')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
