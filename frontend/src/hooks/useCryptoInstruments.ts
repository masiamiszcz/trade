import { useState, useEffect, useCallback } from 'react';
import { marketDataService } from '../services/MarketDataService';
import type { Instrument } from '../types/admin';

export interface UseCryptoInstrumentsResult {
  crypto: Instrument[];
  loading: boolean;
  error: Error | null;
  refetch: () => Promise<void>;
}

export const useCryptoInstruments = (): UseCryptoInstrumentsResult => {
  const [crypto, setCrypto] = useState<Instrument[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchCrypto = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await marketDataService.getCryptoInstruments();
      setCrypto(data || []);
    } catch (err) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj);
      setCrypto([]);
      console.error('Failed to fetch available crypto instruments:', errorObj.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchCrypto();
  }, [fetchCrypto]);

  return {
    crypto,
    loading,
    error,
    refetch: fetchCrypto,
  };
};
