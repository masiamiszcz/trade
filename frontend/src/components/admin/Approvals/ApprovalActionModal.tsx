
import React, { useState } from 'react';
import { AdminRequest } from '../../../types/admin';
import './ApprovalActionModal.css';

interface ApprovalActionModalProps {
  request: AdminRequest | null;
  onApprove: (id: string, comment?: string) => Promise<void>;
  onReject: (id: string, reason: string) => Promise<void>;
  onComment: (id: string, text: string) => Promise<void>;
  onClose: () => void;
  readOnlyMode?: boolean;
}

export const ApprovalActionModal: React.FC<ApprovalActionModalProps> = ({
  request,
  onApprove,
  onReject,
  onComment,
  onClose,
  readOnlyMode = false
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reason, setReason] = useState('');
  const [comment, setComment] = useState('');
  const [approveComment, setApproveComment] = useState('');

  if (!request) return null;
  
  // In read-only mode, always show. In edit mode, only show for pending
  const shouldShow = readOnlyMode || request.status.toLowerCase() === 'pending';
  if (!shouldShow) return null;

  const handleApprove = async () => {
    setLoading(true);
    setError(null);
    try {
      await onApprove(request.id, approveComment || undefined);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd zatwierdzania');
    } finally {
      setLoading(false);
    }
  };

  const handleReject = async () => {
    if (!reason.trim() || reason.length < 10) {
      setError('Powód musi mieć co najmniej 10 znaków');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      await onReject(request.id, reason);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd odrzucania');
    } finally {
      setLoading(false);
    }
  };

  const handleComment = async () => {
    if (!comment.trim()) {
      setError('Komentarz nie może być pusty');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      await onComment(request.id, comment);
      setComment('');
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd dodawania komentarza');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header approval">
          <h2>📋 Zarządzaj Wnioskiem</h2>
          <button className="close-btn" onClick={onClose}>✕</button>
        </div>

        <div className="modal-body">
          {error && <div className="error-message">{error}</div>}

          <div className="request-info">
            <div className="info-row">
              <span className="label">ID:</span>
              <span className="value">{String(request.id).substring(0, 8)}</span>
            </div>
            <div className="info-row">
              <span className="label">Admin:</span>
              <span className="value">{request.requestedByAdminId ? String(request.requestedByAdminId).substring(0, 8) : '-'}</span>
            </div>
            <div className="info-row">
              <span className="label">Akcja:</span>
              <span className="value">{request.action || '-'}</span>
            </div>
            <div className="info-row">
              <span className="label">Typ:</span>
              <span className="value">{request.entityType || '-'}</span>
            </div>
            {request.reason && (
              <div className="info-row">
                <span className="label">Powód z wniosku:</span>
                <span className="value">{request.reason}</span>
              </div>
            )}
          </div>

          {/* COMMENTS SECTION */}
          <div className="comments-section">
            <h3>💬 Komentarze</h3>
            <div className="comments-list">
              <div className="no-comments">
                ℹ️ Brak komentarzy (API w przygotowaniu)
              </div>
            </div>
          </div>

          {!readOnlyMode && (
          <div className="actions-section">
            {/* APPROVE ACTION */}
            <div className="action-group">
              <h3>✅ Zatwierdź Wniosek</h3>
              <p className="action-description">Zaakceptuj i wykonaj żądaną akcję</p>
              <textarea
                className="action-input"
                placeholder="(Opcjonalnie) Dodaj komentarz do zatwierdzenia np. GOOD JOB..."
                value={approveComment}
                onChange={(e) => {
                  setApproveComment(e.target.value);
                  setError(null);
                }}
                disabled={loading}
                rows={2}
              />
              <button
                className="action-button approve"
                onClick={handleApprove}
                disabled={loading}
              >
                {loading ? '⏳ Zatwierdzanie...' : '✅ Zatwierdź'}
              </button>
            </div>

            {/* REJECT ACTION */}
            <div className="action-group">
              <h3>❌ Odrzuć Wniosek</h3>
              <p className="action-description">Odrzuć wniosek i podaj powód (min. 10 znaków)</p>
              <textarea
                className="action-input"
                placeholder="Wyjaśnij powód odrzucenia..."
                value={reason}
                onChange={(e) => {
                  setReason(e.target.value);
                  setError(null);
                }}
                disabled={loading}
                rows={3}
              />
              <button
                className="action-button reject"
                onClick={handleReject}
                disabled={loading || !reason.trim() || reason.length < 10}
              >
                {loading ? '⏳ Odrzucanie...' : '❌ Odrzuć'}
              </button>
            </div>

            {/* COMMENT ACTION */}
            <div className="action-group">
              <h3>💬 Dodaj Komentarz</h3>
              <p className="action-description">Dodaj komentarz bez zmiany statusu wniosku</p>
              <textarea
                className="action-input"
                placeholder="Napisz komentarz..."
                value={comment}
                onChange={(e) => {
                  setComment(e.target.value);
                  setError(null);
                }}
                disabled={loading}
                rows={3}
              />
              <button
                className="action-button comment"
                onClick={handleComment}
                disabled={loading || !comment.trim()}
              >
                {loading ? '⏳ Dodawanie...' : '💬 Dodaj Komentarz'}
              </button>
            </div>
          </div>
          )}
        </div>

        <div className="modal-footer">
          <button className="btn-cancel" onClick={onClose} disabled={loading}>
            Zamknij
          </button>
        </div>
      </div>
    </div>
  );
};
