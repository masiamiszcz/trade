import { useState, useCallback } from 'react';

export interface AdminAuditLog {
  id: string;
  adminId: string;
  adminUserName: string;
  action: string;
  ipAddress: string;
  userAgent: string;
  createdAtUtc: string;
  details?: string;
}

interface UseGetAdminAuditLogsReturn {
  auditLogs: AdminAuditLog[];
  loading: boolean;
  error: string | null;
  fetchAdminAuditLogs: () => Promise<void>;
}

/**
 * Hook for fetching and managing admin action audit logs
 * Requires Admin role authorization
 */
export const useGetAdminAuditLogs = (): UseGetAdminAuditLogsReturn => {
  const [auditLogs, setAuditLogs] = useState<AdminAuditLog[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchAdminAuditLogs = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      // Get token from localStorage
      const tokenData = localStorage.getItem('auth-token');
      if (!tokenData) {
        setError('No authentication token found');
        setLoading(false);
        return;
      }

      const token = JSON.parse(tokenData);
      const authToken = token.token || token;

      const response = await fetch('/api/admin/audit-history', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authToken}`,
        },
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ error: 'Unknown error' }));
        setError(errorData?.error || `Error: ${response.status}`);
        setAuditLogs([]);
        return;
      }

      const data = await response.json();
      setAuditLogs(Array.isArray(data) ? data : []);
      setError(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch audit logs';
      setError(message);
      setAuditLogs([]);
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    auditLogs,
    loading,
    error,
    fetchAdminAuditLogs,
  };
};
