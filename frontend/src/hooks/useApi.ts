import { useState, useEffect, useCallback, useRef } from 'react';
import { ApiResponse, ApiError } from '../types';

interface UseApiState<T> {
  data: T | null;
  loading: boolean;
  error: ApiError | null;
}

export function useApi<T>(apiCall: () => Promise<ApiResponse<T>>) {
  const [state, setState] = useState<UseApiState<T>>({
    data: null,
    loading: false,
    error: null,
  });

  const apiCallRef = useRef(apiCall);
  useEffect(() => {
    apiCallRef.current = apiCall;
  }, [apiCall]);

  const execute = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }));

    try {
      const response = await apiCallRef.current();

      if (response.error) {
        setState(prev => ({
          ...prev,
          loading: false,
          error: response.error || null,
        }));
      } else {
        setState(prev => ({
          ...prev,
          loading: false,
          data: response.data || null,
        }));
      }
    } catch (error) {
      setState(prev => ({
        ...prev,
        loading: false,
        error: {
          message: error instanceof Error ? error.message : 'Unknown error',
        },
      }));
    }
  }, []);

  useEffect(() => {
    execute();
  }, [execute]);

  return {
    ...state,
    refetch: execute,
  };
}