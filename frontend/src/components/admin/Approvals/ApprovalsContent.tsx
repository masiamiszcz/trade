
import React, { useState, useEffect } from 'react';
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
    rejectRequest,
    addComment,
    refetch,
    fetchPendingRequests,
    fetchAllRequests,
    getRequestById,
  } = useAdminRequests();

  const [viewMode, setViewMode] = useState<'pending' | 'all'>('pending');
  const [selectedRequest, setSelectedRequest] = useState<AdminRequest | null>(null);

  useEffect(() => {
    if (viewMode === 'pending') {
      fetchPendingRequests();
    } else {
      fetchAllRequests();
    }
  }, [viewMode, fetchPendingRequests, fetchAllRequests]);

  /**
   * Smart refetch - refetches current view mode (pending or all)
   */
  const smartRefetch = async () => {
    if (viewMode === 'pending') {
      await fetchPendingRequests();
    } else {
      await fetchAllRequests();
    }
  };

  const handleRowClick = async (request: AdminRequest) => {
    if (viewMode === 'pending') {
      // Pending mode: only allow clicking on pending requests
      if (request.status === 'Pending') {
        setSelectedRequest(request);
      }
    } else {
      // All mode: load full details from API
      const detailed = await getRequestById(request.id);
      if (detailed) {
        setSelectedRequest(detailed);
      }
    }
  };

  const handleApprove = async (id: string, comment?: string) => {
    await approveRequest(id, comment);
    setSelectedRequest(null);
    await smartRefetch();
  };

  const handleReject = async (id: string, reason: string) => {
    await rejectRequest(id, reason);
    setSelectedRequest(null);
    await smartRefetch();
  };

  const handleComment = async (id: string, text: string) => {
    await addComment(id, text);
    // Keep modal open so user can add more comments or take other actions
    await smartRefetch();
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Pending':
        return { emoji: '⏳', label: 'Oczekujący', class: 'status-pending' };
      case 'Approved':
        return { emoji: '✅', label: 'Zatwierdzony', class: 'status-approved' };
      case 'Rejected':
        return { emoji: '❌', label: 'Odrzucony', class: 'status-rejected' };
      default:
        return { emoji: '❓', label: status, class: 'status-unknown' };
    }
  };

  return (
    <div className="approvals-content">
      <h2>📋 Zatwierdzenia</h2>

      <div style={{ marginBottom: '20px', display: 'flex', gap: '8px' }}>
        <button
          onClick={() => setViewMode('pending')}
          style={{
            padding: '8px 16px',
            borderRadius: '6px',
            border: viewMode === 'pending' ? '2px solid #00d4ff' : '1px solid #a4b5d6',
            background: viewMode === 'pending' ? 'rgba(0, 212, 255, 0.1)' : 'transparent',
            color: viewMode === 'pending' ? '#00d4ff' : '#a4b5d6',
            fontWeight: viewMode === 'pending' ? 600 : 400,
            cursor: 'pointer',
            transition: 'all 0.2s',
          }}
        >
          ⏳ Zatwierdzenia
        </button>
        <button
          onClick={() => setViewMode('all')}
          style={{
            padding: '8px 16px',
            borderRadius: '6px',
            border: viewMode === 'all' ? '2px solid #00d4ff' : '1px solid #a4b5d6',
            background: viewMode === 'all' ? 'rgba(0, 212, 255, 0.1)' : 'transparent',
            color: viewMode === 'all' ? '#00d4ff' : '#a4b5d6',
            fontWeight: viewMode === 'all' ? 600 : 400,
            cursor: 'pointer',
            transition: 'all 0.2s',
          }}
        >
          📚 Wszystkie wnioski
        </button>
      </div>

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
                <th style={{ width: '150px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Admin przez</th>
                <th style={{ width: '100px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Akcja</th>
                <th style={{ width: '100px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Typ</th>
                <th style={{ width: '110px', padding: '12px', textAlign: 'left', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Status</th>
                <th style={{ width: '180px', padding: '12px', textAlign: 'right', color: '#00d4ff', fontWeight: 600, fontSize: '13px' }}>Data i Godzina</th>
              </tr>
            </thead>
            <tbody>
              {requests.map((request) => {
                const statusInfo = getStatusBadge(request.status);
                const isPending = request.status.toLowerCase() === 'pending';
                const isClickable = viewMode === 'all' || isPending;

                return (
                  <tr
                    key={request.id}
                    onClick={() => {
                      if (isClickable) {
                        handleRowClick(request);
                      }
                    }}
                    style={{
                      borderBottom: '1px solid rgba(0, 212, 255, 0.1)',
                      transition: 'background-color 0.2s',
                      cursor: isClickable ? 'pointer' : 'default',
                      backgroundColor: isClickable ? undefined : 'transparent'
                    }}
                    onMouseEnter={(e) => {
                      if (isClickable) {
                        e.currentTarget.style.backgroundColor = 'rgba(0, 212, 255, 0.08)';
                      }
                    }}
                    onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                  >
                    <td style={{ width: '80px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                      <span className="id-cell">{String(request.id).substring(0, 8)}</span>
                    </td>
                    <td style={{ width: '150px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                      {request.requestedByAdminId ? String(request.requestedByAdminId).substring(0, 8) : '-'}
                    </td>
                    <td style={{ width: '100px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                      <span className="action-badge">{request.action || '-'}</span>
                    </td>
                    <td style={{ width: '100px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                      {request.entityType || '-'}
                    </td>
                    <td style={{ width: '110px', padding: '12px', color: '#a4b5d6', fontSize: '13px' }}>
                      <span className={`status-badge ${statusInfo.class}`}>
                        {statusInfo.emoji} {statusInfo.label}
                      </span>
                    </td>
                    <td style={{ width: '180px', padding: '12px', color: '#a4b5d6', fontSize: '13px', textAlign: 'right' }}>
                      {new Date(request.createdAtUtc).toLocaleString('pl-PL', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                        second: '2-digit'
                      })}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {selectedRequest && (
        <ApprovalActionModal
          request={selectedRequest}
          onApprove={handleApprove}
          onReject={handleReject}
          onComment={handleComment}
          onClose={() => setSelectedRequest(null)}
          readOnlyMode={viewMode === 'all'}
        />
      )}
    </div>
  );
};
