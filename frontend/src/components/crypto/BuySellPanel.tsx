import React, { useState } from 'react';
import { useBuySell } from '../../hooks/useBuySell';
import { useAccount } from '../../hooks/useAccount';
import './BuySellPanel.css';

interface BuySellPanelProps {
  symbol?: string;
  latestPrice?: number;
  onTransactionSuccess?: () => void;
}

export const BuySellPanel: React.FC<BuySellPanelProps> = ({ 
  symbol: presetSymbol = 'BTCUSDT', 
  latestPrice = 0,
  onTransactionSuccess 
}) => {
  const [operation, setOperation] = useState<'buy' | 'sell'>('buy');
  const [quantity, setQuantity] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const { account } = useAccount();
  const { loading, error, buy, sell, reset } = useBuySell(() => {
    reset();
    setQuantity('');
    setSuccessMessage('Transakcja zakończona pomyślnie!');
    setTimeout(() => {
      setSuccessMessage(null);
      onTransactionSuccess?.();
    }, 2000);
  });

  const TOLERANCE_PERCENT = 1;

  const getMaxAmount = () => {
    if (operation === 'buy') {
      if (!latestPrice || latestPrice <= 0) return 0;
      const balanceInUsd = (account?.availableBalance || 0) * 0.25;
      return balanceInUsd / latestPrice;
    }
    return 0;
  };

  const setToMax = () => {
    const maxQty = getMaxAmount();
    if (maxQty > 0) {
      setQuantity(maxQty.toFixed(8));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setValidationError(null);
    setSuccessMessage(null);

    if (!quantity || isNaN(parseFloat(quantity)) || parseFloat(quantity) <= 0) {
      setValidationError('Ilość musi być większa niż 0');
      return;
    }

    if (!latestPrice || latestPrice <= 0) {
      setValidationError('Cena rynkowa nie jest dostępna');
      return;
    }

    if (operation === 'buy') {
      const totalNeeded = parseFloat(quantity) * latestPrice;
      const balanceInUsd = (account?.availableBalance || 0) * 0.25;
      
      if (totalNeeded > balanceInUsd) {
        setValidationError(
          `Niewystarczające saldo. Potrzebujesz: $${totalNeeded.toFixed(2)}, Dostępne: $${balanceInUsd.toFixed(2)}`
        );
        return;
      }
    }

    try {
      if (operation === 'buy') {
        await buy({
          symbol: presetSymbol.toUpperCase(),
          quantity: parseFloat(quantity),
          requestedPrice: latestPrice,
          tolerancePercent: TOLERANCE_PERCENT,
        });
      } else {
        await sell({
          symbol: presetSymbol.toUpperCase(),
          quantity: parseFloat(quantity),
          requestedPrice: latestPrice,
          tolerancePercent: TOLERANCE_PERCENT,
        });
      }
    } catch (err) {
      // Error from hook already set in state
      // Scroll to top to show error message
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  const balanceInUsd = (account?.availableBalance || 0) * 0.25;
  const totalValue = quantity && latestPrice 
    ? (parseFloat(quantity) * latestPrice).toFixed(2)
    : '0.00';

  return (
    <div className="buy-sell-panel">
      <div className="panel-header">
        <h2>Kupuj lub Sprzedawaj - {presetSymbol}</h2>
      </div>

      <div className="form-container">
        {successMessage && (
          <div className="alert success">
            ✓ {successMessage}
          </div>
        )}

        {error && (
          <div className="alert error">
            ✗ {error}
          </div>
        )}

        {validationError && (
          <div className="alert error">
            ✗ {validationError}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          {/* Operation Toggle: Buy / Sell */}
          <div className="operation-toggle">
            <button
              type="button"
              className={`toggle-btn ${operation === 'buy' ? 'active' : ''}`}
              onClick={() => {
                setOperation('buy');
                setValidationError(null);
              }}
              disabled={loading}
            >
              💳 KOPEN
            </button>
            <button
              type="button"
              className={`toggle-btn ${operation === 'sell' ? 'active' : ''}`}
              onClick={() => {
                setOperation('sell');
                setValidationError(null);
              }}
              disabled={loading}
            >
              💰 SPRZEDAJ
            </button>
          </div>

          {/* Current Market Price */}
          {latestPrice > 0 && (
            <div className="form-group">
              <label>Aktualna cena rynkowa</label>
              <div className="price-display">${latestPrice.toFixed(2)}</div>
            </div>
          )}

          {/* Available Balance */}
          <div className="form-group">
            <label>Dostępne saldo konta</label>
            <div className="balance-display">
              {account?.availableBalance ? (
                <>
                  <div className="balance-original">
                    {account.availableBalance.toFixed(2)} {account.currency}
                  </div>
                  <div className="balance-converted">
                    ≈ ${balanceInUsd.toFixed(2)} USD
                  </div>
                </>
              ) : (
                <div className="balance-original">Ładowanie...</div>
              )}
            </div>
          </div>

          {/* Amount Input */}
          <div className="form-group">
            <label>Ilość {operation === 'buy' ? 'do kupienia' : 'do sprzedania'}</label>
            <div className="input-with-action">
              <input
                type="number"
                step="0.00000001"
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                placeholder="0.00000000"
                disabled={loading}
              />
              <button
                type="button"
                className="max-btn"
                onClick={setToMax}
                disabled={loading}
              >
                MAX
              </button>
            </div>
          </div>

          {/* Summary */}
          <div className="summary-card">
            <div className="summary-row">
              <span>Ilość:</span>
              <strong>{quantity || '0'} {presetSymbol.split('USDT')[0]}</strong>
            </div>
            <div className="summary-row">
              <span>Cena za sztukę:</span>
              <strong>${latestPrice.toFixed(2)}</strong>
            </div>
            <div className="summary-row summary-total">
              <span>Całkowita wartość:</span>
              <strong>${totalValue}</strong>
            </div>
          </div>

          {/* Submit Button */}
          <button
            type="submit"
            className={`btn-submit btn-${operation}`}
            disabled={loading || !quantity || !latestPrice || latestPrice <= 0}
          >
            {loading ? 'Przetwarzanie...' : operation === 'buy' ? '💳 Kup teraz' : '💰 Sprzedaj teraz'}
          </button>
        </form>
      </div>
    </div>
  );
};
