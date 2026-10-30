import React, { useEffect, useState } from 'react';
import type { CfdInstrumentSummary } from '../../services/MarketDataService';

interface CfdInstrumentPickerDialogProps {
  open: boolean;
  instruments: CfdInstrumentSummary[];
  initialSymbols: string[];
  onClose: () => void;
  onConfirm: (symbols: string[]) => void;
}

export const CfdInstrumentPickerDialog: React.FC<CfdInstrumentPickerDialogProps> = ({
  open,
  instruments,
  initialSymbols,
  onClose,
  onConfirm,
}) => {
  const [selectedSymbols, setSelectedSymbols] = useState<string[]>(initialSymbols);
  const initialSymbolsKey = initialSymbols.join('|');

  useEffect(() => {
    if (open) setSelectedSymbols(initialSymbolsKey ? initialSymbolsKey.split('|') : []);
  }, [initialSymbolsKey, open]);

  if (!open) return null;

  const toggleSymbol = (symbol: string) => {
    setSelectedSymbols((selected) => selected.includes(symbol)
      ? selected.filter((item) => item !== symbol)
      : selected.length < 4 ? [...selected, symbol] : selected);
  };

  return (
    <div className="cfd-dialog-backdrop" onMouseDown={(event) => {
      if (event.target === event.currentTarget) onClose();
    }}>
      <section
        className="cfd-picker-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="cfd-picker-title"
      >
        <div className="cfd-picker-header">
          <div>
            <span className="cfd-eyebrow">WŁASNY WIDOK</span>
            <h2 id="cfd-picker-title">Wybierz instrumenty</h2>
            <p>Dodaj do czterech CFD do wspólnego wykresu.</p>
          </div>
          <button type="button" className="cfd-dialog-close" onClick={onClose} aria-label="Zamknij">
            ×
          </button>
        </div>

        <div className="cfd-picker-list">
          {instruments.map(({ instrument }) => {
            const checked = selectedSymbols.includes(instrument.symbol);
            const limitReached = selectedSymbols.length >= 4 && !checked;
            return (
              <label className={`cfd-picker-option ${checked ? 'is-selected' : ''}`} key={instrument.id}>
                <input
                  type="checkbox"
                  checked={checked}
                  disabled={limitReached}
                  onChange={() => toggleSymbol(instrument.symbol)}
                />
                <span>
                  <strong>{instrument.symbol}</strong>
                  <small>{instrument.name}</small>
                </span>
              </label>
            );
          })}
          {instruments.length === 0 && (
            <p className="cfd-picker-empty">Brak instrumentów CFD ze skonfigurowanym źródłem danych.</p>
          )}
        </div>

        <div className="cfd-picker-footer">
          <span>{selectedSymbols.length} / 4 wybrane</span>
          <button
            type="button"
            className="cfd-picker-confirm"
            disabled={selectedSymbols.length === 0}
            onClick={() => onConfirm(selectedSymbols)}
          >
            Otwórz widok
          </button>
        </div>
      </section>
    </div>
  );
};
