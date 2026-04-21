import { useState } from 'react';

export interface AdminRequest {
  id: string;
  adminName: string;
  action: string;
  status: 'pending' | 'approved' | 'rejected';
  createdAt: string;
}

export const useAdminRequests = () => {
  const [requests, setRequests] = useState<AdminRequest[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(10);

  const approveRequest = async (requestId: string) => {
    console.log(`Approve request ${requestId}`);
  };

  const rejectRequest = async (requestId: string) => {
    console.log(`Reject request ${requestId}`);
  };

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
  };
};
