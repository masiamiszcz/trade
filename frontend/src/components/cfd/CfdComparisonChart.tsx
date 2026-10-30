import React, { useMemo, useState } from 'react';
import type { CfdChartSeriesSnapshot } from './CfdChartSeriesSource';
import { CFD_SERIES_COLORS } from './cfdChartColors';

interface CfdComparisonChartProps {
  symbols: string[];
  seriesBySymbol: Record<string, CfdChartSeriesSnapshot>;
  highlightedSymbol: string;
  rangeLabel: string;
  intervalLabel: string;
}

const WIDTH = 1000;
const HEIGHT = 460;
const PADDING = { top: 28, right: 28, bottom: 52, left: 80 };

const formatTime = (timestamp: number, includeDate = true) => new Date(timestamp).toLocaleString(undefined, {
  ...(includeDate ? { month: 'short', day: 'numeric' } : {}),
  hour: '2-digit',
  minute: '2-digit',
});

const getIntervalMilliseconds = (label: string): number => {
  const match = label.match(/^(\d+)(m|h|d)$/i);
  if (!match) return 60_000;
  const value = Number(match[1]);
  const unit = match[2].toLowerCase();
  return value * (unit === 'd' ? 86_400_000 : unit === 'h' ? 3_600_000 : 60_000);
};

const formatPrice = (price: number, currency: string) =>
  `${price.toLocaleString(undefined, { maximumFractionDigits: 6 })} ${currency}`;

export const CfdComparisonChart: React.FC<CfdComparisonChartProps> = ({
  symbols,
  seriesBySymbol,
  highlightedSymbol,
  rangeLabel,
  intervalLabel,
}) => {
  const [hoveredTimestamp, setHoveredTimestamp] = useState<number | null>(null);
  const chart = useMemo(() => {
    const rawSeries = symbols.flatMap((symbol, seriesIndex) => {
      const candles = seriesBySymbol[symbol]?.candles ?? [];
      if (candles.length === 0) return [];
      const sorted = [...candles].sort(
        (left, right) => new Date(left.openTime).getTime() - new Date(right.openTime).getTime(),
      );
      return [{
        symbol,
        color: CFD_SERIES_COLORS[seriesIndex % CFD_SERIES_COLORS.length],
        currency: seriesBySymbol[symbol]?.displayCurrency ?? '',
        candles: sorted.flatMap((candle) => {
          const timestamp = new Date(candle.openTime).getTime();
          return Number.isFinite(timestamp) && Number.isFinite(candle.close) && candle.close > 0
            ? [{ timestamp, close: candle.close }]
            : [];
        }),
      }];
    });

    if (rawSeries.length === 0) return null;
    const baseTime = Math.max(...rawSeries.map((series) => series.candles[0].timestamp));
    const preparedSeries = rawSeries.flatMap((series) => {
      const baseCandle = series.candles.reduce<(typeof series.candles)[number] | null>(
        (latest, candle) => candle.timestamp <= baseTime ? candle : latest,
        null,
      );
      if (!baseCandle || baseCandle.close <= 0) return [];
      const points = [
        { timestamp: baseTime, value: 0, close: baseCandle.close },
        ...series.candles
          .filter((candle) => candle.timestamp > baseTime)
          .map((candle) => ({
            timestamp: candle.timestamp,
            value: ((candle.close / baseCandle.close) - 1) * 100,
            close: candle.close,
          })),
      ];
      return [{ ...series, points }];
    });

    if (preparedSeries.length === 0) return null;
    const allPoints = preparedSeries.flatMap((series) => series.points);
    const minTime = baseTime;
    const maxTime = Math.max(...allPoints.map((point) => point.timestamp));
    const minValue = Math.min(0, ...allPoints.map((point) => point.value));
    const maxValue = Math.max(0, ...allPoints.map((point) => point.value));
    const valueRange = maxValue - minValue || 1;
    const innerWidth = WIDTH - PADDING.left - PADDING.right;
    const innerHeight = HEIGHT - PADDING.top - PADDING.bottom;
    const timeRange = maxTime - minTime || 1;
    const y = (value: number) => PADDING.top + ((maxValue - value) / valueRange) * innerHeight;
    const x = (timestamp: number) => PADDING.left + ((timestamp - minTime) / timeRange) * innerWidth;
    const intervalMs = getIntervalMilliseconds(intervalLabel);
    const gridLines = Array.from({ length: 5 }, (_, index) => {
      const value = maxValue - (valueRange * index) / 4;
      return { value, y: y(value) };
    });
    const timeTicks = Array.from({ length: 5 }, (_, index) => {
      const timestamp = minTime + (timeRange * index) / 4;
      return { timestamp, x: x(timestamp) };
    });

    return {
      series: preparedSeries.map((series) => ({
        ...series,
        latestPoint: series.points[series.points.length - 1],
        path: series.points.map((point, index) => {
          const previousPoint = series.points[index - 1];
          const startsSegment = index === 0
            || point.timestamp - previousPoint.timestamp > intervalMs * 1.5;
          return `${startsSegment ? 'M' : 'L'} ${x(point.timestamp)} ${y(point.value)}`;
        }).join(' '),
      })),
      gridLines,
      timeTicks,
      minTime,
      maxTime,
      baseTime,
      intervalMs,
      allPoints,
      x,
      y,
    };
  }, [intervalLabel, seriesBySymbol, symbols]);

  const isLoading = symbols.some((symbol) => seriesBySymbol[symbol]?.loading);
  const errors = symbols.flatMap((symbol) => {
    const error = seriesBySymbol[symbol]?.error;
    return error ? [`${symbol}: ${error}`] : [];
  });
  const onPointerMove = (event: React.PointerEvent<SVGSVGElement>) => {
    if (!chart) return;
    const bounds = event.currentTarget.getBoundingClientRect();
    const pointerX = ((event.clientX - bounds.left) / bounds.width) * WIDTH;
    const plotWidth = WIDTH - PADDING.left - PADDING.right;
    const ratio = Math.max(0, Math.min(1, (pointerX - PADDING.left) / plotWidth));
    setHoveredTimestamp(chart.minTime + ratio * (chart.maxTime - chart.minTime));
  };
  const hoveredPoint = chart && hoveredTimestamp !== null
    ? chart.allPoints.reduce<(typeof chart.allPoints)[number] | null>(
      (nearest, point) => !nearest
        || Math.abs(point.timestamp - hoveredTimestamp) < Math.abs(nearest.timestamp - hoveredTimestamp)
        ? point
        : nearest,
      null,
    )
    : null;
  const tooltipSeries = chart && hoveredPoint
    ? chart.series.flatMap((series) => {
      const point = series.points.reduce<(typeof series.points)[number] | null>(
        (nearest, candidate) => !nearest
          || Math.abs(candidate.timestamp - hoveredPoint.timestamp) < Math.abs(nearest.timestamp - hoveredPoint.timestamp)
          ? candidate
          : nearest,
        null,
      );
      return point && Math.abs(point.timestamp - hoveredPoint.timestamp) <= chart.intervalMs * 1.5
        ? [{ ...series, hoveredPoint: point }]
        : [];
    })
    : [];

  return (
    <section className="cfd-comparison-card">
      <header className="cfd-comparison-header">
        <div>
          <h2>Porównanie wyników</h2>
          <p>
            {rangeLabel} zakres · {intervalLabel} interwał · wspólna baza: {chart ? formatTime(chart.baseTime) : '—'}
          </p>
        </div>
        <span className="cfd-comparison-focus">Wyróżniony: {highlightedSymbol}</span>
      </header>
      {errors.length > 0 && (
        <div className="cfd-series-errors">{errors.join(' · ')}</div>
      )}
      {!chart && isLoading ? (
        <div className="cfd-comparison-state">Ładowanie historii instrumentów…</div>
      ) : !chart ? (
        <div className="cfd-comparison-state">Brak danych wykresu dla wybranych instrumentów.</div>
      ) : (
        <div className="cfd-comparison-visual">
          <svg
            viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
            role="img"
            aria-label="Porównawczy wykres procentowej zmiany cen CFD od wspólnego momentu bazowego"
            onPointerMove={onPointerMove}
            onPointerLeave={() => setHoveredTimestamp(null)}
          >
            <rect width={WIDTH} height={HEIGHT} rx="18" fill="rgba(8, 16, 33, 0.85)" />
            {chart.gridLines.map((line, index) => (
              <g key={index}>
                <line
                  x1={PADDING.left}
                  y1={line.y}
                  x2={WIDTH - PADDING.right}
                  y2={line.y}
                  stroke={line.value === 0 ? 'rgba(255,255,255,0.28)' : 'rgba(255,255,255,0.1)'}
                  strokeDasharray={line.value === 0 ? undefined : '4 6'}
                />
                <text x={PADDING.left - 12} y={line.y + 4} fill="#9fc7ff" fontSize="12" textAnchor="end">
                  {line.value > 0 ? '+' : ''}{line.value.toFixed(2)}%
                </text>
              </g>
            ))}
            {chart.timeTicks.map((tick, index) => (
              <g key={tick.timestamp}>
                <line
                  x1={tick.x}
                  y1={PADDING.top}
                  x2={tick.x}
                  y2={HEIGHT - PADDING.bottom}
                  stroke="rgba(255,255,255,0.06)"
                />
                <text
                  x={tick.x}
                  y={HEIGHT - 18}
                  fill="#9fc7ff"
                  fontSize="11"
                  textAnchor={index === 0 ? 'start' : index === chart.timeTicks.length - 1 ? 'end' : 'middle'}
                >
                  {formatTime(tick.timestamp, chart.maxTime - chart.minTime > 86_400_000)}
                </text>
              </g>
            ))}
            <line
              x1={PADDING.left}
              y1={HEIGHT - PADDING.bottom}
              x2={WIDTH - PADDING.right}
              y2={HEIGHT - PADDING.bottom}
              stroke="rgba(255,255,255,0.18)"
            />
            {hoveredPoint && (
              <line
                x1={chart.x(hoveredPoint.timestamp)}
                y1={PADDING.top}
                x2={chart.x(hoveredPoint.timestamp)}
                y2={HEIGHT - PADDING.bottom}
                stroke="rgba(225, 238, 255, 0.55)"
                strokeDasharray="3 5"
              />
            )}
            {chart.series.map((series) => {
              const isFocused = series.symbol === highlightedSymbol;
              return (
                <g key={series.symbol} opacity={isFocused ? 1 : 0.24}>
                  <path
                    d={series.path}
                    fill="none"
                    stroke={series.color}
                    strokeWidth={isFocused ? 3.5 : 2}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    vectorEffect="non-scaling-stroke"
                  />
                  {isFocused && series.points.length > 0 && (
                    <circle
                      cx={chart.x(series.latestPoint.timestamp)}
                      cy={chart.y(series.latestPoint.value)}
                      r="5"
                      fill={series.color}
                    />
                  )}
                </g>
              );
            })}
          </svg>
          {hoveredPoint && (
            <div
              className="cfd-comparison-tooltip"
              style={{
                left: `${(chart.x(hoveredPoint.timestamp) / WIDTH) * 100}%`,
                transform: chart.x(hoveredPoint.timestamp) > WIDTH * 0.65
                  ? 'translateX(calc(-100% - 12px))'
                  : 'translateX(12px)',
              }}
            >
              <strong>{formatTime(hoveredPoint.timestamp)}</strong>
              {tooltipSeries.map((series) => (
                <div className="cfd-comparison-tooltip-row" key={series.symbol}>
                  <span>
                    <i style={{ backgroundColor: series.color }} />
                    {series.symbol}
                  </span>
                  <span>
                    <b>{series.hoveredPoint.value > 0 ? '+' : ''}{series.hoveredPoint.value.toFixed(2)}%</b>
                    <small>{formatPrice(series.hoveredPoint.close, series.currency)}</small>
                  </span>
                </div>
              ))}
            </div>
          )}
          <div className="cfd-comparison-legend" aria-label="Bieżące wyniki instrumentów">
            {chart.series.map((series) => (
              <div
                className={`cfd-comparison-legend-item ${series.symbol === highlightedSymbol ? 'is-highlighted' : ''}`}
                key={series.symbol}
              >
                <span className="cfd-comparison-legend-name">
                  <i style={{ backgroundColor: series.color }} />
                  <strong>{series.symbol}</strong>
                </span>
                <span className="cfd-comparison-legend-values">
                  <b>{series.latestPoint.value > 0 ? '+' : ''}{series.latestPoint.value.toFixed(2)}%</b>
                  <small>{formatPrice(series.latestPoint.close, series.currency)}</small>
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </section>
  );
};
