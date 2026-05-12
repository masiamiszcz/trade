
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
  const [selectedLog, setSelectedLog] = React.useState<AuditLog | null>(null);

  const columns: Column<AuditLog>[] = [
    {
      key: 'adminUserName',
      label: 'Administrator',
      width: '120px',
      render: (value, item) => {
        const displayName = value || (item as any).adminName || '-';
        return <span>{displayName}</span>;
      }
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
      key: 'createdAtUtc',
      label: 'Data i Czas',
      width: '180px',
      render: (value, item) => {
        try {
          const dateString = value || (item as any).createdAt;
          if (!dateString) return <span>-</span>;
          const date = new Date(dateString);
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
      return (
        item.id ||
        item.entityId ||
        `${item.action}-${item.createdAtUtc || item.createdAt || 'unknown'}` ||
        Math.random()
      );
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
        <p className="audit-log-hint">Kliknij w wiersz, aby zobaczyć pełne szczegóły logu.</p>
      </div>

      <DataTable
        columns={columns}
        data={logs}
        keyExtractor={keyExtractor}
        loading={loading}
        emptyMessage="Brak danych w dzienniku działań"
        onRowClick={(item) => setSelectedLog(item)}
      />

      {selectedLog && (
        <div className="audit-log-modal-overlay" onClick={() => setSelectedLog(null)}>
          <div className="audit-log-modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="audit-log-modal-header">
              <div>
                <h3>Szczegóły logu</h3>
                <p>{selectedLog.action}</p>
              </div>
              <button className="modal-close-btn" onClick={() => setSelectedLog(null)}>✕</button>
            </div>
            <div className="audit-log-modal-body">
              <div className="audit-log-detail-row">
                <span className="label">ID:</span>
                <span>{selectedLog.id}</span>
              </div>
              <div className="audit-log-detail-row">
                <span className="label">Admin:</span>
                <span>{selectedLog.adminUserName || selectedLog.adminId}</span>
              </div>
              <div className="audit-log-detail-row">
                <span className="label">IP:</span>
                <span>{selectedLog.ipAddress || 'N/A'}</span>
              </div>
              <div className="audit-log-detail-row">
                <span className="label">Data:</span>
                <span>{new Date(selectedLog.createdAtUtc || '').toLocaleString('pl-PL')}</span>
              </div>
              <div className="audit-log-detail-row">
                <span className="label">User Agent:</span>
                <span>{selectedLog.userAgent || 'Brak'}</span>
              </div>
              <div className="audit-log-detail-block">
                <span className="label">Szczegóły:</span>
                <pre className="details-pre">{selectedLog.details || 'Brak szczegółów'}</pre>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
