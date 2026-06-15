import { useState, useCallback, useEffect } from 'react';
import { cryptoPortfolioService, PortfolioSummaryDto } from '../services/CryptoPortfolioService';

interface UsePortfolioState {
  data: PortfolioSummaryDto | null;
  loading: boolean;
  error: string | null;
}

export function usePortfolio() {
  const [state, setState] = useState<UsePortfolioState>({
    data: null,
    loading: false,
    error: null,
  });

  const fetch = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }));
    try {
      const portfolio = await cryptoPortfolioService.getPortfolio();
      setState(prev => ({ ...prev, data: portfolio, loading: false }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to fetch portfolio',
        loading: false,
      }));
    }
  }, []);

  useEffect(() => {
    fetch();
  }, [fetch]);

  const refresh = useCallback(() => {
    fetch();
  }, [fetch]);

  return {
    portfolio: state.data,
    loading: state.loading,
    error: state.error,
    refresh,
  };
}
