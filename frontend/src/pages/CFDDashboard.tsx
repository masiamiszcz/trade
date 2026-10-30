import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useCfdInstruments } from '../hooks/useCfdInstruments';
import { useCfdChartViews } from '../hooks/useCfdChartViews';
import type { CfdInstrumentSummary } from '../services/MarketDataService';
import { CfdInstrumentPickerDialog } from '../components/cfd/CfdInstrumentPickerDialog';
import './CFDDashboard.css';
import '../components/cfd/CfdComponents.css';

const formatPrice = (value: number | null, currency: string) => (
  value === null ? '—' : `${value.toLocaleString(undefined, { maximumFractionDigits: 4 })} ${currency}`
);

const formatPercent = (value: number | null) => (
  value === null ? '—' : `${value > 0 ? '+' : ''}${value.toFixed(2)}%`
);

const parseUtcTimestamp = (value: string | null | undefined): Date | null => {
  if (!value) return null;
  const timestamp = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;
  const date = new Date(timestamp);
  return Number.isFinite(date.getTime()) ? date : null;
};

const Sparkline: React.FC<{ values: number[]; symbol: string }> = ({ values, symbol }) => {
  if (values.length < 2) {
    return <div className="cfd-sparkline-empty">Brak danych · 7 dni / 4h</div>;
  }

  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min || 1;
  const points = values
    .map((value, index) => {
      const x = (index / (values.length - 1)) * 100;
      const y = 28 - ((value - min) / range) * 24;
      return `${x},${y}`;
    })
    .join(' ');
  const isUp = values[values.length - 1] >= values[0];

  return (
    <svg
      className={`cfd-sparkline ${isUp ? 'is-up' : 'is-down'}`}
      viewBox="0 0 100 32"
      role="img"
      aria-label={`${symbol}: wykres z ostatnich 7 dni, interwał 4 godziny`}
      preserveAspectRatio="none"
    >
      <polyline points={points} />
    </svg>
  );
};

const CfdRow: React.FC<{
  summary: CfdInstrumentSummary;
  now: number;
}> = ({ summary, now }) => {
  const { instrument } = summary;
  const currency = instrument.quoteCurrency;
  const candleOpenTime = parseUtcTimestamp(summary.candleOpenTimeUtc ?? summary.lastUpdatedUtc);
  const isCurrent = candleOpenTime !== null
    && Math.floor(candleOpenTime.getTime() / 60_000) === Math.floor(now / 60_000);
  const updateLabel = candleOpenTime
    ? `${isCurrent ? 'Aktualne' : 'Nieaktualne'} · ${candleOpenTime.toLocaleTimeString()}`
    : 'Brak danych';

  return (
    <Link
      className="cfd-row"
      to={`/dashboard/cfd/${encodeURIComponent(instrument.symbol)}`}
      aria-label={`Otwórz wykres CFD ${instrument.symbol}`}
    >
      <span className="cfd-instrument">
        <span className="cfd-symbol">{instrument.symbol}</span>
        <span className="cfd-name">{instrument.name}</span>
      </span>
      <span className="cfd-price-block">
        <span className="cfd-cell-label">Ostatnia cena</span>
        <strong>{formatPrice(summary.currentPrice, currency)}</strong>
        <span className={`cfd-update-time ${isCurrent ? 'is-current' : 'is-stale'}`}>{updateLabel}</span>
      </span>
      <span className="cfd-range-block">
        <span className="cfd-cell-label">1 godzina · Low / High</span>
        <span className="cfd-low-high"><small>Low</small>{formatPrice(summary.low1h, currency)}</span>
        <span className="cfd-low-high"><small>High</small>{formatPrice(summary.high1h, currency)}</span>
        <strong className={summary.changePercent1h === null ? '' : summary.changePercent1h >= 0 ? 'positive' : 'negative'}>
          {formatPercent(summary.changePercent1h)}
        </strong>
      </span>
      <span className="cfd-range-block">
        <span className="cfd-cell-label">24 godziny · Low / High</span>
        <span className="cfd-low-high"><small>Low</small>{formatPrice(summary.low24h, currency)}</span>
        <span className="cfd-low-high"><small>High</small>{formatPrice(summary.high24h, currency)}</span>
        <strong className={summary.changePercent24h === null ? '' : summary.changePercent24h >= 0 ? 'positive' : 'negative'}>
          {formatPercent(summary.changePercent24h)}
        </strong>
      </span>
      <span className="cfd-sparkline-wrap">
        <span className="cfd-cell-label">7 dni · świece 4h</span>
        <Sparkline values={summary.sparkline7d} symbol={instrument.symbol} />
      </span>
    </Link>
  );
};

export const CFDDashboard: React.FC = () => {
  const { instruments, loading, error, refetch } = useCfdInstruments();
  const { views, loading: viewsLoading, error: viewsError, refresh: refreshViews, remove: removeView } = useCfdChartViews();
  const [now, setNow] = useState(() => Date.now());
  const [pickerOpen, setPickerOpen] = useState(false);
  const [viewActionError, setViewActionError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    let timeoutId: number;
    const updateAtNextMinute = () => {
      const delay = 60_000 - (Date.now() % 60_000) + 10;
      timeoutId = window.setTimeout(() => {
        setNow(Date.now());
        updateAtNextMinute();
      }, delay);
    };

    updateAtNextMinute();
    return () => window.clearTimeout(timeoutId);
  }, []);

  const openCustomView = (symbols: string[]) => {
    setPickerOpen(false);
    if (symbols.length === 0) return;
    const query = symbols.length > 1 ? `?symbols=${encodeURIComponent(symbols.join(','))}` : '';
    navigate(`/dashboard/cfd/${encodeURIComponent(symbols[0])}${query}`);
  };

  const openSavedView = (id: string) => {
    const view = views.find((item) => item.id === id);
    if (!view) return;
    const { configuration } = view;
    const query = new URLSearchParams();
    if (configuration.symbols.length > 1) query.set('symbols', configuration.symbols.join(','));
    query.set('range', String(configuration.rangeMinutes));
    query.set('interval', String(configuration.intervalMinutes));
    query.set('chartType', configuration.chartType);
    query.set('viewId', id);
    navigate(`/dashboard/cfd/${encodeURIComponent(configuration.highlightedSymbol)}?${query.toString()}`);
  };

  const handleRemoveView = async (id: string, name: string) => {
    if (!window.confirm(`Usunąć zapisany widok „${name}”?`)) return;
    setViewActionError(null);
    try {
      await removeView(id);
    } catch (removeError) {
      setViewActionError(removeError instanceof Error ? removeError.message : String(removeError));
    }
  };

  return (
    <main className="cfd-dashboard">
      <header className="cfd-dashboard-header">
        <div>
          <span className="cfd-eyebrow">MARKETS / CFD</span>
          <h1>Kontrakty CFD</h1>
          <p>Rynek w skrócie — zakresy 1h i 24h oraz trend z ostatnich 7 dni.</p>
        </div>
        <div className="cfd-dashboard-actions">
          <button className="cfd-create-view" type="button" onClick={() => setPickerOpen(true)}>
            <span aria-hidden="true">+</span> Własny widok
          </button>
          <button className="cfd-refresh" type="button" onClick={() => void refetch()} disabled={loading}>
            {loading ? 'Odświeżanie…' : 'Odśwież dane'}
          </button>
        </div>
      </header>

      <section className="cfd-market-list" aria-label="Dostępne instrumenty CFD">
        <div className="cfd-market-head">
          <span>Instrument</span>
          <span>Ostatnia cena</span>
          <span>Zakres 1h</span>
          <span>Zakres 24h</span>
          <span>Trend · 7d / 4h</span>
        </div>
        {loading && instruments.length === 0 ? (
          <div className="cfd-state">Ładowanie instrumentów CFD…</div>
        ) : error && instruments.length === 0 ? (
          <div className="cfd-state cfd-state-error">
            <span>Nie udało się pobrać danych: {error.message}</span>
            <button type="button" onClick={() => void refetch()}>Spróbuj ponownie</button>
          </div>
        ) : instruments.length === 0 ? (
          <div className="cfd-state">Brak dostępnych instrumentów CFD z skonfigurowanym źródłem danych.</div>
        ) : (
          <>
            {error && (
              <div className="cfd-state cfd-state-error">
                Nie udało się odświeżyć danych: {error.message}. Wyświetlane są ostatnie pobrane wartości.
              </div>
            )}
            {instruments.map((summary) => (
              <CfdRow
                key={summary.instrument.id}
                summary={summary}
                now={now}
              />
            ))}
          </>
        )}
      </section>
      <p className="cfd-footnote">
        Zmiana procentowa pokazuje zmianę ceny w oknie. Ekstrema pochodzą z zapisanych świec 1m;
        miniwykres korzysta ze świec 4h z ostatnich 7 dni.
      </p>
      <section className="cfd-saved-views" aria-labelledby="cfd-saved-views-title">
        <header className="cfd-saved-views-header">
          <div>
            <span className="cfd-eyebrow">TWOJE USTAWIENIA</span>
            <h2 id="cfd-saved-views-title">Moje widoki</h2>
            <p>Zapisane konfiguracje instrumentów i wykresów.</p>
          </div>
          <button type="button" className="cfd-refresh" onClick={() => void refreshViews()} disabled={viewsLoading}>
            {viewsLoading ? 'Ładowanie…' : 'Odśwież widoki'}
          </button>
        </header>
        {viewActionError && <p className="cfd-saved-views-error" role="alert">{viewActionError}</p>}
        {viewsError ? (
          <div className="cfd-saved-views-state cfd-saved-views-error" role="alert">
            Nie udało się pobrać zapisanych widoków: {viewsError}
          </div>
        ) : viewsLoading && views.length === 0 ? (
          <div className="cfd-saved-views-state">Ładowanie zapisanych widoków…</div>
        ) : views.length === 0 ? (
          <div className="cfd-saved-views-state">Nie masz jeszcze zapisanych widoków. Otwórz wykres CFD i wybierz „Zapisz widok”.</div>
        ) : (
          <div className="cfd-saved-views-grid">
            {views.map(({ id, name, configuration }) => (
              <article className="cfd-saved-view-card" key={id}>
                <div className="cfd-saved-view-card-copy">
                  <h3>{name}</h3>
                  <p>{configuration.symbols.join(' · ')}</p>
                  <span>
                    {configuration.rangeMinutes} min · świece {configuration.intervalMinutes} min · {
                      configuration.chartType === 'candles' ? 'Świece' : configuration.chartType === 'line' ? 'Linia' : 'OHLC'
                    }
                  </span>
                </div>
                <div className="cfd-saved-view-actions">
                  <button type="button" className="cfd-saved-view-open" onClick={() => openSavedView(id)}>Otwórz</button>
                  <button
                    type="button"
                    className="cfd-saved-view-delete"
                    onClick={() => void handleRemoveView(id, name)}
                    aria-label={`Usuń widok ${name}`}
                  >
                    Usuń
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
      <CfdInstrumentPickerDialog
        open={pickerOpen}
        instruments={instruments}
        initialSymbols={[]}
        onClose={() => setPickerOpen(false)}
        onConfirm={openCustomView}
      />
    </main>
  );
};
