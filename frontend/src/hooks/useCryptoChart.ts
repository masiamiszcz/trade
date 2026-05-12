import { useCallback, useEffect, useMemo, useState } from 'react';
import { CryptoCandle } from '../types/crypto';
import MarketDataService from '../services/MarketDataService';
import { useAccount } from './useAccount';
import { useInstrument } from './useInstrument';
import { useExchangeRate } from './useExchangeRate';

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
  adjustedSymbol: string | undefined;
  refetch: () => void;
  updateLatestCandle: (candle: CryptoCandle) => void;
  exchangeRate: number | null;
  exchangeFromCurrency: string | undefined;
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

  const { account } = useAccount();
  const { instrument } = useInstrument(symbol);
  const userBaseCurrency = account?.currency;
  const instrumentQuoteCurrency = instrument?.quoteCurrency;
  const exchangeFromCurrency = instrumentQuoteCurrency === 'USDT'
    ? 'USD'
    : instrumentQuoteCurrency ?? 'USD';

  const { rate: exchangeRate } = useExchangeRate(
    exchangeFromCurrency,
    userBaseCurrency || ''
  );

  const fetchChart = useCallback(async () => {
    if (!symbol) {
      setCandles([]);
      setError(null);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await MarketDataService.getChartCandles(symbol, rangeMinutes, intervalMinutes, null, userBaseCurrency);
      let processedCandles = response ?? [];

      // Apply currency conversion if needed
      if (userBaseCurrency && exchangeFromCurrency && userBaseCurrency !== exchangeFromCurrency && exchangeRate) {
        processedCandles = processedCandles.map(candle => ({
          ...candle,
          open: candle.open * exchangeRate,
          high: candle.high * exchangeRate,
          low: candle.low * exchangeRate,
          close: candle.close * exchangeRate,
        }));
      }

      setCandles(processedCandles);
    } catch (fetchError) {
      setError(fetchError instanceof Error ? fetchError.message : 'Unable to load chart data');
      setCandles([]);
    } finally {
      setLoading(false);
    }
  }, [rangeMinutes, symbol, intervalMinutes, userBaseCurrency, exchangeRate]);

  const updateLatestCandle = useCallback((candle: CryptoCandle) => {
    const convertedCandle = (userBaseCurrency && exchangeFromCurrency && userBaseCurrency !== exchangeFromCurrency && exchangeRate)
      ? {
          ...candle,
          open: candle.open * exchangeRate,
          high: candle.high * exchangeRate,
          low: candle.low * exchangeRate,
          close: candle.close * exchangeRate,
        }
      : candle;

    setCandles((previous) => {
      if (previous.length === 0) {
        return [convertedCandle];
      }

      const maxCandles = Math.max(Math.ceil(rangeMinutes / Math.max(intervalMinutes ?? 1, 1)), 1);
      const last = previous[previous.length - 1];
      if (last.openTime === convertedCandle.openTime) {
        const updated = [...previous.slice(0, -1), convertedCandle];
        return updated.length > maxCandles ? updated.slice(updated.length - maxCandles) : updated;
      }

      const next = [...previous, convertedCandle];
      return next.length > maxCandles ? next.slice(next.length - maxCandles) : next;
    });
  }, [rangeMinutes, intervalMinutes, exchangeFromCurrency, exchangeRate, userBaseCurrency]);

  useEffect(() => {
    fetchChart();
  }, [fetchChart]);

  const refetch = useCallback(() => {
    fetchChart();
  }, [fetchChart]);

  const adjustedSymbol = useMemo(() => {
    if (!symbol || !userBaseCurrency || !exchangeFromCurrency || userBaseCurrency === exchangeFromCurrency) {
      return symbol;
    }

    return symbol?.replace(new RegExp(exchangeFromCurrency, 'i'), userBaseCurrency.toUpperCase()) || symbol;
  }, [symbol, userBaseCurrency, exchangeFromCurrency]);

  const interval = useMemo(() => {
    if (candles.length > 0 && candles[0].interval) {
      return candles[0].interval;
    }
    // Default to 1h if no interval from data
    if (intervalMinutes) {
      if (intervalMinutes >= 1440) return `${intervalMinutes / 1440}d`; // days
      if (intervalMinutes >= 60) return `${intervalMinutes / 60}h`; // hours
      return `${intervalMinutes}m`; // minutes
    }
    return '1h'; // fallback default
  }, [candles, intervalMinutes]);

  return useMemo(
    () => ({
      candles,
      loading,
      error,
      source,
      interval,
      adjustedSymbol,
      refetch,
      updateLatestCandle,
      exchangeRate,
      exchangeFromCurrency,
    }),
    [candles, error, exchangeFromCurrency, exchangeRate, interval, loading, refetch, source, updateLatestCandle, adjustedSymbol],
  );
}
