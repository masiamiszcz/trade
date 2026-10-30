import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { CfdAssetToolbar } from '../components/cfd/CfdAssetToolbar';
import type { CfdChartDrawing } from '../components/cfd/CfdChartDrawing';
import { CfdChartControls } from '../components/cfd/CfdChartControls';
import { CfdFinancialChart, type CfdChartType } from '../components/cfd/CfdFinancialChart';
import { CfdChartSeriesSource } from '../components/cfd/CfdChartSeriesSource';
import type { CfdChartSeriesSnapshot } from '../components/cfd/CfdChartSeriesSource';
import { CfdSaveViewDialog } from '../components/cfd/CfdSaveViewDialog';
import { CfdComparisonChart } from '../components/cfd/CfdComparisonChart';
import { CFD_RANGE_OPTIONS, getCfdIntervalLabel, getCfdRangeLabel, getAllowedCfdIntervals } from '../components/cfd/cfdChartOptions';
import { CfdInstrumentPickerDialog } from '../components/cfd/CfdInstrumentPickerDialog';
import { useAccount } from '../hooks/useAccount';
import { useCfdInstruments } from '../hooks/useCfdInstruments';
import { useCfdMultiSignalR } from '../hooks/useCfdMultiSignalR';
import type { PriceUpdate } from '../services/SignalRService';
import { cfdChartViewService } from '../services/CfdChartViewService';
import './CfdPage.css';
import '../components/cfd/CfdComponents.css';

const getAccessToken = () => localStorage.getItem('auth_token');

function getSymbolsFromUrl(querySymbols: string | null, routeSymbol: string): string[] {
  const requested = querySymbols
    ? querySymbols.split(',').map((symbol) => symbol.trim().toUpperCase()).filter(Boolean)
    : [routeSymbol];
  return [...new Set(requested)].slice(0, 4);
}

export const CfdPage: React.FC = () => {
  const { symbol: routeSymbol = '' } = useParams<{ symbol: string }>();
  const normalizedRouteSymbol = routeSymbol.toUpperCase();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { instruments, loading: instrumentsLoading, error: instrumentsError } = useCfdInstruments();
  const requestedRange = Number(searchParams.get('range'));
  const initialRange = CFD_RANGE_OPTIONS.some((option) => option.value === requestedRange) ? requestedRange : 60;
  const requestedInterval = Number(searchParams.get('interval'));
  const initialAllowedIntervals = getAllowedCfdIntervals(initialRange);
  const initialInterval = initialAllowedIntervals.some((option) => option.value === requestedInterval)
    ? requestedInterval
    : initialAllowedIntervals[0]?.value ?? 1;
  const requestedChartType = searchParams.get('chartType');
  const initialChartType: CfdChartType = requestedChartType === 'line' || requestedChartType === 'ohlc'
    ? requestedChartType
    : 'candles';
  const [rangeMinutes, setRangeMinutes] = useState(initialRange);
  const [intervalMinutes, setIntervalMinutes] = useState(initialInterval);
  const [chartType, setChartType] = useState<CfdChartType>(initialChartType);
  const [highlightedSymbol, setHighlightedSymbol] = useState(normalizedRouteSymbol);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [saveDialogOpen, setSaveDialogOpen] = useState(false);
  const [savingView, setSavingView] = useState(false);
  const [saveViewError, setSaveViewError] = useState<string | null>(null);
  const [saveViewMessage, setSaveViewMessage] = useState<string | null>(null);
  const [drawings, setDrawings] = useState<CfdChartDrawing[]>([]);
  const [savedViewError, setSavedViewError] = useState<string | null>(null);
  const [seriesBySymbol, setSeriesBySymbol] = useState<Record<string, CfdChartSeriesSnapshot>>({});
  const tickHandlersRef = useRef(new Map<string, (update: PriceUpdate) => void>());
  const { account } = useAccount();

  const requestedSymbols = searchParams.get('symbols');
  const savedViewId = searchParams.get('viewId');

  useEffect(() => {
    let active = true;
    if (!savedViewId) {
      setDrawings([]);
      setSavedViewError(null);
      return () => { active = false; };
    }

    void cfdChartViewService.getView(savedViewId)
      .then(({ configuration }) => {
        if (!active) return;
        setDrawings(configuration.drawings ?? []);
        setHighlightedSymbol(configuration.highlightedSymbol);
        setRangeMinutes(configuration.rangeMinutes);
        setIntervalMinutes(configuration.intervalMinutes);
        setChartType(configuration.chartType);
        setSavedViewError(null);
      })
      .catch((loadError: unknown) => {
        if (active) setSavedViewError(loadError instanceof Error ? loadError.message : String(loadError));
      });

    return () => { active = false; };
  }, [savedViewId]);

  const selectedSymbols = useMemo(() => {
    const availableSymbols = new Set(instruments.map(({ instrument }) => instrument.symbol.toUpperCase()));
    return getSymbolsFromUrl(requestedSymbols, normalizedRouteSymbol)
      .filter((requested) => availableSymbols.has(requested));
  }, [instruments, normalizedRouteSymbol, requestedSymbols]);

  const selectedInstruments = useMemo(
    () => selectedSymbols.flatMap((selected) => {
      const item = instruments.find(({ instrument }) => instrument.symbol.toUpperCase() === selected);
      return item ? [item.instrument] : [];
    }),
    [instruments, selectedSymbols],
  );

  useEffect(() => {
    if (!selectedSymbols.includes(highlightedSymbol) && selectedSymbols.length > 0) {
      setHighlightedSymbol(selectedSymbols[0]);
    }
  }, [highlightedSymbol, selectedSymbols]);

  useEffect(() => {
    const activeSymbols = new Set(selectedSymbols);
    setSeriesBySymbol((previous) => Object.fromEntries(
      Object.entries(previous).filter(([selected]) => activeSymbols.has(selected)),
    ));
  }, [selectedSymbols]);

  useEffect(() => {
    const allowed = getAllowedCfdIntervals(rangeMinutes);
    if (!allowed.some((option) => option.value === intervalMinutes)) {
      setIntervalMinutes(allowed[0]?.value ?? 1);
    }
  }, [intervalMinutes, rangeMinutes]);

  const navigateToSymbols = useCallback((symbols: string[]) => {
    const uniqueSymbols = [...new Set(symbols.map((selected) => selected.toUpperCase()))].slice(0, 4);
    if (uniqueSymbols.length === 0) return;
    const queryParams = new URLSearchParams();
    if (uniqueSymbols.length > 1) queryParams.set('symbols', uniqueSymbols.join(','));
    queryParams.set('range', String(rangeMinutes));
    queryParams.set('interval', String(intervalMinutes));
    queryParams.set('chartType', chartType);
    const query = `?${queryParams.toString()}`;
    navigate(`/dashboard/cfd/${encodeURIComponent(uniqueSymbols[0])}${query}`);
  }, [chartType, intervalMinutes, navigate, rangeMinutes]);

  const registerTickHandler = useCallback((
    symbol: string,
    handler: ((update: PriceUpdate) => void) | null,
  ) => {
    if (handler) tickHandlersRef.current.set(symbol.toUpperCase(), handler);
    else tickHandlersRef.current.delete(symbol.toUpperCase());
  }, []);

  const onPriceUpdate = useCallback((update: PriceUpdate) => {
    tickHandlersRef.current.get(update.symbol.toUpperCase())?.(update);
  }, []);

  const accessTokenFactory = useCallback(() => getAccessToken(), []);
  const { latestPrices, connectionState, error: signalRError } = useCfdMultiSignalR({
    symbols: selectedSymbols,
    rangeMinutes,
    accessTokenFactory,
    onPriceUpdate,
  });

  const onSeriesSnapshot = useCallback((symbol: string, snapshot: CfdChartSeriesSnapshot) => {
    setSeriesBySymbol((previous) => ({ ...previous, [symbol]: snapshot }));
  }, []);

  const seriesLoading = selectedSymbols.some((selected) => seriesBySymbol[selected]?.loading);
  const highlightedInstrument = selectedInstruments.find(
    (instrument) => instrument.symbol.toUpperCase() === highlightedSymbol,
  );
  const highlightedSeries = seriesBySymbol[highlightedSymbol];
  const selectedRangeLabel = getCfdRangeLabel(rangeMinutes);
  const intervalLabel = getCfdIntervalLabel(intervalMinutes);

  const handleRefresh = () => {
    selectedSymbols.forEach((selected) => seriesBySymbol[selected]?.refetch());
  };

  const handlePickerConfirm = (symbols: string[]) => {
    setPickerOpen(false);
    setHighlightedSymbol(symbols[0]);
    navigateToSymbols(symbols);
  };

  const handleRemoveSymbol = (symbolToRemove: string) => {
    const remaining = selectedSymbols.filter((selected) => selected !== symbolToRemove);
    if (remaining.length > 0) navigateToSymbols(remaining);
  };

  const handleSaveView = async (name: string) => {
    setSavingView(true);
    setSaveViewError(null);
    try {
      await cfdChartViewService.createView(name, {
        version: 1,
        symbols: selectedSymbols,
        highlightedSymbol,
        rangeMinutes,
        intervalMinutes,
        chartType,
        drawings: selectedSymbols.length === 1 ? drawings : [],
      });
      setSaveDialogOpen(false);
      setSaveViewMessage(`Widok „${name}” zapisano na Twoim koncie.`);
    } catch (saveError) {
      setSaveViewError(saveError instanceof Error ? saveError.message : String(saveError));
    } finally {
      setSavingView(false);
    }
  };

  const priceSummary = selectedSymbols.map((selected) => {
    const instrument = selectedInstruments.find((item) => item.symbol.toUpperCase() === selected);
    const snapshot = seriesBySymbol[selected];
    const tick = latestPrices[selected];
    const quoteCurrency = instrument?.quoteCurrency ?? '';
    const exchangeFromCurrency = quoteCurrency.toUpperCase() === 'USDT' ? 'USD' : quoteCurrency.toUpperCase();
    const targetCurrency = account?.currency?.toUpperCase() || exchangeFromCurrency;
    const needsConversion = exchangeFromCurrency !== targetCurrency;
    const tickPrice = tick && (!needsConversion || snapshot?.exchangeRate)
      ? tick.price * (needsConversion ? snapshot?.exchangeRate ?? 1 : 1)
      : null;
    const convertedPrice = tickPrice ?? snapshot?.candles.at(-1)?.close ?? null;
    const currency = needsConversion
      ? snapshot?.displayCurrency ?? exchangeFromCurrency
      : targetCurrency || quoteCurrency;
    return { symbol: selected, price: convertedPrice, currency, conversionError: snapshot?.conversionError };
  });

  return (
    <main className="cfd-page">
      {selectedInstruments.map((instrument) => (
        <CfdChartSeriesSource
          key={instrument.symbol}
          symbol={instrument.symbol}
          quoteCurrency={instrument.quoteCurrency}
          rangeMinutes={rangeMinutes}
          intervalMinutes={intervalMinutes}
          onSnapshot={onSeriesSnapshot}
          registerTickHandler={registerTickHandler}
        />
      ))}

      <header className="cfd-page-header">
        <div>
          <span className="cfd-eyebrow">MARKETS / CFD</span>
          <h1>{selectedSymbols.length > 1 ? 'Porównanie instrumentów' : highlightedInstrument?.name ?? normalizedRouteSymbol}</h1>
          <p className="cfd-page-subtitle">
            {selectedSymbols.length > 1
              ? 'Porównanie wyniku procentowego od wspólnego momentu bazowego; ceny instrumentów są widoczne w legendzie i po wskazaniu wykresu.'
              : `Historia oraz notowania na żywo dla ${normalizedRouteSymbol}.`}
          </p>
        </div>
        <Link className="cfd-back-link" to="/dashboard/cfd">← Powrót do listy CFD</Link>
      </header>

      {instrumentsLoading ? (
        <div className="cfd-page-state">Ładowanie instrumentów…</div>
      ) : instrumentsError ? (
        <div className="cfd-page-state is-error">Nie udało się pobrać instrumentów: {instrumentsError.message}</div>
      ) : selectedInstruments.length === 0 ? (
        <div className="cfd-page-state is-error">Nie znaleziono dostępnego instrumentu CFD dla tego widoku.</div>
      ) : (
        <>
          {savedViewError && (
            <div className="cfd-page-state is-error" role="alert">
              Nie udało się odtworzyć pełnej konfiguracji widoku: {savedViewError}
            </div>
          )}
          <CfdAssetToolbar
            instruments={instruments}
            selectedSymbols={selectedSymbols}
            highlightedSymbol={highlightedSymbol}
            onHighlight={setHighlightedSymbol}
            onRemove={handleRemoveSymbol}
            onAdd={() => setPickerOpen(true)}
          />

          <CfdChartControls
            rangeMinutes={rangeMinutes}
            intervalMinutes={intervalMinutes}
            chartType={chartType}
            showChartTypeToggle={selectedSymbols.length === 1}
            loading={seriesLoading}
            onRangeChange={setRangeMinutes}
            onIntervalChange={setIntervalMinutes}
            onChartTypeChange={setChartType}
            onRefresh={handleRefresh}
            onSaveView={() => {
              setSaveViewMessage(null);
              setSaveViewError(null);
              setSaveDialogOpen(true);
            }}
          />
          {saveViewMessage && <p className="cfd-save-view-message" role="status">{saveViewMessage}</p>}

          {selectedSymbols.length === 1 ? (
            <CfdFinancialChart
              key={`${highlightedSymbol}-${rangeMinutes}-${intervalMinutes}-${savedViewId ?? ''}`}
              symbol={highlightedSymbol}
              candles={highlightedSeries?.candles ?? []}
              loading={highlightedSeries?.loading ?? true}
              error={highlightedSeries?.error ?? null}
              range={selectedRangeLabel}
              interval={intervalLabel}
              intervalMinutes={intervalMinutes}
              currency={highlightedSeries?.displayCurrency ?? account?.currency ?? 'USD'}
              chartType={chartType}
              drawings={drawings}
              onDrawingsChange={setDrawings}
            />
          ) : (
            <CfdComparisonChart
              symbols={selectedSymbols}
              seriesBySymbol={seriesBySymbol}
              highlightedSymbol={highlightedSymbol}
              rangeLabel={selectedRangeLabel}
              intervalLabel={intervalLabel}
            />
          )}

          <section className="cfd-live-panel" aria-label="Notowania na żywo">
            <div className="cfd-live-connection">
              <span className={`cfd-connection-indicator is-${connectionState}`} />
              SignalR: <strong>{connectionState}</strong>
              {signalRError && <span className="cfd-live-error">{signalRError}</span>}
            </div>
            <div className="cfd-live-prices">
              {priceSummary.map(({ symbol: itemSymbol, price, currency, conversionError }) => (
                <div className="cfd-live-price" key={itemSymbol}>
                  <span>{itemSymbol}</span>
                  <strong>{price === null ? '—' : `${price.toLocaleString(undefined, { maximumFractionDigits: 4 })} ${currency}`}</strong>
                  {conversionError && <small>{conversionError}</small>}
                </div>
              ))}
            </div>
          </section>
          <p className="cfd-page-note">
            {selectedSymbols.length > 1
              ? 'Wykres porównuje zmianę procentową, a nie nominalną cenę — dzięki temu różne poziomy cen i waluty nie zniekształcają skali. Brakujące odcinki danych są pokazywane jako przerwy.'
              : `Aktualizacje ceny pochodzą z jednego połączenia SignalR. Interwał świecy: ${intervalLabel}.`}
          </p>
        </>
      )}

      <CfdInstrumentPickerDialog
        open={pickerOpen}
        instruments={instruments}
        initialSymbols={selectedSymbols}
        onClose={() => setPickerOpen(false)}
        onConfirm={handlePickerConfirm}
      />
      <CfdSaveViewDialog
        open={saveDialogOpen}
        saving={savingView}
        error={saveViewError}
        onClose={() => setSaveDialogOpen(false)}
        onSave={(name) => void handleSaveView(name)}
      />
    </main>
  );
};
