import { useCallback, useEffect, useRef, useState } from 'react';
import MarketDataService from '../services/MarketDataService';
import type { CfdInstrumentSummary } from '../services/MarketDataService';

export interface UseCfdInstrumentsResult {
  instruments: CfdInstrumentSummary[];
  loading: boolean;
  error: Error | null;
  refetch: () => Promise<void>;
}

export function useCfdInstruments(): UseCfdInstrumentsResult {
  const [instruments, setInstruments] = useState<CfdInstrumentSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const requestInProgressRef = useRef(false);

  const fetchInstruments = useCallback(async (background = false) => {
    if (requestInProgressRef.current) return;
    requestInProgressRef.current = true;
    if (!background) setLoading(true);
    setError(null);

    try {
      setInstruments(await MarketDataService.getCfdInstruments());
    } catch (fetchError) {
      setError(fetchError instanceof Error ? fetchError : new Error(String(fetchError)));
      if (!background) setInstruments([]);
    } finally {
      requestInProgressRef.current = false;
      if (!background) setLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchInstruments();
    const refreshInterval = window.setInterval(() => {
      void fetchInstruments(true);
    }, 15_000);

    return () => window.clearInterval(refreshInterval);
  }, [fetchInstruments]);

  return { instruments, loading, error, refetch: () => fetchInstruments() };
}
