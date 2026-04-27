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
   * Fetch only PENDING admin requests (awaiting approval)
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
   * Fetch ALL admin requests (pending + approved + rejected)
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
   * Approve a request
   */
  const approveRequest = useCallback(async (requestId: string, comment?: string) => {
    try {
      setLoading(true);
      setError(null);
      
      await httpClient.fetch<AdminRequest>({
        url: API_CONFIG.endpoints.adminRequests.approve(requestId),
        method: 'PATCH',
        body: JSON.stringify({ comment }),
      });
      // Don't auto-refetch - let component decide what to refetch
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to approve request';
      setError(message);
      console.error('Error approving request:', message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  /**
   * Reject a request
   */
  const rejectRequest = useCallback(async (requestId: string, comment: string) => {
    try {
      setLoading(true);
      setError(null);

      if (!comment || comment.length < 10) {
        throw new Error('Reason must be at least 10 characters long');
      }

      await httpClient.fetch<AdminRequest>({
        url: API_CONFIG.endpoints.adminRequests.reject(requestId),
        method: 'PATCH',
        body: JSON.stringify({ comment }),
      });
      // Don't auto-refetch - let component decide what to refetch
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to reject request';
      setError(message);
      console.error('Error rejecting request:', message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  // DON'T fetch on mount - let components decide what to fetch
  // Dashboard will use fetchAllRequests
  // ApprovalsContent will use fetchPendingRequests
  useEffect(() => {
    // Initially fetch nothing, components are responsible for fetching
  }, []);

  /**
   * Get single request by ID
   */
  const getRequestById = useCallback(async (requestId: string) => {
    try {
      const response = await httpClient.fetch<AdminRequest>({
        url: API_CONFIG.endpoints.adminRequests.byId(requestId),
        method: 'GET',
      });
      return response;
    } catch (err) {
      console.error('Error fetching request by ID:', err);
      return null;
    }
  }, []);

  /**
   * Add comment to request (stub - can be extended later)
   */
  const addComment = useCallback(async (requestId: string, text: string) => {
    console.log(`Comment added to ${requestId}: ${text}`);
    // Extended later if backend supports it
  }, []);

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
    refetch: fetchPendingRequests, // Refetch pending by default
    fetchPendingRequests,
    fetchAllRequests,
    getRequestById,
    addComment,
  };
};
