import { useCallback, useEffect, useState } from 'react';
import { httpClient } from '../services/http/HttpClient';
import { API_CONFIG } from '../config/apiConfig';

export interface ExchangeRate {
  baseCurrency: string;
  quoteCurrency: string;
  rate: number;
}

export interface UseExchangeRateResult {
  rate: number | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useExchangeRate(from: string, to: string): UseExchangeRateResult {
  const [rate, setRate] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchRate = useCallback(async () => {
    if (!from || !to) {
      setRate(null);
      setError(null);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await httpClient.fetch<ExchangeRate>({
        url: API_CONFIG.endpoints.fiat.rate(`${from.toLowerCase()}${to.toLowerCase()}`),
        method: 'GET',
      });
      setRate(response.rate);
    } catch (fetchError) {
      setError(fetchError instanceof Error ? fetchError.message : 'Unable to load exchange rate');
      setRate(null);
    } finally {
      setLoading(false);
    }
  }, [from, to]);

  useEffect(() => {
    fetchRate();
  }, [fetchRate]);

  const refetch = useCallback(() => {
    fetchRate();
  }, [fetchRate]);

  return {
    rate,
    loading,
    error,
    refetch,
  };
}