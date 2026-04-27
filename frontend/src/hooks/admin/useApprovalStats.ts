import { useState, useEffect, useCallback } from 'react';
import { httpClient } from '../../services/http/HttpClient';
import { API_CONFIG } from '../../config/apiConfig';
import type { AdminRequest } from '../../types/admin';

/**
 * DTO for approval statistics
 */
export interface ApprovalStatistics {
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
}

/**
 * Hook to fetch approval statistics for dashboard
 * Fetches all admin requests and counts by status
 * Returns counts of pending, approved, and rejected requests
 */
export const useApprovalStats = () => {
  const [stats, setStats] = useState<ApprovalStatistics>({
    pendingCount: 0,
    approvedCount: 0,
    rejectedCount: 0,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /**
   * Fetch all admin requests and calculate statistics
   * Endpoint: GET /api/admin/approvals (istniejący!)
   */
  const fetchStatistics = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      
      // Fetchuj ISTNIEJĄCY endpoint: /api/admin/approvals (všechne requesty)
      const response = await httpClient.fetch<AdminRequest[]>({
        url: API_CONFIG.endpoints.adminRequests.all,
        method: 'GET',
      });

      if (response && Array.isArray(response)) {
        // Liczymy na froncie!
        const pendingCount = response.filter((r) => r.status === 'Pending').length;
        const approvedCount = response.filter((r) => r.status === 'Approved').length;
        const rejectedCount = response.filter((r) => r.status === 'Rejected').length;

        setStats({
          pendingCount,
          approvedCount,
          rejectedCount,
        });
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch approval statistics';
      setError(message);
      console.error('Error fetching approval statistics:', message);
      
      // Set default values on error
      setStats({
        pendingCount: 0,
        approvedCount: 0,
        rejectedCount: 0,
      });
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch statistics on component mount
  useEffect(() => {
    fetchStatistics();
    
    // Refresh stats every 60 seconds
    const interval = setInterval(fetchStatistics, 60000);
    return () => clearInterval(interval);
  }, [fetchStatistics]);

  return {
    stats,
    loading,
    error,
    refetch: fetchStatistics,
  };
};

