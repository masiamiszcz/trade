import React from 'react';
import type { CfdChartDrawing, CfdDrawingTool } from './CfdChartDrawing';
import './CfdDrawingToolbar.css';

interface CfdDrawingToolbarProps {
  tool: CfdDrawingTool;
  drawings: CfdChartDrawing[];
  selectedId: string | null;
  canUndo: boolean;
  canRedo: boolean;
  onToolChange: (tool: CfdDrawingTool) => void;
  onDelete: () => void;
  onToggleLock: () => void;
  onUndo: () => void;
  onRedo: () => void;
}

const tools: Array<[CfdDrawingTool, string]> = [
  ['select', 'Kursor'],
  ['horizontal', 'Pozioma'],
  ['vertical', 'Pionowa'],
  ['trend', 'Trend'],
  ['rectangle', 'Prostokąt'],
  ['ruler', 'Linijka'],
];

export const CfdDrawingToolbar: React.FC<CfdDrawingToolbarProps> = ({
  tool,
  drawings,
  selectedId,
  canUndo,
  canRedo,
  onToolChange,
  onDelete,
  onToggleLock,
  onUndo,
  onRedo,
}) => {
  const selected = drawings.find(({ id }) => id === selectedId);

  return (
    <div className="cfd-drawing-toolbar" aria-label="Narzędzia rysowania">
      <div className="cfd-drawing-tool-list">
        {tools.map(([value, label]) => (
          <button
            type="button"
            key={value}
            className={`cfd-drawing-tool ${tool === value ? 'is-selected' : ''}`}
            aria-pressed={tool === value}
            onClick={() => onToolChange(value)}
          >
            {label}
          </button>
        ))}
      </div>
      <div className="cfd-drawing-edit-actions">
        <button type="button" onClick={onUndo} disabled={!canUndo} title="Cofnij (Ctrl+Z)">Cofnij</button>
        <button type="button" onClick={onRedo} disabled={!canRedo} title="Ponów (Ctrl+Y)">Ponów</button>
        <button type="button" onClick={onToggleLock} disabled={!selected} title="Zablokuj lub odblokuj zaznaczenie">
          {selected?.locked ? 'Odblokuj' : 'Zablokuj'}
        </button>
        <button type="button" onClick={onDelete} disabled={!selected} title="Usuń zaznaczenie (Delete)">Usuń</button>
        <span aria-live="polite">
          {drawings.length} {drawings.length === 1 ? 'obiekt' : 'obiektów'}
          {selected?.locked ? ' · zablokowany' : ''}
        </span>
      </div>
    </div>
  );
};
