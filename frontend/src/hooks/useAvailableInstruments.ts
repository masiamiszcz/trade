/**
 * Hook: useAvailableInstruments
 * 
 * Fetches available instruments from the backend API (/api/instruments/active)
 * Provides:
 * - Loading state
 * - Error handling
 * - Automatic data refresh
 * - Filtering by instrument type (Stock, Crypto, Cfd, etc.)
 * 
 * Usage:
 * const { allInstruments, stocks, crypto, cfd, loading, error, refetch } = useAvailableInstruments();
 */

import { useState, useEffect, useCallback } from 'react';
import { marketDataService } from '../services/MarketDataService';
import type { Instrument, InstrumentType } from '../types/admin';

export interface UseAvailableInstrumentsResult {
  // All instruments (unfiltered)
  allInstruments: Instrument[];
  
  // Filtered by type
  stocks: Instrument[];
  crypto: Instrument[];
  cfd: Instrument[];
  etf: Instrument[];
  forex: Instrument[];
  
  // State
  loading: boolean;
  error: Error | null;
  
  // Actions
  refetch: () => Promise<void>;
}

/**
 * Fetch and filter available instruments by type
 */
export const useAvailableInstruments = (): UseAvailableInstrumentsResult => {
  const [allInstruments, setAllInstruments] = useState<Instrument[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  /**
   * Filter instruments by type
   */
  const filterByType = useCallback((instruments: Instrument[], type: InstrumentType): Instrument[] => {
    return instruments.filter((inst) => inst.type === type);
  }, []);

  /**
   * Fetch available instruments from backend
   */
  const fetchInstruments = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await marketDataService.getAvailableInstruments();
      setAllInstruments(data || []);
    } catch (err) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj);
      console.error('Failed to fetch available instruments:', errorObj.message);
      setAllInstruments([]);
    } finally {
      setLoading(false);
    }
  }, []);

  /**
   * Fetch on mount
   */
  useEffect(() => {
    fetchInstruments();
  }, [fetchInstruments]);

  /**
   * Export filtered lists
   */
  return {
    allInstruments,
    stocks: filterByType(allInstruments, 'Stock'),
    crypto: filterByType(allInstruments, 'Crypto'),
    cfd: filterByType(allInstruments, 'Cfd'),
    etf: filterByType(allInstruments, 'Etf'),
    forex: filterByType(allInstruments, 'Forex'),
    loading,
    error,
    refetch: fetchInstruments,
  };
};
