import React, { useMemo, useState } from 'react';
import { CryptoCandle } from '../../types/crypto';
import './CryptoChart.css';

interface CryptoChartProps {
  candles: CryptoCandle[];
  loading: boolean;
  error: string | null;
  range: string;
  interval: string;
  source: string;
  chartType: 'line' | 'candles';
}

interface HoverState {
  x: number;
  y: number;
  candle: CryptoCandle;
}

function formatTick(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export const CryptoChart: React.FC<CryptoChartProps> = ({ candles, loading, error, range, interval, source, chartType }) => {
  const [hoverState, setHoverState] = useState<HoverState | null>(null);

  const chartData = useMemo(() => {
    if (candles.length === 0) return null;

    const sortedCandles = [...candles].sort(
      (a, b) => new Date(a.openTime).getTime() - new Date(b.openTime).getTime()
    );

    const values = sortedCandles.flatMap((c) => [c.high, c.low, c.close]);
    const max = Math.max(...values);
    const min = Math.min(...values);
    const yRange = max - min || 1;

    const width = 950;
    const height = 320;
    const padding = 48;
    const innerWidth = width - padding * 2;
    const innerHeight = height - padding * 2;
    const baseline = height - padding;

    const barWidth = Math.min(innerWidth / Math.max(sortedCandles.length, 1) * 0.6, 32);
    const xSpan = Math.max(innerWidth - barWidth, 0);
    const coordinates = sortedCandles.map((candle, index) => {
      const x = padding + barWidth / 2 + (xSpan * index) / Math.max(sortedCandles.length - 1, 1);
      const y = padding + innerHeight - ((candle.close - min) / yRange) * innerHeight;
      return { x, y, label: formatTick(candle.openTime), value: candle.close, candle };
    });

    const polylinePoints = coordinates
      .map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`)
      .join(' ');

    const areaPath = coordinates.reduce((path, point, index) => {
      if (index === 0) {
        return `M ${point.x} ${baseline} L ${point.x} ${point.y}`;
      }
      return `${path} L ${point.x} ${point.y}`;
    }, '');

    const areaPathClosed = coordinates.length > 0
      ? `${areaPath} L ${coordinates[coordinates.length - 1].x} ${baseline} L ${coordinates[0].x} ${baseline} Z`
      : '';

    const candleBars = sortedCandles.map((candle, index) => {
      const xCenter = padding + barWidth / 2 + (xSpan * index) / Math.max(sortedCandles.length - 1, 1);
      const yHigh = padding + innerHeight - ((candle.high - min) / yRange) * innerHeight;
      const yLow = padding + innerHeight - ((candle.low - min) / yRange) * innerHeight;
      const yOpen = padding + innerHeight - ((candle.open - min) / yRange) * innerHeight;
      const yClose = padding + innerHeight - ((candle.close - min) / yRange) * innerHeight;
      const bodyTop = Math.min(yOpen, yClose);
      const bodyHeight = Math.max(Math.abs(yClose - yOpen), 2);

      return {
        xCenter,
        yHigh,
        yLow,
        yOpen,
        yClose,
        bodyTop,
        bodyHeight,
        isUp: candle.close >= candle.open,
        barWidth,
        label: formatTick(candle.openTime),
        candle,
      };
    });

    const gridLines = 5;
    const yLabels = Array.from({ length: gridLines }, (_, index) => {
      const value = max - (yRange * index) / (gridLines - 1);
      return {
        value,
        y: padding + innerHeight - ((value - min) / yRange) * innerHeight,
      };
    });

    return {
      chartWidth: width,
      chartHeight: height,
      coordinates,
      polylinePoints,
      areaPath: areaPathClosed,
      candleBars,
      min,
      max,
      baseline,
      padding,
      yLabels,
      lastPrice: coordinates[coordinates.length - 1].value,
    };
  }, [candles]);

  const formatTooltipDate = (dateString: string) => {
    return new Date(dateString).toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <section className="crypto-chart-card">
      <div className="crypto-chart-header">
        <div>
          <h2>Chart</h2>
          <p className="crypto-chart-meta">{range} range · {interval} interval · {chartType === 'candles' ? 'Świece' : 'Linia'} · Source: {source}</p>
        </div>
        <div className="crypto-chart-summary">
          <span>Points: {candles.length}</span>
          <span>Last price: {chartData ? chartData.lastPrice.toFixed(2) : '—'}</span>
        </div>
      </div>

      {loading && <div className="crypto-chart-status">Loading chart data…</div>}
      {error && <div className="crypto-chart-error">{error}</div>}
      {!loading && !error && chartData && (
        <div className="crypto-chart-visual">
          <svg viewBox={`0 0 ${chartData.chartWidth} ${chartData.chartHeight}`} aria-label="Crypto price chart">
            <defs>
              <linearGradient id="chartGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#7de5ff" stopOpacity="0.45" />
                <stop offset="100%" stopColor="#0b1f3e" stopOpacity="0.05" />
              </linearGradient>
            </defs>
            <rect x="0" y="0" width="100%" height="100%" fill="rgba(8, 16, 33, 0.85)" rx="18" />
            <line
              x1={chartData.padding}
              y1={chartData.padding}
              x2={chartData.padding}
              y2={chartData.baseline}
              stroke="rgba(255,255,255,0.12)"
              strokeWidth="1"
            />
            <line
              x1={chartData.padding}
              y1={chartData.baseline}
              x2={chartData.chartWidth - chartData.padding}
              y2={chartData.baseline}
              stroke="rgba(255,255,255,0.12)"
              strokeWidth="1"
            />
            {chartData.yLabels.map((label, index) => (
              <g key={index}>
                <line
                  x1={chartData.padding}
                  y1={label.y}
                  x2={chartData.chartWidth - chartData.padding}
                  y2={label.y}
                  stroke="rgba(255,255,255,0.12)"
                  strokeWidth="1"
                  strokeDasharray="4 6"
                />
                <text x={chartData.padding - 10} y={label.y - 8} fill="#9fc7ff" fontSize="10" opacity="0.75" textAnchor="end">
                  {label.value.toFixed(2)}
                </text>
              </g>
            ))}
            {chartType === 'line' ? (
              <>
                <path
                  d={chartData.areaPath}
                  fill="url(#chartGradient)"
                  opacity="0.55"
                />
                <path
                  d={`${chartData.polylinePoints}`}
                  fill="none"
                  stroke="#7de5ff"
                  strokeWidth="2.2"
                  vectorEffect="non-scaling-stroke"
                />
                {chartData.coordinates.map((point, index) => (
                  <g
                    key={index}
                    onMouseEnter={() => setHoverState({ x: point.x, y: point.y, candle: point.candle })}
                    onMouseMove={() => setHoverState({ x: point.x, y: point.y, candle: point.candle })}
                    onMouseLeave={() => setHoverState(null)}
                  >
                    <circle cx={point.x} cy={point.y} r="3.4" fill="#a5f3ff" />
                    <circle cx={point.x} cy={point.y} r="10" fill="transparent" style={{ cursor: 'pointer' }} />
                  </g>
                ))}
              </>
            ) : (
              <>
                {chartData.candleBars.map((bar, index) => (
                  <g
                    key={index}
                    onMouseEnter={() => setHoverState({ x: bar.xCenter, y: bar.yHigh, candle: bar.candle })}
                    onMouseMove={() => setHoverState({ x: bar.xCenter, y: bar.yHigh, candle: bar.candle })}
                    onMouseLeave={() => setHoverState(null)}
                    style={{ cursor: 'pointer' }}
                  >
                    <line
                      x1={bar.xCenter}
                      y1={bar.yHigh}
                      x2={bar.xCenter}
                      y2={bar.yLow}
                      stroke={bar.isUp ? '#22c55e' : '#f43f5e'}
                      strokeWidth="2"
                      strokeLinecap="round"
                    />
                    <rect
                      x={bar.xCenter - bar.barWidth / 2}
                      y={bar.bodyTop}
                      width={bar.barWidth}
                      height={bar.bodyHeight}
                      fill={bar.isUp ? '#22c55e' : '#f43f5e'}
                      rx="1"
                    />
                    <rect
                      x={bar.xCenter - 16}
                      y={chartData.padding}
                      width={32}
                      height={chartData.chartHeight - chartData.padding * 2}
                      fill="transparent"
                    />
                  </g>
                ))}
              </>
            )}
          </svg>
          {hoverState && (
            <div
              className="crypto-chart-tooltip"
              style={{
                left: `${(hoverState.x / chartData.chartWidth) * 100}%`,
                top: `${Math.max(8, (hoverState.y / chartData.chartHeight) * 100)}%`,
              }}
            >
              <div className="tooltip-title">{formatTooltipDate(hoverState.candle.openTime)}</div>
              <div className="tooltip-row"><span>Open:</span><span>{hoverState.candle.open.toFixed(2)}</span></div>
              <div className="tooltip-row"><span>High:</span><span>{hoverState.candle.high.toFixed(2)}</span></div>
              <div className="tooltip-row"><span>Low:</span><span>{hoverState.candle.low.toFixed(2)}</span></div>
              <div className="tooltip-row"><span>Close:</span><span>{hoverState.candle.close.toFixed(2)}</span></div>
            </div>
          )}
          <div className="crypto-chart-ticks">
            {chartData.coordinates.filter((_, index) => index % Math.max(Math.ceil(chartData.coordinates.length / 6), 1) === 0).map((point, index, ticks) => {
              const isFirst = index === 0;
              const isLast = index === ticks.length - 1;
              const offset = isFirst ? 'translateX(0)' : isLast ? 'translateX(-100%)' : 'translateX(-50%)';
              const anchor = isFirst ? 'start' : isLast ? 'end' : 'middle';

              return (
                <span
                  key={index}
                  style={{
                    left: `${(point.x / chartData.chartWidth) * 100}%`,
                    transform: offset,
                    textAlign: isFirst ? 'left' : isLast ? 'right' : 'center',
                    display: 'inline-block',
                  }}
                >
                  {point.label}
                </span>
              );
            })}
          </div>
        </div>
      )}
      {!loading && !error && !chartData && <div className="crypto-chart-status">No chart data available.</div>}
    </section>
  );
};
