import React, { useEffect, useState } from 'react';
import { useGetAdminAuditLogs, AdminAuditLog } from '../../hooks/admin/useGetAdminAuditLogs.ts';
import './AdminAuditLogsContent.css';

interface SortConfig {
  key: keyof AdminAuditLog;
  direction: 'asc' | 'desc';
}

/**
 * Component displaying admin action audit logs in a table
 * Shows history of all admin actions with timestamps, IPs, and details
 */
const AdminAuditLogsContent: React.FC = () => {
  const { auditLogs, loading, error, fetchAdminAuditLogs } = useGetAdminAuditLogs();
  const [filteredLogs, setFilteredLogs] = useState<AdminAuditLog[]>([]);
  const [sortConfig, setSortConfig] = useState<SortConfig>({
    key: 'createdAtUtc',
    direction: 'desc',
  });
  const [filterAction, setFilterAction] = useState('');
  const [filterAdmin, setFilterAdmin] = useState('');

  // Fetch logs on component mount
  useEffect(() => {
    fetchAdminAuditLogs();
  }, [fetchAdminAuditLogs]);

  // Apply filters and sorting
  useEffect(() => {
    let filtered = [...auditLogs];

    // Apply action filter
    if (filterAction) {
      filtered = filtered.filter(log =>
        log.action.toLowerCase().includes(filterAction.toLowerCase())
      );
    }

    // Apply admin filter
    if (filterAdmin) {
      filtered = filtered.filter(log =>
        log.adminUserName.toLowerCase().includes(filterAdmin.toLowerCase())
      );
    }

    // Apply sorting
    filtered.sort((a, b) => {
      const aValue = a[sortConfig.key];
      const bValue = b[sortConfig.key];

      if (aValue === null || aValue === undefined) return 1;
      if (bValue === null || bValue === undefined) return -1;

      if (typeof aValue === 'string' && typeof bValue === 'string') {
        return sortConfig.direction === 'asc'
          ? aValue.localeCompare(bValue)
          : bValue.localeCompare(aValue);
      }

      if (aValue < bValue) {
        return sortConfig.direction === 'asc' ? -1 : 1;
      }
      if (aValue > bValue) {
        return sortConfig.direction === 'asc' ? 1 : -1;
      }
      return 0;
    });

    setFilteredLogs(filtered);
  }, [auditLogs, filterAction, filterAdmin, sortConfig]);

  const handleSort = (key: keyof AdminAuditLog) => {
    setSortConfig(prevConfig => ({
      key,
      direction: prevConfig.key === key && prevConfig.direction === 'asc' ? 'desc' : 'asc',
    }));
  };

  const formatDate = (dateString: string): string => {
    try {
      const date = new Date(dateString);
      return date.toLocaleString();
    } catch {
      return dateString;
    }
  };

  const getActionColor = (action: string): string => {
    if (action.includes('LOGIN') || action.includes('AUTH')) return 'action-auth';
    if (action.includes('APPROVE')) return 'action-approve';
    if (action.includes('REJECT')) return 'action-reject';
    if (action.includes('BLOCK')) return 'action-block';
    if (action.includes('DELETE')) return 'action-delete';
    return 'action-default';
  };

  const SortIcon = ({ columnKey }: { columnKey: keyof AdminAuditLog }) => {
    if (sortConfig.key !== columnKey) {
      return <span className="sort-icon">↕</span>;
    }
    return (
      <span className="sort-icon">
        {sortConfig.direction === 'asc' ? '↑' : '↓'}
      </span>
    );
  };

  if (loading) {
    return <div className="audit-logs-loading">Loading audit history...</div>;
  }

  if (error) {
    return (
      <div className="audit-logs-error">
        <p>Error loading audit history: {error}</p>
        <button onClick={fetchAdminAuditLogs} className="retry-button">
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="audit-logs-container">
      <div className="audit-logs-header">
        <h2>Admin Action History</h2>
        <p className="total-count">Total: {filteredLogs.length} actions</p>
      </div>

      <div className="filters-section">
        <div className="filter-group">
          <label htmlFor="filter-action">Filter by Action:</label>
          <input
            id="filter-action"
            type="text"
            placeholder="e.g., APPROVE, BLOCK, LOGIN..."
            value={filterAction}
            onChange={e => setFilterAction(e.target.value)}
            className="filter-input"
          />
        </div>

        <div className="filter-group">
          <label htmlFor="filter-admin">Filter by Admin:</label>
          <input
            id="filter-admin"
            type="text"
            placeholder="e.g., superadmin..."
            value={filterAdmin}
            onChange={e => setFilterAdmin(e.target.value)}
            className="filter-input"
          />
        </div>

        {(filterAction || filterAdmin) && (
          <button
            onClick={() => {
              setFilterAction('');
              setFilterAdmin('');
            }}
            className="clear-filters-button"
          >
            Clear Filters
          </button>
        )}
      </div>

      {filteredLogs.length === 0 ? (
        <div className="no-logs">
          <p>No audit logs found</p>
        </div>
      ) : (
        <div className="table-wrapper">
          <table className="audit-logs-table">
            <thead>
              <tr>
                <th onClick={() => handleSort('createdAtUtc')} className="sortable">
                  Timestamp <SortIcon columnKey="createdAtUtc" />
                </th>
                <th onClick={() => handleSort('adminUserName')} className="sortable">
                  Admin <SortIcon columnKey="adminUserName" />
                </th>
                <th onClick={() => handleSort('action')} className="sortable">
                  Action <SortIcon columnKey="action" />
                </th>
                <th onClick={() => handleSort('ipAddress')} className="sortable">
                  IP Address <SortIcon columnKey="ipAddress" />
                </th>
                <th className="details-col">Details</th>
              </tr>
            </thead>
            <tbody>
              {filteredLogs.map(log => (
                <tr key={log.id} className="log-row">
                  <td className="timestamp">{formatDate(log.createdAtUtc)}</td>
                  <td className="admin-name">{log.adminUserName}</td>
                  <td className={`action ${getActionColor(log.action)}`}>
                    {log.action}
                  </td>
                  <td className="ip-address">{log.ipAddress}</td>
                  <td className="details-cell">
                    {log.details && (
                      <details className="details-element">
                        <summary>View</summary>
                        <pre className="details-content">{log.details}</pre>
                      </details>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default AdminAuditLogsContent;
