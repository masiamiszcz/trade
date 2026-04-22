
import React from 'react';
import { useAuditLogs } from '../../../hooks/admin/useAuditLogs';
import { AuditLog } from '../../../types/admin';
import { DataTable, Column } from '../../common/DataTable';
import './AuditLogsContent.css';

export const AuditLogsContent: React.FC = () => {
  const {
    logs,
    loading,
    error,
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
      render: (value) => {
        try {
          return <span className="action-badge">{value || '-'}</span>;
        } catch (e) {
          return <span>-</span>;
        }
      }
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
      render: (value) => {
        try {
          const strValue = String(value || '');
          return <span className="entity-id">{strValue.substring(0, 12)}{strValue.length > 12 ? '...' : ''}</span>;
        } catch (e) {
          return <span>-</span>;
        }
      }
    },
    {
      key: 'ipAddress',
      label: 'Adres IP',
      width: '130px',
      render: (value) => {
        try {
          return <span className="ip-address">{value || '-'}</span>;
        } catch (e) {
          return <span>-</span>;
        }
      }
    },
    {
      key: 'createdAt',
      label: 'Data i Czas',
      width: '180px',
      render: (value) => {
        try {
          if (!value) return <span>-</span>;
          const date = new Date(value);
          if (isNaN(date.getTime())) return <span>-</span>;
          return <span>{date.toLocaleString('pl-PL')}</span>;
        } catch (e) {
          return <span>-</span>;
        }
      }
    }
  ];

  // ✅ SAFE KEY EXTRACTOR
  const keyExtractor = (item: AuditLog) => {
    try {
      return item.id || String(item.entityId) || Math.random();
    } catch {
      return Math.random();
    }
  };

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
        keyExtractor={keyExtractor}
        loading={loading}
        emptyMessage="Brak danych w dzienniku działań"
      />
    </div>
  );
};
