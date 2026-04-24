import React, { useState, useEffect } from 'react';
import { Instrument, ACCOUNT_PILLARS, AccountPillar} from '../../../types/admin';

interface InstrumentModalProps {
  isOpen: boolean;
  instrument?: Instrument | null;
  onClose: () => void;
  onSubmit: (data: Partial<Instrument>) => Promise<void>;
}

export const InstrumentModal: React.FC<InstrumentModalProps> = ({ isOpen, instrument, onClose, onSubmit }) => {
  const [formData, setFormData] = useState<Partial<Instrument>>({
    symbol: '',
    name: '',
    description: '',
    type: 'Stock',
    pillar: 'Stocks',
    baseCurrency: 'USD',
    quoteCurrency: 'USD'
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (instrument) {
      setFormData(instrument);
    } else {
      setFormData({
        symbol: '',
        name: '',
        description: '',
        type: 'Stock',
        pillar: 'Stocks',
        baseCurrency: 'USD',
        quoteCurrency: 'USD'
      });
    }
    setError(null);
  }, [instrument, isOpen]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!formData.symbol?.trim() || !formData.name?.trim() || !formData.description?.trim()) {
      setError('Symbol, Name, and Description are required');
      return;
    }

    setLoading(true);
    try {
      await onSubmit(formData);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{instrument ? '✏️ Edytuj Instrument' : '➕ Nowy Instrument'}</h2>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && <div style={{ color: '#ff4757', marginBottom: '12px', fontSize: '13px' }}>{error}</div>}

            <div className="form-group">
              <label>Symbol *</label>
              <input
                type="text"
                name="symbol"
                value={formData.symbol || ''}
                onChange={handleChange}
                placeholder="e.g., AAPL, BTCUSD"
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label>Name *</label>
              <input
                type="text"
                name="name"
                value={formData.name || ''}
                onChange={handleChange}
                placeholder="e.g., Apple Inc."
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label>Description *</label>
              <textarea
                name="description"
                value={formData.description || ''}
                onChange={handleChange}
                placeholder="Detailed description of the instrument"
                rows={3}
                disabled={loading}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group">
                <label>Type</label>
                <select
                  name="type"
                  value={formData.type || 'Stock'}
                  onChange={handleChange}
                  disabled={loading}
                >
                  <option value="Stock">Stock</option>
                  <option value="Crypto">Crypto</option>
                  <option value="Cfd">CFD</option>
                  <option value="Etf">ETF</option>
                  <option value="Forex">Forex</option>
                </select>
              </div>

              <div className="form-group">
                <label>Pillar</label>
                <select
                  name="pillar"
                  value={formData.pillar || 'General'}
                  onChange={handleChange}
                  disabled={loading}
                >
                  {ACCOUNT_PILLARS.map(pillar => (
                    <option key={pillar} value={pillar}>
                      {pillar}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group">
                <label>Base Currency</label>
                <input
                  type="text"
                  name="baseCurrency"
                  value={formData.baseCurrency || ''}
                  onChange={handleChange}
                  placeholder="e.g., USD, BTC"
                  disabled={loading}
                />
              </div>

              <div className="form-group">
                <label>Quote Currency</label>
                <input
                  type="text"
                  name="quoteCurrency"
                  value={formData.quoteCurrency || ''}
                  onChange={handleChange}
                  placeholder="e.g., USD"
                  disabled={loading}
                />
              </div>
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-cancel" onClick={onClose} disabled={loading}>
              Anuluj
            </button>
            <button type="submit" className="btn-confirm" disabled={loading}>
              {loading ? '⏳ Zapisywanie...' : (instrument ? '💾 Aktualizuj' : '➕ Utwórz')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
