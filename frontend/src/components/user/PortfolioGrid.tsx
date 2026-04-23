import React, { useMemo } from 'react';
import './PortfolioGrid.css';
import { InstrumentTile } from './InstrumentTile';
import { useAvailableInstruments } from '../../hooks/useAvailableInstruments';

export interface PortfolioTile {
  id: string;
  category: 'STOCK' | 'CRYPTO' | 'CFD';
  instrumentSymbol?: string;
  instrumentName?: string;
  currentPrice?: number;
  changePercent?: number;
  timeframe?: 'MONTHLY' | 'QUARTERLY' | 'YEARLY';
  lastUpdated?: string;
}

interface PortfolioGridProps {
  tiles?: PortfolioTile[];
  limit?: number;
}

/**
 * Convert Instrument to PortfolioTile
 */
const instrumentToPortfolioTile = (instrument: any, category: 'STOCK' | 'CRYPTO' | 'CFD'): PortfolioTile => {
  return {
    id: instrument.id,
    category,
    instrumentSymbol: instrument.symbol,
    instrumentName: instrument.name,
    currentPrice: 0,
    changePercent: 0,
    timeframe: 'MONTHLY',
    lastUpdated: new Date().toISOString(),
  };
};

export const PortfolioGrid: React.FC<PortfolioGridProps> = ({ tiles, limit = 6 }) => {
  const { stocks, crypto, cfd, loading, error } = useAvailableInstruments();

  /**
   * Build portfolio tiles from real API data
   * Combines stocks, crypto, and CFDs and limits to 6 items (2 from each category)
   */
  const portfolio = useMemo(() => {
    if (tiles && tiles.length > 0) {
      return tiles;
    }

    if (loading || error) {
      return [];
    }

    const result: PortfolioTile[] = [];
    const itemsPerCategory = Math.max(1, Math.floor(limit / 3));

    // Add stocks
    stocks.slice(0, itemsPerCategory).forEach((stock) => {
      result.push(instrumentToPortfolioTile(stock, 'STOCK'));
    });

    // Add crypto
    crypto.slice(0, itemsPerCategory).forEach((c) => {
      result.push(instrumentToPortfolioTile(c, 'CRYPTO'));
    });

    // Add CFD
    cfd.slice(0, itemsPerCategory).forEach((c) => {
      result.push(instrumentToPortfolioTile(c, 'CFD'));
    });

    return result;
  }, [tiles, limit, stocks, crypto, cfd, loading, error]);

  const handleCustomize = (tileId: string) => {
    console.log('Customizing tile:', tileId);
    // TODO: Implement customization modal
  };

  if (loading) {
    return (
      <div className="portfolio-grid-container">
        <div className="portfolio-grid">
          <div className="loader">Wczytywanie instrumentów...</div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="portfolio-grid-container">
        <div className="portfolio-grid">
          <div className="error-message">
            ❌ Błąd podczas wczytywania instrumentów: {error.message}
          </div>
        </div>
      </div>
    );
  }

  if (portfolio.length === 0) {
    return (
      <div className="portfolio-grid-container">
        <div className="portfolio-grid">
          <div className="empty-message">Brak dostępnych instrumentów</div>
        </div>
      </div>
    );
  }

  return (
    <div className="portfolio-grid-container">
      <div className="portfolio-grid">
        {portfolio.map((tile) => (
          <InstrumentTile
            key={tile.id}
            tile={tile}
            onCustomize={() => handleCustomize(tile.id)}
          />
        ))}
      </div>
    </div>
  );
};
