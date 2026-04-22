import { useState, useCallback, useEffect } from 'react';
import { AuditLog } from '../../types/admin';

// ✅ UNIFIED: Read from same storage key as AdminAuthContext (trading-admin-session)
const ADMIN_SESSION_KEY = 'trading-admin-session';

function getAdminToken(): string | null {
  try {
    const session = localStorage.getItem(ADMIN_SESSION_KEY);
    if (!session) {
      console.log('❌ [useAuditLogs] No session in localStorage');
      return null;
    }
    const parsed = JSON.parse(session);
    return parsed.token || null;
  } catch (e) {
    console.error('❌ [useAuditLogs] Failed to parse session:', e);
    return null;
  }
}

export const useAuditLogs = () => {
  const [logs, setLogs] = useState<AuditLog[]>([]);
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
    console.log('🔍 fetchLogs called - currentPage:', currentPage, 'pageSize:', pageSize);
    setLoading(true);
    setError(null);

    try {
      const authToken = getAdminToken();
      console.log('🔐 Token from trading-admin-session:', authToken ? 'EXISTS' : 'NULL');
      
      if (!authToken) {
        console.log('❌ No token found - setting error');
        setError('No authentication token found');
        setLoading(false);
        return;
      }

      console.log('📡 Fetching: /api/admin/audit-history');
      const response = await fetch(
        `/api/admin/audit-history?page=${currentPage}&pageSize=${pageSize}`,
        {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${authToken}`,
          },
        }
      );

      console.log('📨 Response status:', response.status);
      
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ error: 'Unknown error' }));
        setError(errorData?.error || `Error: ${response.status}`);
        setLogs([]);
        return;
      }

      const data = await response.json();
      console.log('✅ Data received:', data.length, 'records');
      
      // ✅ MAP response fields to AuditLog type
      const mappedData = data.map((item: any) => ({
        id: item.id,
        adminId: item.adminId,
        adminName: item.adminUserName || 'Unknown',  // Map adminUserName -> adminName
        action: item.action,
        entityType: item.details || 'System',  // Use details as entityType
        entityId: item.adminId,  // Use adminId as fallback
        details: item.details ? { description: item.details } : {},
        ipAddress: item.ipAddress,
        createdAt: item.createdAtUtc  // Map createdAtUtc -> createdAt
      }));
      
      setLogs(Array.isArray(mappedData) ? mappedData : []);
      setError(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch audit logs';
      console.log('💥 Error:', message);
      setError(message);
      setLogs([]);
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize]);

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
