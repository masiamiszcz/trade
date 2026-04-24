import { useState, useEffect, useCallback } from 'react';
import { httpClient } from '../../services/http/HttpClient';
import { API_CONFIG } from '../../config/apiConfig';
import type { AdminRequest } from '../../types/admin';

export const useAdminRequests = () => {
  const [requests, setRequests] = useState<AdminRequest[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(10);

  /**
   * Fetch pending admin requests
   */
  const fetchPendingRequests = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await httpClient.fetch<AdminRequest[]>({
        url: API_CONFIG.endpoints.adminRequests.pending,
        method: 'GET',
      });
      setRequests(response || []);
      setTotalCount(response?.length || 0);
      setTotalPages(Math.ceil((response?.length || 0) / pageSize));
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch pending requests';
      setError(message);
      console.error('Error fetching pending requests:', message);
      setRequests([]);
    } finally {
      setLoading(false);
    }
  }, [pageSize]);

  /**
   * Approve a request
   */
  const approveRequest = useCallback(async (requestId: string, comment?: string) => {
    try {
      setLoading(true);
      setError(null);
      
      await httpClient.fetch<AdminRequest>({
        url: API_CONFIG.endpoints.adminRequests.approve(requestId),
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: comment ? JSON.stringify({ comment }) : JSON.stringify({}),
      });

      // Refetch to update list (remove approved request from pending)
      await fetchPendingRequests();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to approve request';
      setError(message);
      console.error('Error approving request:', message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, [fetchPendingRequests]);

  /**
   * Reject a request
   */
  const rejectRequest = useCallback(async (requestId: string, reason: string) => {
    try {
      setLoading(true);
      setError(null);

      if (!reason || reason.length < 10) {
        throw new Error('Reason must be at least 10 characters long');
      }

      await httpClient.fetch<AdminRequest>({
        url: API_CONFIG.endpoints.adminRequests.reject(requestId),
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason }),
      });

      // Refetch to update list (remove rejected request from pending)
      await fetchPendingRequests();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to reject request';
      setError(message);
      console.error('Error rejecting request:', message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, [fetchPendingRequests]);

  /**
   * Add comment to a request (without changing status)
   */
  const addComment = useCallback(async (requestId: string, text: string) => {
    try {
      setLoading(true);
      setError(null);

      if (!text || text.trim().length === 0) {
        throw new Error('Comment cannot be empty');
      }

      await httpClient.fetch({
        url: API_CONFIG.endpoints.adminRequests.comment(requestId),
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text }),
      });

      // Refetch to update list with new comment
      await fetchPendingRequests();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to add comment';
      setError(message);
      console.error('Error adding comment:', message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, [fetchPendingRequests]);

  /**
   * Fetch ALL admin requests (all statuses: pending, approved, rejected)
   */
  const fetchAllRequests = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await httpClient.fetch<AdminRequest[]>({
        url: API_CONFIG.endpoints.adminRequests.all,
        method: 'GET',
      });
      setRequests(response || []);
      setTotalCount(response?.length || 0);
      setTotalPages(Math.ceil((response?.length || 0) / pageSize));
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch all requests';
      setError(message);
      console.error('Error fetching all requests:', message);
      setRequests([]);
    } finally {
      setLoading(false);
    }
  }, [pageSize]);

  /**
   * Get single request by ID (detailed view)
   */
  const getRequestById = useCallback(async (requestId: string): Promise<AdminRequest | null> => {
    try {
      setLoading(true);
      setError(null);
      const response = await httpClient.fetch<AdminRequest>({
        url: API_CONFIG.endpoints.adminRequests.byId(requestId),
        method: 'GET',
      });
      return response || null;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch request details';
      setError(message);
      console.error('Error fetching request details:', message);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch pending requests on mount
  useEffect(() => {
    fetchPendingRequests();
  }, [fetchPendingRequests]);

  const page = currentPage;
  const setPage = setCurrentPage;

  return {
    requests,
    totalCount,
    currentPage: page,
    totalPages,
    loading,
    error,
    page,
    pageSize,
    setPage,
    setPageSize,
    approveRequest,
    rejectRequest,
    addComment,
    refetch: fetchPendingRequests,
    fetchPendingRequests,
    fetchAllRequests,
    getRequestById,
  };
};
