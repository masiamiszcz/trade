
import React, { useState } from 'react';
import { useAdminRequests } from '../../../hooks/admin/useAdminRequests';
import { AdminRequest } from '../../../types/admin';
import { ApprovalActionModal } from './ApprovalActionModal';
import './ApprovalsContent.css';

export const ApprovalsContent: React.FC = () => {
  const {
    requests,
    loading,
    error,
    approveRequest,
    rejectRequest
  } = useAdminRequests();

  const [selectedRequest, setSelectedRequest] = useState<AdminRequest | null>(null);
  const [modalType, setModalType] = useState<'approve' | 'reject' | null>(null);
  const [reason, setReason] = useState('');

  const handleApprove = (request: AdminRequest) => {
    setSelectedRequest(request);
    setModalType('approve');
  };

  const handleReject = (request: AdminRequest) => {
    setSelectedRequest(request);
    setModalType('reject');
  };

  const handleConfirm = async () => {
    if (!selectedRequest) return;

    try {
      if (modalType === 'approve') {
        await approveRequest(selectedRequest.id, reason);
      } else if (modalType === 'reject') {
        await rejectRequest(selectedRequest.id, reason);
      }
      setReason('');
      setSelectedRequest(null);
      setModalType(null);
    } catch (err) {
      console.error('Error:', err);
    }
  };

  return (
    <div className="approvals-content">
      <h2>📋 Zatwierdzenia</h2>

      {error && <div className="error-banner">{error}</div>}

      {loading ? (
        <div style={{ padding: '20px', textAlign: 'center', color: '#00d4ff' }}>
          ⏳ Ładowanie wniosków...
        </div>
      ) : requests.length === 0 ? (
        <div style={{ padding: '20px', textAlign: 'center', color: '#a4b5d6' }}>
          Brak wniosków do zatwierdzenia ✅
        </div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid rgba(0, 212, 255, 0.2)' }}>
                <th style={{ width: '80px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>ID</th>
                <th style={{ width: '130px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Użytkownik</th>
                <th style={{ width: '100px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Akcja</th>
                <th style={{ width: '100px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Typ</th>
                <th style={{ width: '110px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Status</th>
                <th style={{ width: '150px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Data</th>
                <th style={{ padding: '12px', textAlign: 'right', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Akcje</th>
              </tr>
            </thead>
            <tbody>
              {requests.map((request) => (
                <tr
                  key={request.id}
                  style={{
                    borderBottom: '1px solid rgba(0, 212, 255, 0.1)',
                    transition: 'background-color 0.2s'
                  }}
                  onMouseEnter={(e) =>
                    (e.currentTarget.style.backgroundColor = 'rgba(0, 212, 255, 0.05)')
                  }
                  onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                >
                  <td style={{ width: '80px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                    <span className="id-cell">{String(request.id).substring(0, 8)}</span>
                  </td>
                  <td style={{ width: '130px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                    {request.requestedBy || '-'}
                  </td>
                  <td style={{ width: '100px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                    <span className="action-badge">{request.action || '-'}</span>
                  </td>
                  <td style={{ width: '100px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                    {request.entityType || '-'}
                  </td>
                  <td style={{ width: '110px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                    <span className={`status-badge status-${request.status}`}>
                      {request.status === 'pending' && '⏳ Oczekujący'}
                      {request.status === 'approved' && '✅ Zatwierdzony'}
                      {request.status === 'rejected' && '❌ Odrzucony'}
                    </span>
                  </td>
                  <td style={{ width: '150px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                    {new Date(request.createdAt).toLocaleString('pl-PL')}
                  </td>
                  <td style={{ padding: '12px', textAlign: 'right', display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                    {request.status === 'pending' ? (
                      <>
                        <button
                          className="btn-approve"
                          onClick={() => handleApprove(request)}
                          style={{ marginRight: '4px' }}
                        >
                          ✅ Zatwierdź
                        </button>
                        <button className="btn-reject" onClick={() => handleReject(request)}>
                          ❌ Odrzuć
                        </button>
                      </>
                    ) : (
                      <span className="action-done">Zakończono</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {modalType && selectedRequest && (
        <ApprovalActionModal
          type={modalType}
          request={selectedRequest}
          reason={reason}
          onReasonChange={setReason}
          onConfirm={handleConfirm}
          onCancel={() => {
            setModalType(null);
            setSelectedRequest(null);
            setReason('');
          }}
        />
      )}
    </div>
  );
};
