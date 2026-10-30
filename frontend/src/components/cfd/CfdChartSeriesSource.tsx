import { useEffect } from 'react';
import type React from 'react';
import { useCfdChart } from '../../hooks/useCfdChart';
import type { PriceUpdate } from '../../services/SignalRService';
import type { CryptoCandle } from '../../types/crypto';

export interface CfdChartSeriesSnapshot {
  candles: CryptoCandle[];
  loading: boolean;
  error: string | null;
  source: string;
  interval: string;
  refetch: () => void;
  displayCurrency: string;
  exchangeRate: number | null;
  conversionError: string | null;
}

interface CfdChartSeriesSourceProps {
  symbol: string;
  quoteCurrency: string;
  rangeMinutes: number;
  intervalMinutes: number;
  onSnapshot: (symbol: string, snapshot: CfdChartSeriesSnapshot) => void;
  registerTickHandler: (symbol: string, handler: ((update: PriceUpdate) => void) | null) => void;
}

export const CfdChartSeriesSource: React.FC<CfdChartSeriesSourceProps> = ({
  symbol,
  quoteCurrency,
  rangeMinutes,
  intervalMinutes,
  onSnapshot,
  registerTickHandler,
}) => {
  const series = useCfdChart(symbol, quoteCurrency, rangeMinutes, intervalMinutes);

  useEffect(() => {
    registerTickHandler(symbol, series.updateLatestPrice);
    return () => registerTickHandler(symbol, null);
  }, [registerTickHandler, series.updateLatestPrice, symbol]);

  useEffect(() => {
    onSnapshot(symbol, {
      candles: series.candles,
      loading: series.loading,
      error: series.error,
      source: series.source,
      interval: series.interval,
      refetch: series.refetch,
      displayCurrency: series.displayCurrency,
      exchangeRate: series.exchangeRate,
      conversionError: series.conversionError,
    });
  }, [
    onSnapshot,
    series.candles,
    series.conversionError,
    series.displayCurrency,
    series.exchangeRate,
    series.error,
    series.interval,
    series.loading,
    series.refetch,
    series.source,
    symbol,
  ]);

  return null;
};
