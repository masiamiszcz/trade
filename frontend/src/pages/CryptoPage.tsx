import React, { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useCryptoInstruments } from '../hooks/useCryptoInstruments';
import { useSignalR } from '../hooks/useSignalR';
import { useCryptoChart } from '../hooks/useCryptoChart';
import { CryptoChart } from '../components/crypto/CryptoChart';
import type { CandleUpdate } from '../services/SignalRService';
import './CryptoPage.css';

const RANGE_OPTIONS = [
  { label: '15m', value: 15 },
  { label: '30m', value: 30 },
  { label: '1h', value: 60 },
  { label: '2h', value: 120 },
  { label: '4h', value: 240 },
  { label: '6h', value: 360 },
  { label: '12h', value: 12 * 60 },
  { label: '1d', value: 24 * 60 },
  { label: '7d', value: 7 * 24 * 60 },
  { label: '14d', value: 14 * 24 * 60 },
  { label: '30d', value: 30 * 24 * 60 },
  { label: '1y', value: 365 * 24 * 60 },
  { label: 'all', value: 20 * 365 * 24 * 60 },
];

const INTERVAL_OPTIONS = [
  { label: '1m', value: 1 },
  { label: '3m', value: 3 },
  { label: '5m', value: 5 },
  { label: '15m', value: 15 },
  { label: '30m', value: 30 },
  { label: '1h', value: 60 },
  { label: '2h', value: 120 },
  { label: '4h', value: 240 },
  { label: '1d', value: 1440 },
];

const resolveMinimumIntervalMinutes = (rangeMinutes: number): number => {
  const DAY = 1440;
  const YEAR = 525600;

  if (rangeMinutes <= DAY) return 1;
  if (rangeMinutes <= 7 * DAY) return 5;
  if (rangeMinutes <= 14 * DAY) return 15;
  if (rangeMinutes <= 30 * DAY) return 30;
  if (rangeMinutes <= YEAR) return 60;
  return 1440;
};

const getAllowedIntervalOptions = (rangeMinutes: number) => {
  const minInterval = resolveMinimumIntervalMinutes(rangeMinutes);
  return INTERVAL_OPTIONS.filter((option) => option.value >= minInterval && option.value <= rangeMinutes);
};

export const CryptoPage: React.FC = () => {
  const { symbol } = useParams<{ symbol: string }>();
  const { crypto, loading, error } = useCryptoInstruments();

  const instrument = useMemo(
    () => crypto.find((item) => item.symbol.toUpperCase() === symbol?.toUpperCase()),
    [crypto, symbol]
  );

  const [rangeMinutes, setRangeMinutes] = useState(7 * 24 * 60);
  const [intervalMinutes, setIntervalMinutes] = useState(resolveMinimumIntervalMinutes(7 * 24 * 60));
  const [showCandles, setShowCandles] = useState(false);

  const selectedRangeLabel = useMemo(
    () => RANGE_OPTIONS.find((option) => option.value === rangeMinutes)?.label ?? '7d',
    [rangeMinutes]
  );

  useEffect(() => {
    const minInterval = resolveMinimumIntervalMinutes(rangeMinutes);
    if (intervalMinutes < minInterval || intervalMinutes > rangeMinutes) {
      setIntervalMinutes(minInterval);
    }
  }, [intervalMinutes, rangeMinutes]);

  const accessTokenFactory = useMemo(
    () => () => localStorage.getItem('auth_token'),
    []
  );

  const { candles, loading: chartLoading, error: chartError, source, interval, refetch, updateLatestCandle } = useCryptoChart(
    instrument?.symbol,
    rangeMinutes,
    intervalMinutes,
  );

  const handleCandleUpdate = React.useCallback(
    (update: CandleUpdate) => {
      updateLatestCandle(update);
    },
    [updateLatestCandle]
  );

  const { latestPrice, connectionState, error: signalRError } = useSignalR({
    symbol: symbol?.toUpperCase() ?? '',
    rangeMinutes,
    accessTokenFactory,
    onCandleUpdate: handleCandleUpdate,
  });

  return (
    <div className="crypto-page">
      <div className="crypto-page-header">
        <h1>Crypto details</h1>
        <p className="subtitle">Live updates and quote feed for {symbol?.toUpperCase()}</p>
        <Link className="back-link" to="/dashboard/crypto">
          ← Powrót do listy kryptowalut
        </Link>
      </div>

      <div className="crypto-page-content">
        {loading ? (
          <div className="loader">Ładowanie instrumentu...</div>
        ) : error ? (
          <div className="error-message">Błąd: {error.message}</div>
        ) : !instrument ? (
          <div className="error-message">Nie znaleziono instrumentu {symbol}</div>
        ) : (
          <>
            <div className="instrument-summary">
              <div className="instrument-card">
                <div className="instrument-title">{instrument.name}</div>
                <div className="instrument-symbol">{instrument.symbol}</div>
                <div className="instrument-meta">
                  <span>Base: {instrument.baseCurrency}</span>
                  <span>Quote: {instrument.quoteCurrency}</span>
                  <span>Status: {instrument.status}</span>
                </div>
                <div className="instrument-description">{instrument.description || 'Brak opisu'}</div>
              </div>

              <div className="live-summary">
                <div className="live-box">
                  <div className="live-label">Połączenie SignalR</div>
                  <div className={`live-status live-${connectionState}`}>
                    {connectionState}
                  </div>
                </div>
                <div className="live-box">
                  <div className="live-label">Ostatnia cena</div>
                  <div className="live-value">
                    {latestPrice ? `${latestPrice.price.toFixed(2)} ${instrument.quoteCurrency}` : 'Brak danych' }
                  </div>
                </div>
                <div className="live-box">
                  <div className="live-label">Ostatni update</div>
                  <div className="live-value">
                    {latestPrice ? new Date(latestPrice.timestamp).toLocaleTimeString() : '—'}
                  </div>
                </div>
                {signalRError && <div className="error-message">SignalR: {signalRError}</div>}
              </div>
            </div>

            <div className="crypto-note">
              Po wejściu na tę stronę uruchomiono połączenie SignalR dla symbolu {instrument.symbol}. Tutaj możesz obserwować kurs i analizować dane historyczne.
            </div>

            <div className="crypto-chart-section">
              <div className="crypto-chart-controls">
                <div className="crypto-chart-control">
                  <label htmlFor="chart-range">Range</label>
                  <select
                    id="chart-range"
                    value={rangeMinutes}
                    onChange={(event) => setRangeMinutes(Number(event.target.value))}
                  >
                    {RANGE_OPTIONS.map((option) => (
                      <option value={option.value} key={option.value}>{option.label}</option>
                    ))}
                  </select>
                </div>

                <div className="crypto-chart-control">
                  <label htmlFor="chart-interval">Interval</label>
                  <select
                    id="chart-interval"
                    value={intervalMinutes}
                    onChange={(event) => setIntervalMinutes(Number(event.target.value))}
                  >
                    {getAllowedIntervalOptions(rangeMinutes).map((option) => (
                      <option value={option.value} key={option.value}>{option.label}</option>
                    ))}
                  </select>
                </div>

                <button
                  type="button"
                  className={`crypto-chart-toggle ${showCandles ? 'active' : ''}`}
                  onClick={() => setShowCandles((current) => !current)}
                >
                  {showCandles ? 'Wykres liniowy' : 'Świece'}
                </button>

                <button type="button" className="crypto-chart-refresh" onClick={refetch}>
                  Odśwież wykres
                </button>
              </div>

              <CryptoChart
                key={`${instrument?.symbol ?? symbol}-${selectedRangeLabel}-${interval}-${showCandles}`}
                candles={candles}
                loading={chartLoading}
                error={chartError}
                range={selectedRangeLabel}
                interval={interval}
                source={source}
                chartType={showCandles ? 'candles' : 'line'}
              />
            </div>
          </>
        )}
      </div>
    </div>
  );
};
