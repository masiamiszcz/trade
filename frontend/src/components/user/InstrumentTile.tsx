import React from 'react';
import { PortfolioTile } from './PortfolioGrid';
import './InstrumentTile.css';

interface InstrumentTileProps {
  tile: PortfolioTile;
  onCustomize: () => void;
}

export const InstrumentTile: React.FC<InstrumentTileProps> = ({ tile, onCustomize }) => {
  const categoryColors: Record<string, string> = {
    STOCK: '#1de9b6',
    CRYPTO: '#ffd700',
    CFD: '#ff6b9d',
  };

  const categoryLabel: Record<string, string> = {
    STOCK: 'Akcje',
    CRYPTO: 'Kryptowaluty',
    CFD: 'CFD',
  };

  const isPositive = (tile.changePercent || 0) >= 0;

  return (
    <div
      className="instrument-tile"
      style={{
        borderTopColor: categoryColors[tile.category],
      }}
    >
      <div className="tile-header">
        <span className="category-badge" style={{ backgroundColor: categoryColors[tile.category] }}>
          {categoryLabel[tile.category]}
        </span>
        <button className="customize-btn" onClick={onCustomize} aria-label="Customize">
          ⚙️
        </button>
      </div>

      <div className="tile-content">
        <div className="instrument-info">
          <h3 className="instrument-symbol">{tile.instrumentSymbol}</h3>
          <p className="instrument-name">{tile.instrumentName}</p>
        </div>

        <div className="instrument-stats">
          <div className="price-section">
            <span className="current-price">
              {tile.currentPrice ? tile.currentPrice.toLocaleString('pl-PL', {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2,
              }) : '--'}
            </span>
            <span className={`change-percent ${isPositive ? 'positive' : 'negative'}`}>
              {isPositive ? '+' : ''}{tile.changePercent?.toFixed(2)}%
            </span>
          </div>

          <div className="timeframe-section">
            <small className="timeframe-label">
              {tile.timeframe === 'MONTHLY' && 'ostatnie 2 miesiące'}
              {tile.timeframe === 'QUARTERLY' && 'ostatnie 3 miesiące'}
              {tile.timeframe === 'YEARLY' && 'ostatnie 2 lata'}
            </small>
          </div>
        </div>
      </div>

      <div className="tile-chart-placeholder">
        <div className="chart-background">
          <svg viewBox="0 0 100 50" preserveAspectRatio="none">
            <polyline
              points="0,40 10,35 20,30 30,25 40,20 50,18 60,22 70,25 80,20 90,25 100,15"
              fill="none"
              stroke={categoryColors[tile.category]}
              strokeWidth="1.5"
              opacity="0.6"
            />
          </svg>
          <span className="chart-label">Kliknij aby wybrać instrument</span>
        </div>
      </div>
    </div>
  );
};
