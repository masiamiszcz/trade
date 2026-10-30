import React, { useEffect, useMemo, useRef, useState } from 'react';
import {
  BarSeries,
  CandlestickSeries,
  ColorType,
  createChart,
  LineStyle,
  LineSeries,
  type IChartApi,
  type ISeriesApi,
  type MouseEventParams,
  type Time,
  type UTCTimestamp,
} from 'lightweight-charts';
import type { CryptoCandle } from '../../types/crypto';
import { CfdDrawingOverlay } from './CfdDrawingOverlay';
import { CfdDrawingToolbar } from './CfdDrawingToolbar';
import type { CfdChartDrawing, CfdDrawingPoint, CfdDrawingTool } from './CfdChartDrawing';
import './CfdFinancialChart.css';

export type CfdChartType = 'candles' | 'line' | 'ohlc';

interface CfdFinancialChartProps {
  symbol: string;
  candles: CryptoCandle[];
  loading: boolean;
  error: string | null;
  range: string;
  interval: string;
  intervalMinutes: number;
  currency: string;
  chartType: CfdChartType;
  drawings: CfdChartDrawing[];
  onDrawingsChange: (drawings: CfdChartDrawing[]) => void;
}

interface CandleReadout {
  time: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

const formatPrice = (value: number, currency: string) =>
  `${value.toLocaleString(undefined, { maximumFractionDigits: 6 })} ${currency}`;

const formatCandleTime = (time: Time): string => {
  if (typeof time === 'number') return new Date(time * 1000).toLocaleString();
  if (typeof time === 'string') return new Date(time).toLocaleString();
  return new Date(time.year, time.month - 1, time.day).toLocaleString();
};

const toUnixSeconds = (timestamp: string): number | null => {
  const parsed = new Date(timestamp).getTime();
  if (!Number.isFinite(parsed)) return null;
  return Math.floor(parsed / 1000);
};

const getTimeRemaining = (intervalMinutes: number, now: number): string => {
  const intervalMs = intervalMinutes * 60_000;
  const remainingMs = intervalMs - (now % intervalMs);
  const seconds = Math.max(0, Math.ceil(remainingMs / 1000));
  const minutesPart = Math.floor(seconds / 60);
  const secondsPart = seconds % 60;
  return `${String(minutesPart).padStart(2, '0')}:${String(secondsPart).padStart(2, '0')}`;
};

export const CfdFinancialChart: React.FC<CfdFinancialChartProps> = ({
  symbol,
  candles,
  loading,
  error,
  range,
  interval,
  intervalMinutes,
  currency,
  chartType,
  drawings,
  onDrawingsChange,
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const drawingSurfaceRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<'Candlestick' | 'Line' | 'Bar', Time> | null>(null);
  const hasFitContentRef = useRef(false);
  const [readout, setReadout] = useState<CandleReadout | null>(null);
  const [now, setNow] = useState(Date.now);
  const [drawingTool, setDrawingTool] = useState<CfdDrawingTool>('select');
  const drawingToolRef = useRef(drawingTool);
  drawingToolRef.current = drawingTool;
  const [selectedDrawingId, setSelectedDrawingId] = useState<string | null>(null);
  const [surfaceSize, setSurfaceSize] = useState({ width: 0, height: 0 });
  const [, setHistoryVersion] = useState(0);
  const pastDrawingsRef = useRef<CfdChartDrawing[][]>([]);
  const futureDrawingsRef = useRef<CfdChartDrawing[][]>([]);
  const drawingsRef = useRef(drawings);

  const chartData = useMemo(() => {
    const sorted = candles.flatMap((candle) => {
      const time = toUnixSeconds(candle.openTime);
      if (time === null || ![candle.open, candle.high, candle.low, candle.close].every(Number.isFinite)) {
        return [];
      }
      return [{ ...candle, time: time as Time }];
    }).sort((left, right) => Number(left.time) - Number(right.time));
    return sorted.filter((candle, index) => index === 0 || candle.time !== sorted[index - 1].time);
  }, [candles]);

  const chartDataRef = useRef(chartData);
  chartDataRef.current = chartData;
  drawingsRef.current = drawings;

  const updateDrawings = (next: CfdChartDrawing[], recordHistory = true) => {
    if (recordHistory) {
      pastDrawingsRef.current = [...pastDrawingsRef.current, drawingsRef.current].slice(-100);
      futureDrawingsRef.current = [];
    }
    drawingsRef.current = next;
    onDrawingsChange(next);
    setHistoryVersion((version) => version + 1);
  };

  const undo = () => {
    const previous = pastDrawingsRef.current.at(-1);
    if (!previous) return;
    pastDrawingsRef.current = pastDrawingsRef.current.slice(0, -1);
    futureDrawingsRef.current = [...futureDrawingsRef.current, drawingsRef.current];
    updateDrawings(previous, false);
    setSelectedDrawingId(null);
  };

  const redo = () => {
    const next = futureDrawingsRef.current.at(-1);
    if (!next) return;
    futureDrawingsRef.current = futureDrawingsRef.current.slice(0, -1);
    pastDrawingsRef.current = [...pastDrawingsRef.current, drawingsRef.current];
    updateDrawings(next, false);
    setSelectedDrawingId(null);
  };

  const selectedDrawing = drawings.find(({ id }) => id === selectedDrawingId);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return undefined;

    const chart = createChart(container, {
      autoSize: true,
      layout: {
        background: { type: ColorType.Solid, color: '#081021' },
        textColor: '#9fc7ff',
        attributionLogo: true,
      },
      grid: {
        vertLines: { color: 'rgba(255,255,255,0.06)' },
        horzLines: { color: 'rgba(255,255,255,0.09)' },
      },
      crosshair: {
        vertLine: { color: 'rgba(159, 199, 255, 0.6)', style: LineStyle.Dashed, labelBackgroundColor: '#244266' },
        horzLine: { color: 'rgba(159, 199, 255, 0.6)', style: LineStyle.Dashed, labelBackgroundColor: '#244266' },
      },
      rightPriceScale: { borderColor: 'rgba(126,160,199,0.22)' },
      timeScale: {
        borderColor: 'rgba(126,160,199,0.22)',
        timeVisible: true,
        secondsVisible: false,
        rightOffset: 6,
        fixLeftEdge: true,
      },
      handleScroll: { mouseWheel: true, pressedMouseMove: true, horzTouchDrag: true, vertTouchDrag: false },
      handleScale: { mouseWheel: true, pinch: true, axisPressedMouseMove: true },
    });

    const handleCrosshairMove = (param: MouseEventParams<Time>) => {
      const candle = param.time === undefined
        ? undefined
        : chartDataRef.current.find((item) => Number(item.time) === Number(param.time));
      if (!candle) {
        setReadout(null);
        return;
      }
      setReadout({
        time: param.time ? formatCandleTime(param.time) : '',
        open: candle.open,
        high: candle.high,
        low: candle.low,
        close: candle.close,
        volume: candle.volume,
      });
    };

    const handleChartClick = () => {
      if (drawingToolRef.current === 'select') setSelectedDrawingId(null);
    };

    chart.subscribeCrosshairMove(handleCrosshairMove);
    chart.subscribeClick(handleChartClick);
    chartRef.current = chart;

    return () => {
      chart.unsubscribeCrosshairMove(handleCrosshairMove);
      chart.unsubscribeClick(handleChartClick);
      chart.remove();
      chartRef.current = null;
      seriesRef.current = null;
    };
  }, []);

  useEffect(() => {
    setReadout(null);
    setSelectedDrawingId(null);
    setDrawingTool('select');
    pastDrawingsRef.current = [];
    futureDrawingsRef.current = [];
    setHistoryVersion((version) => version + 1);
  }, [symbol]);

  useEffect(() => {
    const chart = chartRef.current;
    if (!chart) return;
    if (seriesRef.current) {
      chart.removeSeries(seriesRef.current);
    }

    const decimalPlaces = chartDataRef.current.reduce(
      (maximum, candle) => Math.max(maximum, (String(candle.close).split('.')[1] ?? '').length),
      2,
    );
    const precision = Math.min(decimalPlaces, 8);
    const commonOptions = {
      lastValueVisible: true,
      priceLineVisible: true,
      priceFormat: { type: 'price' as const, precision, minMove: 10 ** -precision },
    };
    if (chartType === 'candles') {
      seriesRef.current = chart.addSeries(CandlestickSeries, {
        ...commonOptions,
        upColor: '#22c55e',
        downColor: '#f43f5e',
        borderVisible: false,
        wickUpColor: '#22c55e',
        wickDownColor: '#f43f5e',
      });
      seriesRef.current.setData(chartData.map(({ time, open, high, low, close }) => ({ time, open, high, low, close })));
    } else if (chartType === 'ohlc') {
      seriesRef.current = chart.addSeries(BarSeries, {
        ...commonOptions,
        upColor: '#22c55e',
        downColor: '#f43f5e',
      });
      seriesRef.current.setData(chartData.map(({ time, open, high, low, close }) => ({ time, open, high, low, close })));
    } else {
      seriesRef.current = chart.addSeries(LineSeries, {
        ...commonOptions,
        color: '#55d6be',
        lineWidth: 2,
      });
      seriesRef.current.setData(chartData.map(({ time, close }) => ({ time, value: close })));
    }
    if (chartDataRef.current.length > 0) {
      chart.timeScale().fitContent();
      hasFitContentRef.current = true;
    }
    setHistoryVersion((version) => version + 1);
  }, [chartType]);

  useEffect(() => {
    const series = seriesRef.current;
    const chart = chartRef.current;
    if (!series || !chart) return;

    if (chartType === 'line') {
      (series as ISeriesApi<'Line', Time>).setData(chartData.map(({ time, close }) => ({ time, value: close })));
    } else {
      const ohlc = chartData.map(({ time, open, high, low, close }) => ({ time, open, high, low, close }));
      if (chartType === 'candles') {
        (series as ISeriesApi<'Candlestick', Time>).setData(ohlc);
      } else {
        (series as ISeriesApi<'Bar', Time>).setData(ohlc);
      }
    }
    if (!hasFitContentRef.current && chartData.length > 0) {
      chart.timeScale().fitContent();
      hasFitContentRef.current = true;
    }
  }, [chartData, chartType]);

  useEffect(() => {
    const surface = drawingSurfaceRef.current;
    if (!surface) return undefined;
    const resizeObserver = new ResizeObserver(([entry]) => {
      setSurfaceSize({
        width: entry.contentRect.width,
        height: entry.contentRect.height,
      });
    });
    resizeObserver.observe(surface);
    return () => resizeObserver.disconnect();
  }, []);

  useEffect(() => {
    const chart = chartRef.current;
    if (!chart) return undefined;
    const handleVisibleRangeChange = () => setHistoryVersion((version) => version + 1);
    chart.timeScale().subscribeVisibleLogicalRangeChange(handleVisibleRangeChange);
    return () => chart.timeScale().unsubscribeVisibleLogicalRangeChange(handleVisibleRangeChange);
  }, []);

  useEffect(() => {
    const timerId = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timerId);
  }, []);

  useEffect(() => {
    const handler = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setDrawingTool('select');
      const target = event.target;
      if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement) return;
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'z') {
        event.preventDefault();
        if (event.shiftKey) redo();
        else undo();
      } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'y') {
        event.preventDefault();
        redo();
      } else if ((event.key === 'Delete' || event.key === 'Backspace') && selectedDrawingId) {
        updateDrawings(drawingsRef.current.filter(({ id }) => id !== selectedDrawingId));
        setSelectedDrawingId(null);
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [selectedDrawingId]);

  const chart = chartRef.current;
  const series = seriesRef.current;
  const timeToX = (time: number) => chart?.timeScale().timeToCoordinate(time as UTCTimestamp) ?? null;
  const priceToY = (price: number) => series?.priceToCoordinate(price) ?? null;
  const pointAt = (x: number, y: number): CfdDrawingPoint | null => {
    const time = chart?.timeScale().coordinateToTime(x);
    const price = series?.coordinateToPrice(y);
    return typeof time === 'number' && price !== null && price !== undefined
      ? { time, price }
      : null;
  };

  const moveDrawing = (
    id: string,
    original: CfdDrawingPoint[],
    deltaTime: number,
    deltaPrice: number,
    commit: boolean,
  ) => {
    if (commit) {
      const previous = drawingsRef.current.map((drawing) => drawing.id === id
        ? { ...drawing, points: original }
        : drawing);
      pastDrawingsRef.current = [...pastDrawingsRef.current, previous].slice(-100);
      futureDrawingsRef.current = [];
    }
    const next = drawingsRef.current.map((drawing) => drawing.id === id
      ? {
          ...drawing,
          points: original.map((point) => {
            if (drawing.type === 'horizontal') return { ...point, price: point.price + deltaPrice };
            if (drawing.type === 'vertical') return { ...point, time: point.time + deltaTime };
            return { time: point.time + deltaTime, price: point.price + deltaPrice };
          }),
        }
      : drawing);
    drawingsRef.current = next;
    onDrawingsChange(next);
    if (commit) setHistoryVersion((version) => version + 1);
  };

  const latest = chartData.at(-1);
  const displayReadout = readout ?? (latest ? {
    time: formatCandleTime(latest.time),
    open: latest.open,
    high: latest.high,
    low: latest.low,
    close: latest.close,
    volume: latest.volume,
  } : null);

  return (
    <section className="cfd-financial-chart">
      <header className="cfd-financial-chart-header">
        <div>
          <strong>{symbol}</strong>
          <span>{range} zakres · {interval} interwał · {chartType === 'ohlc' ? 'OHLC' : chartType === 'line' ? 'Linia' : 'Świece'}</span>
        </div>
        <div className="cfd-candle-readout" aria-live="polite">
          {displayReadout ? (
            <>
              <span>{displayReadout.time}</span>
              <span>O <b>{formatPrice(displayReadout.open, currency)}</b></span>
              <span>H <b>{formatPrice(displayReadout.high, currency)}</b></span>
              <span>L <b>{formatPrice(displayReadout.low, currency)}</b></span>
              <span>C <b>{formatPrice(displayReadout.close, currency)}</b></span>
              <span>Do zamknięcia <b>{getTimeRemaining(intervalMinutes, now)}</b></span>
            </>
          ) : <span>Oczekiwanie na dane świec…</span>}
        </div>
      </header>
      <CfdDrawingToolbar
        tool={drawingTool}
        drawings={drawings}
        selectedId={selectedDrawingId}
        canUndo={pastDrawingsRef.current.length > 0}
        canRedo={futureDrawingsRef.current.length > 0}
        onToolChange={setDrawingTool}
        onDelete={() => {
          if (!selectedDrawingId) return;
          updateDrawings(drawingsRef.current.filter(({ id }) => id !== selectedDrawingId));
          setSelectedDrawingId(null);
        }}
        onToggleLock={() => {
          if (!selectedDrawingId) return;
          updateDrawings(drawingsRef.current.map((drawing) => drawing.id === selectedDrawingId
            ? { ...drawing, locked: !drawing.locked }
            : drawing));
        }}
        onUndo={undo}
        onRedo={redo}
      />
      {loading && <div className="cfd-financial-chart-status">Ładowanie historii…</div>}
      {error && <div className="cfd-financial-chart-status is-error">{error}</div>}
      <div ref={drawingSurfaceRef} className="cfd-financial-chart-surface">
        <div ref={containerRef} className="cfd-financial-chart-canvas" aria-label={`Wykres CFD ${symbol}`} />
        {surfaceSize.width > 0 && surfaceSize.height > 0 && (
          <CfdDrawingOverlay
            width={surfaceSize.width}
            plotWidth={chart?.timeScale().width() ?? surfaceSize.width}
            height={surfaceSize.height}
            tool={drawingTool}
            drawings={drawings}
            selectedId={selectedDrawing?.id ?? null}
            timeToX={timeToX}
            priceToY={priceToY}
            pointAt={pointAt}
            onCreate={(drawing) => {
              updateDrawings([...drawingsRef.current, drawing]);
              setSelectedDrawingId(drawing.id);
              setDrawingTool('select');
            }}
            onSelect={setSelectedDrawingId}
            onMove={moveDrawing}
          />
        )}
      </div>
      <p className="cfd-chart-volume-note">
        Wolumen prezentuje wartość przekazaną przez feed rynkowy; dla CFD nie należy interpretować go jako pełnego wolumenu rynku.
        {' '}Charting by{' '}
        <a href="https://www.tradingview.com/" target="_blank" rel="noreferrer">TradingView</a>
      </p>
    </section>
  );
};
