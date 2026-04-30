import React, { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useCryptoInstruments } from '../hooks/useCryptoInstruments';
import { useSignalR } from '../hooks/useSignalR';
import { useCryptoChart } from '../hooks/useCryptoChart';
import { CryptoChart } from '../components/crypto/CryptoChart';
import type { CandleUpdate } from '../services/SignalRService';
import './CryptoPage.css';

const RANGE_OPTIONS = [
  { label: '12h', value: 12 * 60 },
  { label: '1d', value: 24 * 60 },
  { label: '7d', value: 7 * 24 * 60 },
  { label: '14d', value: 14 * 24 * 60 },
  { label: '30d', value: 30 * 24 * 60 },
  { label: '1y', value: 365 * 24 * 60 },
  { label: 'all', value: 20 * 365 * 24 * 60 },
];


export const CryptoPage: React.FC = () => {
  const { symbol } = useParams<{ symbol: string }>();
  const { crypto, loading, error } = useCryptoInstruments();

  const instrument = useMemo(
    () => crypto.find((item) => item.symbol.toUpperCase() === symbol?.toUpperCase()),
    [crypto, symbol]
  );

  const [rangeMinutes, setRangeMinutes] = useState(7 * 24 * 60);

  const selectedRangeLabel = useMemo(
    () => RANGE_OPTIONS.find((option) => option.value === rangeMinutes)?.label ?? '7d',
    [rangeMinutes]
  );

  const accessTokenFactory = useMemo(
    () => () => localStorage.getItem('auth_token'),
    []
  );

  const { candles, loading: chartLoading, error: chartError, source, interval, refetch, updateLatestCandle } = useCryptoChart(
    instrument?.symbol,
    rangeMinutes,
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
                  <label>Interval</label>
                  <div className="crypto-chart-value">{interval || '...'}</div>
                </div>

                <button type="button" className="crypto-chart-refresh" onClick={refetch}>
                  Odśwież wykres
                </button>
              </div>

              <CryptoChart
                key={`${instrument?.symbol ?? symbol}-${selectedRangeLabel}-${interval}`}
                candles={candles}
                loading={chartLoading}
                error={chartError}
                range={selectedRangeLabel}
                interval={interval}
                source={source}
              />
            </div>
          </>
        )}
      </div>
    </div>
  );
};
