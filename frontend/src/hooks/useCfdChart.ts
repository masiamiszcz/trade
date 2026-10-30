import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { CryptoCandle } from '../types/crypto';
import type { PriceUpdate } from '../services/SignalRService';
import MarketDataService from '../services/MarketDataService';
import { useAccount } from './useAccount';
import { useExchangeRate } from './useExchangeRate';

const CFD_MARKET_SOURCE = 'CFD market feed';

function getBucketStart(timestamp: number, intervalMinutes: number): number {
  const intervalMs = intervalMinutes * 60_000;
  return Math.floor(timestamp / intervalMs) * intervalMs;
}

export interface UseCfdChartResult {
  candles: CryptoCandle[];
  loading: boolean;
  error: string | null;
  source: string;
  interval: string;
  refetch: () => void;
  updateLatestPrice: (update: PriceUpdate) => void;
  exchangeRate: number | null;
  exchangeFromCurrency: string | undefined;
  displayCurrency: string;
  conversionError: string | null;
}

export function useCfdChart(
  symbol: string | undefined,
  quoteCurrency: string | undefined,
  rangeMinutes: number,
  intervalMinutes: number,
): UseCfdChartResult {
  const [candles, setCandles] = useState<CryptoCandle[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pendingTicksRef = useRef<PriceUpdate[]>([]);
  const loadingRef = useRef(false);
  const requestRef = useRef(0);
  const seenUpdateIdsRef = useRef<string[]>([]);
  const lastTickRef = useRef<PriceUpdate | null>(null);
  const candleTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const { account } = useAccount();
  const userBaseCurrency = account?.currency?.toUpperCase() || '';
  const exchangeFromCurrency = quoteCurrency?.toUpperCase() === 'USDT'
    ? 'USD'
    : quoteCurrency?.toUpperCase();
  const { rate: exchangeRate, error: exchangeRateError } = useExchangeRate(
    exchangeFromCurrency ?? '',
    userBaseCurrency,
  );

  const priceMultiplier = exchangeFromCurrency && userBaseCurrency
    && exchangeFromCurrency !== userBaseCurrency
    ? exchangeRate
    : 1;
  const displayCurrency = priceMultiplier && userBaseCurrency
    ? userBaseCurrency
    : exchangeFromCurrency ?? userBaseCurrency ?? 'USD';
  const conversionError = exchangeRateError
    ?? (exchangeFromCurrency && userBaseCurrency && exchangeFromCurrency !== userBaseCurrency && !exchangeRate
      ? `Nie można przeliczyć ceny z ${exchangeFromCurrency} na ${userBaseCurrency}.`
      : null);

  const convertCandle = useCallback((candle: CryptoCandle): CryptoCandle => {
    if (!priceMultiplier || priceMultiplier === 1) return candle;
    return {
      ...candle,
      open: candle.open * priceMultiplier,
      high: candle.high * priceMultiplier,
      low: candle.low * priceMultiplier,
      close: candle.close * priceMultiplier,
    };
  }, [priceMultiplier]);

  const applyTick = useCallback((existing: CryptoCandle[], tick: PriceUpdate): CryptoCandle[] => {
    const tickTime = new Date(tick.timestamp).getTime();
    if (!Number.isFinite(tickTime) || !Number.isFinite(tick.price)) return existing;

    const bucketStart = getBucketStart(tickTime, intervalMinutes);
    const bucketEnd = bucketStart + intervalMinutes * 60_000;
    const price = tick.price * (priceMultiplier ?? 1);
    const tickVolume = Number.isFinite(tick.volume) ? tick.volume : 0;
    const tickCandle: CryptoCandle = {
      symbol: symbol ?? tick.symbol,
      openTime: new Date(bucketStart).toISOString(),
      closeTime: new Date(bucketEnd).toISOString(),
      open: price,
      high: price,
      low: price,
      close: price,
      volume: tickVolume,
      colorDirection: existing.length > 0
        ? existing[existing.length - 1].colorDirection
          ?? (existing[existing.length - 1].close >= existing[existing.length - 1].open ? 'up' : 'down')
        : 'up',
      interval: intervalMinutes >= 1440
        ? `${intervalMinutes / 1440}d`
        : intervalMinutes >= 60
          ? `${intervalMinutes / 60}h`
          : `${intervalMinutes}m`,
    };

    const lastIndex = existing.length - 1;
    const lastCandle = existing[lastIndex];
    const lastBucket = lastCandle
      ? getBucketStart(new Date(lastCandle.openTime).getTime(), intervalMinutes)
      : -1;

    if (bucketStart < lastBucket) return existing;

    const maxCandles = Math.max(Math.ceil(rangeMinutes / intervalMinutes), 1);
    if (bucketStart === lastBucket) {
      const updated = [...existing];
      updated[lastIndex] = {
        ...lastCandle,
        high: Math.max(lastCandle.high, price),
        low: Math.min(lastCandle.low, price),
        close: price,
        volume: lastCandle.volume + tickVolume,
        colorDirection: price > lastCandle.open
          ? 'up'
          : price < lastCandle.open
            ? 'down'
            : lastCandle.colorDirection
              ?? (lastCandle.close >= lastCandle.open ? 'up' : 'down'),
      };
      return updated.length > maxCandles ? updated.slice(-maxCandles) : updated;
    }

    const updated = [...existing, tickCandle];
    return updated.length > maxCandles ? updated.slice(-maxCandles) : updated;
  }, [intervalMinutes, priceMultiplier, rangeMinutes, symbol]);

  const updateLatestPrice = useCallback((update: PriceUpdate) => {
    if (!symbol || update.symbol.toUpperCase() !== symbol.toUpperCase()) return;
    if (update.updateId) {
      if (seenUpdateIdsRef.current.includes(update.updateId)) return;
      seenUpdateIdsRef.current = [...seenUpdateIdsRef.current.slice(-499), update.updateId];
    }

    lastTickRef.current = update;
    if (candleTimerRef.current) clearTimeout(candleTimerRef.current);
    const tickTime = new Date(update.timestamp).getTime();
    if (Number.isFinite(tickTime)) {
      const intervalMs = intervalMinutes * 60_000;
      const nextBoundary = getBucketStart(tickTime, intervalMinutes) + intervalMs;
      candleTimerRef.current = setTimeout(() => {
        const lastTick = lastTickRef.current;
        if (!lastTick) return;

        const activeBucket = Math.max(nextBoundary, getBucketStart(Date.now(), intervalMinutes));
        const boundaryTick: PriceUpdate = {
          ...lastTick,
          timestamp: new Date(activeBucket).toISOString(),
          volume: 0,
          updateId: undefined,
        };
        if (loadingRef.current) {
          pendingTicksRef.current = [...pendingTicksRef.current.slice(-999), boundaryTick];
        } else {
          setCandles((previous) => applyTick(previous, boundaryTick));
        }
        candleTimerRef.current = null;
      }, Math.max(nextBoundary - Date.now(), 0));
    }

    if (loadingRef.current) {
      pendingTicksRef.current = [...pendingTicksRef.current.slice(-999), update];
      return;
    }
    setCandles((previous) => applyTick(previous, update));
  }, [applyTick, intervalMinutes, symbol]);

  useEffect(() => {
    lastTickRef.current = null;
    if (candleTimerRef.current) {
      clearTimeout(candleTimerRef.current);
      candleTimerRef.current = null;
    }
    return () => {
      if (candleTimerRef.current) {
        clearTimeout(candleTimerRef.current);
        candleTimerRef.current = null;
      }
    };
  }, [intervalMinutes, symbol]);

  const fetchChart = useCallback(async () => {
    if (!symbol) {
      setCandles([]);
      setError(null);
      return;
    }
    if (exchangeFromCurrency && userBaseCurrency
      && exchangeFromCurrency !== userBaseCurrency && !exchangeRate) {
      setError(conversionError ?? 'Oczekiwanie na kurs wymiany walut.');
      setCandles([]);
      return;
    }

    const requestId = ++requestRef.current;
    loadingRef.current = true;
    pendingTicksRef.current = [];
    setLoading(true);
    setError(null);

    try {
      const response = await MarketDataService.getCfdChartCandles(
        symbol,
        rangeMinutes,
        intervalMinutes,
        null,
        userBaseCurrency || undefined,
      );
      if (requestRef.current !== requestId) return;

      let loadedCandles = (response ?? []).map(convertCandle);
      for (const tick of pendingTicksRef.current) {
        loadedCandles = applyTick(loadedCandles, tick);
      }
      pendingTicksRef.current = [];
      setCandles(loadedCandles);
    } catch (fetchError) {
      if (requestRef.current !== requestId) return;
      const message = fetchError instanceof Error ? fetchError.message : 'Nie udało się wczytać historii CFD.';
      setError(message);
      setCandles([]);
    } finally {
      if (requestRef.current === requestId) {
        loadingRef.current = false;
        setLoading(false);
      }
    }
  }, [
    applyTick,
    convertCandle,
    conversionError,
    exchangeFromCurrency,
    exchangeRate,
    intervalMinutes,
    rangeMinutes,
    symbol,
    userBaseCurrency,
  ]);

  useEffect(() => {
    void fetchChart();
    return () => {
      requestRef.current += 1;
      loadingRef.current = false;
      pendingTicksRef.current = [];
      if (candleTimerRef.current) {
        clearTimeout(candleTimerRef.current);
        candleTimerRef.current = null;
      }
    };
  }, [fetchChart]);

  const refetch = useCallback(() => {
    void fetchChart();
  }, [fetchChart]);

  const interval = useMemo(() => {
    if (intervalMinutes >= 1440) return `${intervalMinutes / 1440}d`;
    if (intervalMinutes >= 60) return `${intervalMinutes / 60}h`;
    return `${intervalMinutes}m`;
  }, [intervalMinutes]);

  return {
    candles,
    loading,
    error,
    source: CFD_MARKET_SOURCE,
    interval,
    refetch,
    updateLatestPrice,
    exchangeRate,
    exchangeFromCurrency,
    displayCurrency,
    conversionError,
  };
}
