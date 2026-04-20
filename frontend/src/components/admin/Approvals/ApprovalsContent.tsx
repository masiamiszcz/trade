
import React, { useState } from 'react';
import { useAdminRequests } from '../../../hooks/admin/useAdminRequests';
import { AdminRequest } from '../../../types/admin';
import { DataTable, Column } from '../../common/DataTable';
import { ApprovalActionModal } from './ApprovalActionModal';
import './ApprovalsContent.css';

export const ApprovalsContent: React.FC = () => {
  const {
    requests,
    totalCount,
    currentPage,
    totalPages,
    loading,
    error,
    page,
    pageSize,
    setPage,
    setPageSize,
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

  const columns: Column<AdminRequest>[] = [
    {
      key: 'id',
      label: 'ID',
      width: '80px',
      render: (value) => <span className="id-cell">{String(value).substring(0, 8)}</span>
    },
    {
      key: 'requestedBy',
      label: 'Użytkownik',
      width: '130px'
    },
    {
      key: 'action',
      label: 'Akcja',
      width: '100px',
      render: (value) => <span className="action-badge">{value}</span>
    },
    {
      key: 'entityType',
      label: 'Typ',
      width: '100px'
    },
    {
      key: 'status',
      label: 'Status',
      width: '110px',
      render: (value) => {
        const statusClass = `status-badge status-${value}`;
        const statusLabel = {
          pending: 'Oczekujący',
          approved: 'Zatwierdzony',
          rejected: 'Odrzucony'
        }[value as string] || value;
        return <span className={statusClass}>{statusLabel}</span>;
      }
    },
    {
      key: 'createdAt',
      label: 'Data',
      width: '150px',
      render: (value) => new Date(value).toLocaleString('pl-PL')
    }
  ];

  const getRowActions = (request: AdminRequest) => (
    <div className="action-buttons">
      {request.status === 'pending' ? (
        <>
          <button className="btn-approve" onClick={() => handleApprove(request)}>
            ✅ Zatwierdź
          </button>
          <button className="btn-reject" onClick={() => handleReject(request)}>
            ❌ Odrzuć
          </button>
        </>
      ) : (
        <span className="action-done">Zakończono</span>
      )}
    </div>
  );

  return (
    <div className="approvals-content">
      <h2>📋 Zatwierdzenia</h2>

      {error && <div className="error-banner">{error}</div>}

      <DataTable
        columns={columns}
        data={requests}
        totalCount={totalCount}
        currentPage={currentPage}
        totalPages={totalPages}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        loading={loading}
        actions={getRowActions}
      />

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
