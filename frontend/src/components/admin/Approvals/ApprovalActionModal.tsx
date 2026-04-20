
import React from 'react';
import { AdminRequest } from '../../../types/admin';
import './ApprovalActionModal.css';

interface ApprovalActionModalProps {
  type: 'approve' | 'reject';
  request: AdminRequest;
  reason: string;
  onReasonChange: (reason: string) => void;
  onConfirm: () => void;
  onCancel: () => void;
}

export const ApprovalActionModal: React.FC<ApprovalActionModalProps> = ({
  type,
  request,
  reason,
  onReasonChange,
  onConfirm,
  onCancel
}) => {
  const isApprove = type === 'approve';
  const title = isApprove ? '✅ Zatwierdź Wniosek' : '❌ Odrzuć Wniosek';
  const description = isApprove
    ? 'Jesteś pewny, że chcesz zatwierdź ten wniosek?'
    : 'Podaj powód odrzucenia tego wniosku';

  return (
    <div className="modal-overlay" onClick={onCancel}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className={`modal-header ${type}`}>
          <h2>{title}</h2>
        </div>

        <div className="modal-body">
          <p className="modal-description">{description}</p>

          <div className="request-info">
            <p>
              <strong>Użytkownik:</strong> {request.requestedBy}
            </p>
            <p>
              <strong>Akcja:</strong> {request.action}
            </p>
            <p>
              <strong>Typ:</strong> {request.entityType}
            </p>
            {request.reason && (
              <p>
                <strong>Powód z wniosku:</strong> {request.reason}
              </p>
            )}
          </div>

          {!isApprove || true && (
            <div className="form-group">
              <label>
                {isApprove ? 'Komentarz (opcjonalnie):' : 'Powód odrzucenia (wymagany):'}
              </label>
              <textarea
                value={reason}
                onChange={(e) => onReasonChange(e.target.value)}
                placeholder={isApprove ? 'Dodaj komentarz...' : 'Wyjaśnij powód...'}
                rows={4}
                required={!isApprove}
              />
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button className="btn-cancel" onClick={onCancel}>
            Anuluj
          </button>
          <button
            className={`btn-confirm ${type}`}
            onClick={onConfirm}
            disabled={!isApprove && !reason.trim()}
          >
            {isApprove ? '✅ Zatwierdź' : '❌ Odrzuć'}
          </button>
        </div>
      </div>
    </div>
  );
};
