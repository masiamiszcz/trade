import { useState, useEffect, useCallback } from 'react';
import { marketDataService } from '../services/MarketDataService';
import type { Instrument } from '../types/admin';

export interface UseInstrumentResult {
  instrument: Instrument | null;
  loading: boolean;
  error: Error | null;
  refetch: () => Promise<void>;
}

export const useInstrument = (symbol: string | undefined): UseInstrumentResult => {
  const [instrument, setInstrument] = useState<Instrument | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const fetchInstrument = useCallback(async () => {
    if (!symbol) {
      setInstrument(null);
      setError(null);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const instruments = await marketDataService.getAvailableInstruments();
      const found = instruments.find(inst => inst.symbol.toLowerCase() === symbol.toLowerCase());
      setInstrument(found || null);
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Failed to fetch instrument'));
      setInstrument(null);
    } finally {
      setLoading(false);
    }
  }, [symbol]);

  useEffect(() => {
    fetchInstrument();
  }, [fetchInstrument]);

  const refetch = useCallback(async () => {
    await fetchInstrument();
  }, [fetchInstrument]);

  return {
    instrument,
    loading,
    error,
    refetch,
  };
};