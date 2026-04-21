import { useState } from 'react';
import { Instrument } from '../../types/admin';

export const useInstruments = () => {
  const [instruments, setInstruments] = useState<Instrument[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(10);

  const page = currentPage;
  const setPage = setCurrentPage;

  return {
    instruments,
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
