import React, { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { CryptoChart } from '../components/crypto/CryptoChart';
import { useAccount } from '../hooks/useAccount';
import { useCfdChart } from '../hooks/useCfdChart';
import { useCfdInstruments } from '../hooks/useCfdInstruments';
import { useSignalR } from '../hooks/useSignalR';
import type { PriceUpdate } from '../services/SignalRService';
import './CryptoPage.css';

const RANGE_OPTIONS = [
  { label: '5m', value: 5 },
  { label: '15m', value: 15 },
  { label: '30m', value: 30 },
  { label: '1h', value: 60 },
  { label: '2h', value: 120 },
  { label: '4h', value: 240 },
  { label: '6h', value: 360 },
  { label: '12h', value: 720 },
  { label: '1d', value: 1440 },
  { label: '7d', value: 10080 },
  { label: '30d', value: 43200 },
  { label: '90d', value: 129600 },
  { label: '1y', value: 525600 },
];

const INTERVAL_OPTIONS = [
  { label: '1m', value: 1 },
  { label: '5m', value: 5 },
  { label: '15m', value: 15 },
  { label: '30m', value: 30 },
  { label: '1h', value: 60 },
  { label: '2h', value: 120 },
  { label: '4h', value: 240 },
  { label: '1d', value: 1440 },
];

const resolveMinimumIntervalMinutes = (rangeMinutes: number): number => {
  const day = 1440;
  const year = 525600;
  if (rangeMinutes <= day) return 1;
  if (rangeMinutes <= 7 * day) return 5;
  if (rangeMinutes <= 14 * day) return 15;
  if (rangeMinutes <= 30 * day) return 30;
  if (rangeMinutes <= year) return 60;
  return 1440;
};

const getAllowedIntervals = (rangeMinutes: number) => {
  const minimumInterval = resolveMinimumIntervalMinutes(rangeMinutes);
  return INTERVAL_OPTIONS.filter(
    (option) => option.value >= minimumInterval && option.value <= rangeMinutes,
  );
};

const getAccessToken = () => localStorage.getItem('auth_token');

export const CfdPage: React.FC = () => {
  const { symbol } = useParams<{ symbol: string }>();
  const normalizedSymbol = symbol?.toUpperCase() ?? '';
  const { instruments, loading: instrumentsLoading, error: instrumentsError } = useCfdInstruments();
  const instrument = useMemo(
    () => instruments.find((item) => item.instrument.symbol.toUpperCase() === normalizedSymbol)?.instrument ?? null,
    [instruments, normalizedSymbol],
  );

  const [rangeMinutes, setRangeMinutes] = useState(60);
  const [intervalMinutes, setIntervalMinutes] = useState(1);
  const [showCandles, setShowCandles] = useState(true);
  const { account } = useAccount();
  const {
    candles,
    loading: chartLoading,
    error: chartError,
    source,
    interval,
    refetch,
    updateLatestPrice,
    exchangeRate,
    exchangeFromCurrency,
    displayCurrency,
    conversionError,
  } = useCfdChart(instrument?.symbol, instrument?.quoteCurrency, rangeMinutes, intervalMinutes);

  const handlePriceUpdate = React.useCallback(
    (update: PriceUpdate) => updateLatestPrice(update),
    [updateLatestPrice],
  );

  const { latestPrice, connectionState, error: signalRError } = useSignalR({
    symbol: instrument?.symbol ?? '',
    rangeMinutes,
    accessTokenFactory: getAccessToken,
    onPriceUpdate: handlePriceUpdate,
  });

  useEffect(() => {
    const allowed = getAllowedIntervals(rangeMinutes);
    if (!allowed.some((option) => option.value === intervalMinutes)) {
      setIntervalMinutes(allowed[0]?.value ?? 1);
    }
  }, [intervalMinutes, rangeMinutes]);

  const selectedRangeLabel = RANGE_OPTIONS.find((option) => option.value === rangeMinutes)?.label ?? '1h';
  const latestPriceValue = latestPrice
    ? latestPrice.price * (
      exchangeFromCurrency
      && account?.currency
      && exchangeFromCurrency !== account.currency.toUpperCase()
        ? exchangeRate ?? 1
        : 1
    )
    : null;
  const latestPriceCurrency = latestPriceValue !== null && conversionError
    ? exchangeFromCurrency ?? displayCurrency
    : displayCurrency;

  return (
    <div className="crypto-page">
      <div className="crypto-page-header">
        <h1>CFD details</h1>
        <p className="subtitle">Historia oraz notowania na żywo dla {normalizedSymbol}</p>
        <Link className="back-link" to="/dashboard/cfd">
          ← Powrót do listy CFD
        </Link>
      </div>

      <div className="crypto-page-content">
        {instrumentsLoading ? (
          <div className="loader">Ładowanie instrumentu...</div>
        ) : instrumentsError ? (
          <div className="error-message">Nie udało się pobrać instrumentów CFD: {instrumentsError.message}</div>
        ) : !instrument ? (
          <div className="error-message">Nie znaleziono dostępnego CFD {normalizedSymbol}.</div>
        ) : (
          <>
            <div className="crypto-chart-top">
              <div className="crypto-chart-controls">
                <div className="crypto-dropdown-control">
                  <label htmlFor="cfd-chart-range">Zakres</label>
                  <select
                    id="cfd-chart-range"
                    value={rangeMinutes}
                    onChange={(event) => setRangeMinutes(Number(event.target.value))}
                  >
                    {RANGE_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                </div>

                <div className="crypto-dropdown-control">
                  <label htmlFor="cfd-chart-interval">Interwał</label>
                  <select
                    id="cfd-chart-interval"
                    value={intervalMinutes}
                    onChange={(event) => setIntervalMinutes(Number(event.target.value))}
                  >
                    {getAllowedIntervals(rangeMinutes).map((option) => (
                      <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                </div>

                <div className="crypto-chart-actions">
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
              </div>

              <CryptoChart
                key={`${instrument.symbol}-${selectedRangeLabel}-${interval}-${showCandles}`}
                candles={candles}
                loading={chartLoading}
                error={chartError}
                range={selectedRangeLabel}
                interval={interval}
                source={source}
                chartType={showCandles ? 'candles' : 'line'}
              />
            </div>

            <div className="instrument-summary">
              <div className="instrument-card">
                <div className="instrument-title">{instrument.name}</div>
                <div className="instrument-symbol">{instrument.symbol}</div>
                <div className="instrument-meta">
                  <span>Typ: CFD</span>
                  <span>Waluta bazowa: {instrument.baseCurrency}</span>
                  <span>Waluta kwotowania: {instrument.quoteCurrency}</span>
                  <span>Waluta konta: {account?.currency ?? '—'}</span>
                </div>
                <div className="instrument-description">{instrument.description || 'Brak opisu'}</div>
              </div>

              <div className="live-summary">
                <div className="live-box">
                  <div className="live-label">Połączenie SignalR</div>
                  <div className={`live-status live-${connectionState}`}>{connectionState}</div>
                </div>
                <div className="live-box">
                  <div className="live-label">Ostatnia cena</div>
                  <div className="live-value">
                    {latestPriceValue !== null
                      ? `${latestPriceValue.toFixed(2)} ${latestPriceCurrency}`
                      : 'Brak danych'}
                  </div>
                </div>
                <div className="live-box">
                  <div className="live-label">Ostatni tick</div>
                  <div className="live-value">
                    {latestPrice ? new Date(latestPrice.timestamp).toLocaleTimeString() : '—'}
                  </div>
                </div>
                {conversionError && <div className="error-message">Przeliczanie waluty: {conversionError}</div>}
                {signalRError && <div className="error-message">SignalR: {signalRError}</div>}
              </div>
            </div>

            <div className="crypto-note">
              Historia jest pobierana przez API CFD. Ticki są odbierane przez istniejący hub SignalR,
              a bieżąca świeca jest budowana z ticków dla interwału {interval}.
            </div>
          </>
        )}
      </div>
    </div>
  );
};
