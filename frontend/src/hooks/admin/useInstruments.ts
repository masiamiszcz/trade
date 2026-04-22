import { useState, useCallback } from 'react';
import {
  Instrument,
  CreateInstrumentRequest,
  UpdateInstrumentRequest,
  RejectInstrumentRequest,
} from '../../types/admin';
import { instrumentsService } from '../../services/admin/instrumentsService';

/**
 * USEИНСТРУMENTS HOOK
 * 
 * Responsibilities:
 * - State management (instruments list, pagination, loading, errors)
 * - Orchestration of API calls via instrumentsService
 * - Loading/error state handling
 * - NO domain logic, NO business rules
 * 
 * All validation happens on backend.
 * Hook just propagates errors to UI.
 */

export const useInstruments = () => {
  // ============ STATE ============
  const [instruments, setInstruments] = useState<Instrument[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(10);

  // Aliases for compatibility
  const page = currentPage;
  const setPage = setCurrentPage;

  // ============ HELPER: ERROR HANDLER ============
  const handleError = useCallback((err: unknown, context: string) => {
    console.error(`[useInstruments] ${context}:`, err);
    
    if (err instanceof Error) {
      setError(err.message);
    } else if (typeof err === 'object' && err !== null && 'response' in err) {
      // Axios error
      const axiosErr = err as any;
      const message = axiosErr.response?.data?.error || axiosErr.message || 'An error occurred';
      setError(message);
    } else {
      setError('An unexpected error occurred');
    }
  }, []);

  // ============ FETCH INSTRUMENTS ============
  const fetchInstruments = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      
      const instruments = await instrumentsService.getAll();
      setInstruments(instruments);
      
      // Simple pagination: divide by pageSize
      const total = instruments.length;
      setTotalCount(total);
      setTotalPages(Math.ceil(total / pageSize));
    } catch (err) {
      handleError(err, 'fetchInstruments');
    } finally {
      setLoading(false);
    }
  }, [pageSize, handleError]);

  // ============ CREATE INSTRUMENT ============
  const createInstrument = useCallback(
    async (data: CreateInstrumentRequest): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const created = await instrumentsService.create(data);
        
        // Add to list (optimistic update)
        setInstruments(prev => [created, ...prev]);
        setTotalCount(prev => prev + 1);
        
        return created;
      } catch (err) {
        handleError(err, 'createInstrument');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ UPDATE INSTRUMENT ============
  const updateInstrument = useCallback(
    async (id: string, data: UpdateInstrumentRequest): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const updated = await instrumentsService.update(id, data);
        
        // Update in list (optimistic update)
        setInstruments(prev =>
          prev.map(inst => (inst.id === id ? updated : inst))
        );
        
        return updated;
      } catch (err) {
        handleError(err, 'updateInstrument');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ DELETE INSTRUMENT ============
  const deleteInstrument = useCallback(
    async (id: string): Promise<boolean> => {
      try {
        setLoading(true);
        setError(null);
        
        await instrumentsService.delete_(id);
        
        // Remove from list (optimistic update)
        setInstruments(prev => prev.filter(inst => inst.id !== id));
        setTotalCount(prev => prev - 1);
        
        return true;
      } catch (err) {
        handleError(err, 'deleteInstrument');
        return false;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ WORKFLOW: REQUEST APPROVAL ============
  const requestApproval = useCallback(
    async (id: string): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const updated = await instrumentsService.requestApproval(id);
        
        setInstruments(prev =>
          prev.map(inst => (inst.id === id ? updated : inst))
        );
        
        return updated;
      } catch (err) {
        handleError(err, 'requestApproval');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ WORKFLOW: APPROVE ============
  const approveInstrument = useCallback(
    async (id: string): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const updated = await instrumentsService.approve(id);
        
        setInstruments(prev =>
          prev.map(inst => (inst.id === id ? updated : inst))
        );
        
        return updated;
      } catch (err) {
        handleError(err, 'approveInstrument');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ WORKFLOW: REJECT ============
  const rejectInstrument = useCallback(
    async (id: string, request: RejectInstrumentRequest): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const updated = await instrumentsService.reject(id, request);
        
        setInstruments(prev =>
          prev.map(inst => (inst.id === id ? updated : inst))
        );
        
        return updated;
      } catch (err) {
        handleError(err, 'rejectInstrument');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ WORKFLOW: RETRY SUBMISSION ============
  const retrySubmission = useCallback(
    async (id: string): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const updated = await instrumentsService.retrySubmission(id);
        
        setInstruments(prev =>
          prev.map(inst => (inst.id === id ? updated : inst))
        );
        
        return updated;
      } catch (err) {
        handleError(err, 'retrySubmission');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ WORKFLOW: ARCHIVE ============
  const archiveInstrument = useCallback(
    async (id: string): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const updated = await instrumentsService.archive(id);
        
        setInstruments(prev =>
          prev.map(inst => (inst.id === id ? updated : inst))
        );
        
        return updated;
      } catch (err) {
        handleError(err, 'archiveInstrument');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ ADMINISTRATIVE: BLOCK ============
  const blockInstrument = useCallback(
    async (id: string): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const updated = await instrumentsService.block(id);
        
        setInstruments(prev =>
          prev.map(inst => (inst.id === id ? updated : inst))
        );
        
        return updated;
      } catch (err) {
        handleError(err, 'blockInstrument');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ ADMINISTRATIVE: UNBLOCK ============
  const unblockInstrument = useCallback(
    async (id: string): Promise<Instrument | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const updated = await instrumentsService.unblock(id);
        
        setInstruments(prev =>
          prev.map(inst => (inst.id === id ? updated : inst))
        );
        
        return updated;
      } catch (err) {
        handleError(err, 'unblockInstrument');
        return null;
      } finally {
        setLoading(false);
      }
    },
    [handleError]
  );

  // ============ RETURN PUBLIC API ============
  return {
    // State
    instruments,
    totalCount,
    currentPage,
    totalPages,
    loading,
    error,
    pageSize,
    
    // Aliases
    page,
    setPage,
    setPageSize,

    // CRUD
    fetchInstruments,
    createInstrument,
    updateInstrument,
    deleteInstrument,

    // Workflow
    requestApproval,
    approveInstrument,
    rejectInstrument,
    retrySubmission,
    archiveInstrument,

    // Administrative
    blockInstrument,
    unblockInstrument,

    // Manual state control
    setInstruments,
    setError,
    setLoading,
  };
};
