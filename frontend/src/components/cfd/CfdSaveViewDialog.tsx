import React, { useEffect, useState } from 'react';
import './CfdComponents.css';

interface CfdSaveViewDialogProps {
  open: boolean;
  saving: boolean;
  error: string | null;
  onClose: () => void;
  onSave: (name: string) => void;
}

export const CfdSaveViewDialog: React.FC<CfdSaveViewDialogProps> = ({
  open,
  saving,
  error,
  onClose,
  onSave,
}) => {
  const [name, setName] = useState('');

  useEffect(() => {
    if (open) setName('');
  }, [open]);

  if (!open) return null;

  return (
    <div className="cfd-dialog-backdrop" role="presentation" onMouseDown={(event) => {
      if (event.target === event.currentTarget && !saving) onClose();
    }}>
      <form
        className="cfd-picker-dialog cfd-save-view-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="cfd-save-view-title"
        onSubmit={(event) => {
          event.preventDefault();
          onSave(name.trim());
        }}
      >
        <header className="cfd-picker-header">
          <div>
            <span className="cfd-eyebrow">CFD / WŁASNY WIDOK</span>
            <h2 id="cfd-save-view-title">Zapisz konfigurację</h2>
            <p>Zapis obejmuje instrumenty, zaznaczenie, zakres, interwał i typ wykresu.</p>
          </div>
          <button type="button" className="cfd-dialog-close" onClick={onClose} disabled={saving} aria-label="Zamknij">
            ×
          </button>
        </header>
        <div className="cfd-save-view-content">
          <label htmlFor="cfd-view-name">Nazwa widoku</label>
          <input
            id="cfd-view-name"
            autoFocus
            maxLength={80}
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="np. BTC i ETH — krótkoterminowo"
          />
          {error && <p className="cfd-save-view-error" role="alert">{error}</p>}
        </div>
        <footer className="cfd-picker-footer">
          <button type="button" className="cfd-option-button" onClick={onClose} disabled={saving}>Anuluj</button>
          <button type="submit" className="cfd-picker-confirm" disabled={saving || !name.trim()}>
            {saving ? 'Zapisywanie…' : 'Zapisz widok'}
          </button>
        </footer>
      </form>
    </div>
  );
};
