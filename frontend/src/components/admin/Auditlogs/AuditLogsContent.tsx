import React from 'react';
import React from 'react';
import { useAuditLogs } from '../../../hooks/admin/useAuditLogs';
import { AuditLog } from '../../../types/admin';
import { DataTable, Column } from '../../common/DataTable';
import './AuditLogsContent.css';

export const AuditLogsContent: React.FC = () => {
  const {
    logs,
    totalCount,
    currentPage,
    totalPages,
    loading,
    error,
    page,
    pageSize,
    setPage,
    setPageSize
  } = useAuditLogs();

  const columns: Column<AuditLog>[] = [
    {
      key: 'adminName',
      label: 'Administrator',
      width: '120px'
    },
    {
      key: 'action',
      label: 'Akcja',
      width: '100px',
      render: (value) => <span className="action-badge">{value}</span>
    },
    {
      key: 'entityType',
      label: 'Typ Encji',
      width: '100px'
    },
    {
      key: 'entityId',
      label: 'ID Encji',
      width: '120px',
      render: (value) => <span className="entity-id">{String(value).substring(0, 12)}...</span>
    },
    {
      key: 'ipAddress',
      label: 'Adres IP',
      width: '130px',
      render: (value) => <span className="ip-address">{value}</span>
    },
    {
      key: 'createdAt',
      label: 'Data i Czas',
      width: '180px',
      render: (value) => new Date(value).toLocaleString('pl-PL')
    }
  ];

  return (
    <div className="audit-logs-content">
      <h2>📋 Historia Działań</h2>

      {error && <div className="error-banner">{error}</div>}

      <div className="logs-info">
        <p>📌 Tutaj znajdziesz wszystkie akcje podejmowane przez administratorów</p>
      </div>

      <DataTable
        columns={columns}
        data={logs}
        totalCount={totalCount}
        currentPage={currentPage}
        totalPages={totalPages}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        loading={loading}
      />
    </div>
  );
};
