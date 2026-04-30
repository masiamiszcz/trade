import { useCallback, useEffect, useMemo, useState } from 'react';
import { CryptoCandle } from '../types/crypto';
import MarketDataService from '../services/MarketDataService';

const rangeSourceMap: Record<number, string> = {
  [12 * 60]: 'Binance',
  [24 * 60]: 'Binance',
  [7 * 24 * 60]: 'Binance',
  [14 * 24 * 60]: 'Binance',
  [30 * 24 * 60]: 'Binance',
  [365 * 24 * 60]: 'Binance',
  [20 * 365 * 24 * 60]: 'CoinGecko',
};

export interface UseCryptoChartResult {
  candles: CryptoCandle[];
  loading: boolean;
  error: string | null;
  source: string;
  interval: string;
  refetch: () => void;
  updateLatestCandle: (candle: CryptoCandle) => void;
}

export function useCryptoChart(
  symbol: string | undefined,
  rangeMinutes: number,
  intervalMinutes?: number,
): UseCryptoChartResult {
  const [candles, setCandles] = useState<CryptoCandle[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const source = rangeSourceMap[rangeMinutes] ?? 'Binance';

  const fetchChart = useCallback(async () => {
    if (!symbol) {
      setCandles([]);
      setError(null);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await MarketDataService.getChartCandles(symbol, rangeMinutes, intervalMinutes);
      setCandles(response ?? []);
    } catch (fetchError) {
      setError(fetchError instanceof Error ? fetchError.message : 'Unable to load chart data');
      setCandles([]);
    } finally {
      setLoading(false);
    }
  }, [rangeMinutes, symbol, intervalMinutes]);

  const updateLatestCandle = useCallback((candle: CryptoCandle) => {
    setCandles((previous) => {
      if (previous.length === 0) {
        return [candle];
      }

      const maxCandles = Math.max(Math.ceil(rangeMinutes / Math.max(intervalMinutes ?? 1, 1)), 1);
      const last = previous[previous.length - 1];
      if (last.openTime === candle.openTime) {
        const updated = [...previous.slice(0, -1), candle];
        return updated.length > maxCandles ? updated.slice(updated.length - maxCandles) : updated;
      }

      const next = [...previous, candle];
      return next.length > maxCandles ? next.slice(next.length - maxCandles) : next;
    });
  }, [rangeMinutes, intervalMinutes]);

  useEffect(() => {
    fetchChart();
  }, [fetchChart]);

  const refetch = useCallback(() => {
    fetchChart();
  }, [fetchChart]);

  const interval = useMemo(() => {
    if (candles.length === 0) return '';
    return candles[0].interval ?? '';
  }, [candles]);

  return useMemo(
    () => ({
      candles,
      loading,
      error,
      source,
      interval,
      refetch,
      updateLatestCandle,
    }),
    [candles, error, interval, loading, refetch, source, updateLatestCandle],
  );
}
