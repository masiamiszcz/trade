import { useState, useCallback, useEffect } from 'react';
import { httpClient } from '../../services/http/HttpClient';
import { useAdminAuth } from './useAdminAuth';
import { AuditLog } from '../../types/admin';

export const useAuditLogs = () => {
  const { token } = useAdminAuth();
  const [logs, setLogs] = useState<AuditLog[]>([])
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(10);

  const page = currentPage;
  const setPage = setCurrentPage;

  // Fetch audit logs from API
  const fetchLogs = useCallback(async () => {
    if (!token) {
      setError('No authentication token found');
      setLoading(false);
      return;
    }

    console.log('🔍 fetchLogs called - currentPage:', currentPage, 'pageSize:', pageSize);
    setLoading(true);
    setError(null);

    try {
      console.log('📡 Fetching: /admin/audit-history');
      const data = await httpClient.fetch<AuditLog[]>({
        url: `/admin/audit-history?page=${currentPage}&pageSize=${pageSize}`,
        method: 'GET',
      });

      console.log('✅ Data received:', Array.isArray(data) ? data.length : 0, 'records');
      setLogs(Array.isArray(data) ? data : []);
      setError(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch audit logs';
      console.log('💥 Error:', message);
      setError(message);
      setLogs([]);
    } finally {
      setLoading(false);
    }
  }, [token, currentPage, pageSize]);

  // Fetch on component mount and when page changes
  useEffect(() => {
    console.log('🎯 useEffect triggered - calling fetchLogs');
    fetchLogs();
    
    // ✅ AUTO-REFRESH: Fetch every 10 seconds for FRESH data
    const refreshInterval = setInterval(() => {
      console.log('🔄 Auto-refresh triggered');
      fetchLogs();
    }, 10000); // 10 seconds
    
    return () => clearInterval(refreshInterval);
  }, [fetchLogs]);

  return {
    logs,
    totalCount,
    currentPage: page,
    totalPages,
    loading,
    error,
    page,
    pageSize,
    setPage,
    setPageSize,
  };
};
