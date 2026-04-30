import React, { useMemo } from 'react';
import { CryptoCandle } from '../../types/crypto';
import './CryptoChart.css';

interface CryptoChartProps {
  candles: CryptoCandle[];
  loading: boolean;
  error: string | null;
  range: string;
  interval: string;
  source: string;
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

export const CryptoChart: React.FC<CryptoChartProps> = ({ candles, loading, error, range, interval, source }) => {
  const chartData = useMemo(() => {
    if (candles.length === 0) return null;

    const sortedCandles = [...candles].sort(
      (a, b) => new Date(a.openTime).getTime() - new Date(b.openTime).getTime()
    );

    const points = sortedCandles.map((c) => ({
      time: c.openTime,
      value: c.close,
    }));

    const values = points.map((point) => point.value);
    const max = Math.max(...values);
    const min = Math.min(...values);
    const yRange = max - min || 1;

    const width = 950;
    const height = 320;
    const padding = 32;
    const innerWidth = width - padding * 2;
    const innerHeight = height - padding * 2;
    const baseline = height - padding;

    const coordinates = points.map((point, index) => {
      const x = padding + (innerWidth * index) / Math.max(points.length - 1, 1);
      const y = padding + innerHeight - ((point.value - min) / yRange) * innerHeight;
      return { x, y, label: formatTick(point.time), value: point.value };
    });

    const polylinePoints = coordinates.map((point) => `${point.x},${point.y}`).join(' ');

    const areaPath = coordinates.reduce((path, point, index) => {
      if (index === 0) {
        return `M ${point.x} ${baseline} L ${point.x} ${point.y}`;
      }
      return `${path} L ${point.x} ${point.y}`;
    }, '');

    const areaPathClosed = coordinates.length > 0
      ? `${areaPath} L ${coordinates[coordinates.length - 1].x} ${baseline} L ${coordinates[0].x} ${baseline} Z`
      : '';

    const yLabels = [max, min].map((value) => ({
      value,
      y: padding + innerHeight - ((value - min) / yRange) * innerHeight,
    }));

    return {
      chartWidth: width,
      chartHeight: height,
      coordinates,
      polylinePoints,
      areaPath: areaPathClosed,
      min,
      max,
      baseline,
      padding,
      yLabels,
      lastPrice: coordinates[coordinates.length - 1].value,
    };
  }, [candles]);

  return (
    <section className="crypto-chart-card">
      <div className="crypto-chart-header">
        <div>
          <h2>Chart</h2>
          <p className="crypto-chart-meta">{range} range · {interval} interval · Source: {source}</p>
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
            {chartData.yLabels.map((label) => (
              <g key={label.value}>
                <line
                  x1={chartData.padding}
                  y1={label.y}
                  x2={chartData.chartWidth - chartData.padding}
                  y2={label.y}
                  stroke="rgba(255,255,255,0.06)"
                  strokeWidth="1"
                />
                <text x={chartData.padding} y={label.y - 8} fill="#9fc7ff" fontSize="10" opacity="0.75">
                  {label.value.toFixed(2)}
                </text>
              </g>
            ))}
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
              <circle key={index} cx={point.x} cy={point.y} r="3.4" fill="#a5f3ff" />
            ))}
            <text x="16" y="32" fill="#c7e8ff" fontSize="11" opacity="0.88">
              {chartData.max.toFixed(2)}
            </text>
            <text x="16" y={chartData.chartHeight - 14} fill="#c7e8ff" fontSize="11" opacity="0.88">
              {chartData.min.toFixed(2)}
            </text>
          </svg>
          <div className="crypto-chart-ticks">
            {chartData.coordinates.filter((_, index) => index % Math.max(Math.ceil(chartData.coordinates.length / 6), 1) === 0).map((point, index) => (
              <span key={index}>{point.label}</span>
            ))}
          </div>
        </div>
      )}
      {!loading && !error && !chartData && <div className="crypto-chart-status">No chart data available.</div>}
    </section>
  );
};
