import React, { useState, useMemo } from 'react';
import { usePortfolio } from '../../hooks/usePortfolio';
import { useAccount } from '../../hooks/useAccount';
import { useExchangeRate } from '../../hooks/useExchangeRate';
import { BuySellForm } from './BuySellForm';
import './PortfolioPanel.css';

interface PortfolioPanelProps {
  onRefresh?: () => void;
}

export const PortfolioPanel: React.FC<PortfolioPanelProps> = ({ onRefresh }) => {
  const { portfolio, loading, error, refresh } = usePortfolio();
  const { account } = useAccount();
  const { rate: exchangeRate, loading: rateLoading } = useExchangeRate('USD', account?.currency || 'USD');
  const [showForm, setShowForm] = useState(false);
  const [selectedSymbol, setSelectedSymbol] = useState<string | null>(null);

  // Memoized conversion function - converts USD to user's currency
  const convertValue = useMemo(() => {
    return (usdValue: number) => {
      if (!account || !exchangeRate || exchangeRate === 1) {
        return usdValue;
      }
      return usdValue * exchangeRate;
    };
  }, [account, exchangeRate]);

  const handleRefresh = () => {
    refresh();
    onRefresh?.();
  };

  const handleOpenForm = (symbol: string) => {
    setSelectedSymbol(symbol);
    setShowForm(true);
  };

  const handleFormSuccess = () => {
    setShowForm(false);
    setSelectedSymbol(null);
    handleRefresh();
  };

  if (loading && !portfolio) {
    return <div className="portfolio-panel">Loading portfolio...</div>;
  }

  if (error && !portfolio) {
    return (
      <div className="portfolio-panel error">
        <p>Error: {error}</p>
        <button onClick={handleRefresh}>Retry</button>
      </div>
    );
  }

  if (!portfolio) {
    return (
      <div className="portfolio-panel">
        <p>No portfolio data available</p>
        <button onClick={handleRefresh}>Load Portfolio</button>
      </div>
    );
  }

  return (
    <div className="portfolio-panel">
      <div className="portfolio-header">
        <h2>
          Portfolio ({portfolio?.baseCurrency}
          {account?.currency && portfolio?.baseCurrency !== account.currency && ` → ${account.currency}`})
        </h2>
        <button className="btn-refresh" onClick={handleRefresh} disabled={loading || rateLoading}>
          {loading || rateLoading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      <div className="portfolio-summary">
        <div className="summary-item">
          <label>Total Value:</label>
          <div className="value-row">
            <span className="value usd-value">${portfolio.summary.totalValue.toFixed(2)}</span>
            {exchangeRate && exchangeRate !== 1 && (
              <span className="value converted-value">
                {convertValue(portfolio.summary.totalValue).toFixed(2)} {account?.currency}
              </span>
            )}
          </div>
        </div>
        <div className="summary-item">
          <label>P&L:</label>
          <div className="value-row">
            <span className={`value usd-value ${portfolio.summary.totalPnL >= 0 ? 'positive' : 'negative'}`}>
              ${portfolio.summary.totalPnL.toFixed(2)}
            </span>
            {exchangeRate && exchangeRate !== 1 && (
              <span className={`value converted-value ${portfolio.summary.totalPnL >= 0 ? 'positive' : 'negative'}`}>
                {convertValue(portfolio.summary.totalPnL).toFixed(2)} {account?.currency}
              </span>
            )}
          </div>
        </div>
        <div className="summary-item">
          <label>P&L %:</label>
          <span className={`value ${portfolio.summary.totalPnLPercent >= 0 ? 'positive' : 'negative'}`}>
            {portfolio.summary.totalPnLPercent.toFixed(2)}%
          </span>
        </div>
      </div>

      <div className="positions-list">
        <h3>Positions</h3>
        {portfolio.positions.length === 0 ? (
          <p>No positions</p>
        ) : (
          <table className="positions-table">
            <thead>
              <tr>
                <th>Symbol</th>
                <th>Quantity</th>
                <th>Avg Price</th>
                <th>Current Price</th>
                <th>Total Value</th>
                <th>P&L</th>
                <th>P&L %</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {portfolio.positions.map(position => (
                <tr key={position.symbol}>
                  <td>{position.symbol}</td>
                  <td>{position.quantity.toFixed(8)}</td>
                  <td>${position.averageOpenPrice.toFixed(2)}</td>
                  <td>${position.currentPrice.toFixed(2)}</td>
                  <td>
                    <div className="value-stack">
                      <span className="usd-value">${position.totalValue.toFixed(2)}</span>
                      {exchangeRate && exchangeRate !== 1 && (
                        <span className="converted-value">
                          {convertValue(position.totalValue).toFixed(2)} {account?.currency}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className={position.pnL >= 0 ? 'positive' : 'negative'}>
                    <div className="value-stack">
                      <span className="usd-value">${position.pnL.toFixed(2)}</span>
                      {exchangeRate && exchangeRate !== 1 && (
                        <span className="converted-value">
                          {convertValue(position.pnL).toFixed(2)} {account?.currency}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className={position.pnLPercent >= 0 ? 'positive' : 'negative'}>
                    {position.pnLPercent.toFixed(2)}%
                  </td>
                  <td>
                    <button 
                      className="btn-small"
                      onClick={() => handleOpenForm(position.symbol)}
                    >
                      Trade
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showForm && (
        <BuySellForm
          defaultSymbol={selectedSymbol}
          onClose={() => setShowForm(false)}
          onSuccess={handleFormSuccess}
        />
      )}
    </div>
  );
};
