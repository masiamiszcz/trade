import React, { useState } from 'react';
import { useBuySell } from '../../hooks/useBuySell';
import './BuySellForm.css';

interface BuySellFormProps {
  defaultSymbol?: string | null;
  onClose: () => void;
  onSuccess: () => void;
}

export const BuySellForm: React.FC<BuySellFormProps> = ({
  defaultSymbol,
  onClose,
  onSuccess,
}) => {
  const [operation, setOperation] = useState<'buy' | 'sell'>('buy');
  const [symbol, setSymbol] = useState(defaultSymbol || '');
  const [quantity, setQuantity] = useState('');
  const [requestedPrice, setRequestedPrice] = useState('');
  const [tolerance, setTolerance] = useState('1');
  const [validationError, setValidationError] = useState<string | null>(null);

  const { loading, error, success, buy, sell, reset } = useBuySell(() => {
    reset();
    setTimeout(() => {
      onClose();
      onSuccess();
    }, 1000);
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setValidationError(null);

    // Validation
    if (!symbol.trim()) {
      setValidationError('Symbol is required');
      return;
    }
    if (!quantity || isNaN(parseFloat(quantity)) || parseFloat(quantity) <= 0) {
      setValidationError('Quantity must be greater than 0');
      return;
    }
    if (!requestedPrice || isNaN(parseFloat(requestedPrice)) || parseFloat(requestedPrice) <= 0) {
      setValidationError('Price must be greater than 0');
      return;
    }

    try {
      if (operation === 'buy') {
        await buy({
          symbol: symbol.toUpperCase(),
          quantity: parseFloat(quantity),
          requestedPrice: parseFloat(requestedPrice),
          tolerancePercent: parseFloat(tolerance) || 1,
        });
      } else {
        await sell({
          symbol: symbol.toUpperCase(),
          quantity: parseFloat(quantity),
          requestedPrice: parseFloat(requestedPrice),
          tolerancePercent: parseFloat(tolerance) || 1,
        });
      }
    } catch (err) {
      // Error already handled in hook
    }
  };

  return (
    <div className="buy-sell-form-overlay">
      <div className="buy-sell-form">
        <button className="btn-close" onClick={onClose}>×</button>

        <h2>{operation === 'buy' ? 'Buy' : 'Sell'} Crypto</h2>

        {success && (
          <div className="alert success">
            Transaction completed successfully!
          </div>
        )}

        {error && (
          <div className="alert error">
            {error}
          </div>
        )}

        {validationError && (
          <div className="alert error">
            {validationError}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Operation</label>
            <div className="radio-group">
              <label>
                <input
                  type="radio"
                  value="buy"
                  checked={operation === 'buy'}
                  onChange={e => setOperation(e.target.value as 'buy' | 'sell')}
                />
                Buy
              </label>
              <label>
                <input
                  type="radio"
                  value="sell"
                  checked={operation === 'sell'}
                  onChange={e => setOperation(e.target.value as 'buy' | 'sell')}
                />
                Sell
              </label>
            </div>
          </div>

          <div className="form-group">
            <label>Symbol</label>
            <input
              type="text"
              value={symbol}
              onChange={e => setSymbol(e.target.value)}
              placeholder="e.g., BTCUSDT"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label>Quantity</label>
            <input
              type="number"
              step="0.00000001"
              value={quantity}
              onChange={e => setQuantity(e.target.value)}
              placeholder="0.0"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label>Requested Price</label>
            <input
              type="number"
              step="0.01"
              value={requestedPrice}
              onChange={e => setRequestedPrice(e.target.value)}
              placeholder="0.00"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label>Price Tolerance (%)</label>
            <input
              type="number"
              step="0.1"
              value={tolerance}
              onChange={e => setTolerance(e.target.value)}
              placeholder="1"
              disabled={loading}
            />
          </div>

          <div className="form-actions">
            <button
              type="button"
              className="btn-secondary"
              onClick={onClose}
              disabled={loading}
            >
              Cancel
            </button>
            <button
              type="submit"
              className={`btn-primary ${operation}`}
              disabled={loading}
            >
              {loading ? 'Processing...' : (operation === 'buy' ? 'Buy' : 'Sell')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
