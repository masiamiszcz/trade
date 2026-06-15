import React, { useState, useEffect } from 'react';
import { cryptoPortfolioService, CryptoTransactionDto } from '../../services/CryptoPortfolioService';
import './TransactionHistory.css';

interface TransactionHistoryProps {
  accountId?: string;
  limit?: number;
}

export const TransactionHistory: React.FC<TransactionHistoryProps> = ({
  limit = 50,
}) => {
  const [transactions, setTransactions] = useState<CryptoTransactionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [offset, setOffset] = useState(0);
  const [type, setType] = useState<string>('');
  const [status, setStatus] = useState<string>('');

  const fetchTransactions = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await cryptoPortfolioService.getTransactions(
        limit,
        offset,
        type || undefined,
        status || undefined
      );
      setTransactions(result.transactions || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch transactions');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTransactions();
  }, [offset, type, status, limit]);

  const handlePrevPage = () => {
    setOffset(Math.max(0, offset - limit));
  };

  const handleNextPage = () => {
    setOffset(offset + limit);
  };

  return (
    <div className="transaction-history">
      <h3>Transaction History</h3>

      <div className="filters">
        <select
          value={type}
          onChange={e => {
            setType(e.target.value);
            setOffset(0);
          }}
        >
          <option value="">All Types</option>
          <option value="BUY">Buy</option>
          <option value="SELL">Sell</option>
        </select>

        <select
          value={status}
          onChange={e => {
            setStatus(e.target.value);
            setOffset(0);
          }}
        >
          <option value="">All Status</option>
          <option value="COMPLETED">Completed</option>
          <option value="FAILED">Failed</option>
        </select>

        <button onClick={() => fetchTransactions()} disabled={loading}>
          {loading ? 'Loading...' : 'Refresh'}
        </button>
      </div>

      {error && (
        <div className="alert error">
          {error}
        </div>
      )}

      {transactions.length === 0 ? (
        <p className="no-data">No transactions found</p>
      ) : (
        <>
          <table className="transactions-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Symbol</th>
                <th>Type</th>
                <th>Quantity</th>
                <th>Requested Price</th>
                <th>Execution Price</th>
                <th>Total Value</th>
                <th>Status</th>
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map(tx => (
                <tr key={tx.id}>
                  <td>{new Date(tx.createdAt).toLocaleString()}</td>
                  <td>{tx.symbol}</td>
                  <td className={tx.type}>{tx.type}</td>
                  <td>{tx.quantity.toFixed(8)}</td>
                  <td>${tx.requestedPrice.toFixed(2)}</td>
                  <td>${tx.executionPrice.toFixed(2)}</td>
                  <td>${tx.totalValue.toFixed(2)}</td>
                  <td className={`status ${tx.status.toLowerCase()}`}>
                    {tx.status}
                  </td>
                  <td>{tx.errorMessage || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="pagination">
            <button onClick={handlePrevPage} disabled={offset === 0 || loading}>
              ← Previous
            </button>
            <span>
              Page {Math.floor(offset / limit) + 1} | Showing {transactions.length} items
            </span>
            <button onClick={handleNextPage} disabled={transactions.length < limit || loading}>
              Next →
            </button>
          </div>
        </>
      )}
    </div>
  );
};
