import React from 'react';
import type { CfdInstrumentSummary } from '../../services/MarketDataService';
import { CFD_SERIES_COLORS } from './cfdChartColors';

interface CfdAssetToolbarProps {
  instruments: CfdInstrumentSummary[];
  selectedSymbols: string[];
  highlightedSymbol: string;
  onHighlight: (symbol: string) => void;
  onRemove: (symbol: string) => void;
  onAdd: () => void;
}

export const CfdAssetToolbar: React.FC<CfdAssetToolbarProps> = ({
  instruments,
  selectedSymbols,
  highlightedSymbol,
  onHighlight,
  onRemove,
  onAdd,
}) => (
  <div className="cfd-assets-toolbar">
    <div className="cfd-assets-list" aria-label="Wybrane instrumenty">
      {selectedSymbols.map((symbol, index) => {
        const instrument = instruments.find((item) => item.instrument.symbol === symbol)?.instrument;
        const color = CFD_SERIES_COLORS[index % CFD_SERIES_COLORS.length];
        return (
          <div
            key={symbol}
            className={`cfd-asset-chip ${highlightedSymbol === symbol ? 'is-highlighted' : ''}`}
            style={{ '--asset-color': color } as React.CSSProperties}
          >
            <button type="button" className="cfd-asset-select" onClick={() => onHighlight(symbol)}>
              <span className="cfd-asset-dot" />
              {symbol}
              {instrument?.name && <small>{instrument.name}</small>}
            </button>
            {selectedSymbols.length > 1 && (
              <button
                type="button"
                className="cfd-asset-remove"
                onClick={() => onRemove(symbol)}
                aria-label={`Usuń ${symbol} z widoku`}
              >
                ×
              </button>
            )}
          </div>
        );
      })}
    </div>
    {selectedSymbols.length < 4 && (
      <button type="button" className="cfd-add-asset" onClick={onAdd}>
        <span aria-hidden="true">+</span> Dodaj instrument
      </button>
    )}
  </div>
);
