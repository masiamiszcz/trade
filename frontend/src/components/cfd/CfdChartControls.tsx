import React from 'react';
import { CFD_RANGE_OPTIONS, getAllowedCfdIntervals } from './cfdChartOptions';
import type { CfdChartType } from './CfdFinancialChart';

interface CfdChartControlsProps {
  rangeMinutes: number;
  intervalMinutes: number;
  chartType: CfdChartType;
  showChartTypeToggle: boolean;
  loading: boolean;
  onRangeChange: (rangeMinutes: number) => void;
  onIntervalChange: (intervalMinutes: number) => void;
  onChartTypeChange: (chartType: CfdChartType) => void;
  onRefresh: () => void;
  onSaveView: () => void;
}

export const CfdChartControls: React.FC<CfdChartControlsProps> = ({
  rangeMinutes,
  intervalMinutes,
  chartType,
  showChartTypeToggle,
  loading,
  onRangeChange,
  onIntervalChange,
  onChartTypeChange,
  onRefresh,
  onSaveView,
}) => (
  <div className="cfd-chart-toolbar">
    <div className="cfd-toolbar-group" aria-label="Zakres wykresu">
      <span className="cfd-toolbar-label">Zakres</span>
      <div className="cfd-button-strip">
        {CFD_RANGE_OPTIONS.map((option) => (
          <button
            key={option.value}
            type="button"
            className={`cfd-option-button ${rangeMinutes === option.value ? 'is-selected' : ''}`}
            aria-pressed={rangeMinutes === option.value}
            onClick={() => onRangeChange(option.value)}
          >
            {option.label}
          </button>
        ))}
      </div>
    </div>

    <div className="cfd-toolbar-group" aria-label="Interwał świecy">
      <span className="cfd-toolbar-label">Interwał</span>
      <div className="cfd-button-strip">
        {getAllowedCfdIntervals(rangeMinutes).map((option) => (
          <button
            key={option.value}
            type="button"
            className={`cfd-option-button ${intervalMinutes === option.value ? 'is-selected' : ''}`}
            aria-pressed={intervalMinutes === option.value}
            onClick={() => onIntervalChange(option.value)}
          >
            {option.label}
          </button>
        ))}
      </div>
    </div>

    <div className="cfd-chart-actions">
      <button type="button" className="cfd-option-button" onClick={onSaveView}>
        Zapisz widok
      </button>
      {showChartTypeToggle && (
        <div className="cfd-button-strip" aria-label="Typ wykresu">
          {([
            ['candles', 'Świece'],
            ['line', 'Linia'],
            ['ohlc', 'OHLC'],
          ] as const).map(([type, label]) => (
            <button
              key={type}
              type="button"
              className={`cfd-option-button ${chartType === type ? 'is-selected' : ''}`}
              aria-pressed={chartType === type}
              onClick={() => onChartTypeChange(type)}
            >
              {label}
            </button>
          ))}
        </div>
      )}
      <button type="button" className="cfd-option-button" onClick={onRefresh} disabled={loading}>
        {loading ? 'Ładowanie…' : 'Odśwież'}
      </button>
      <button type="button" className="cfd-trade-button cfd-trade-long" disabled title="Dostępne po dodaniu obsługi backendowej">
        Long
      </button>
      <button type="button" className="cfd-trade-button cfd-trade-short" disabled title="Dostępne po dodaniu obsługi backendowej">
        Short
      </button>
    </div>
  </div>
);
