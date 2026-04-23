import { useState, useCallback } from 'react';
import { httpClient } from '../../services/http/HttpClient';
import { API_CONFIG } from '../../config/apiConfig';

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
      const data = await httpClient.fetch<AdminAuditLog[]>({
        url: API_CONFIG.endpoints.adminAudit.history,
        method: 'GET',
      });

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
