import React, { useState } from 'react';
import { Column, DataTable } from '../common/DataTable';
import { useTransactions, Transaction } from '../../hooks/useTransactions';
import './TransactionHistory.css';

export const TransactionHistory: React.FC = () => {
  const { transactions, loading, error, total, refetch } = useTransactions(20, 1);
  const [currentPage, setCurrentPage] = useState(1);

  const columns: Column<Transaction>[] = [
    {
      key: 'createdAtUtc',
      label: 'Data',
      width: '20%',
      render: (value: string) => {
        const date = new Date(value);
        return date.toLocaleDateString('pl-PL', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit',
          hour: '2-digit',
          minute: '2-digit',
        });
      },
    },
    {
      key: 'type',
      label: 'Typ',
      width: '15%',
      render: (value: string) => {
        const typeLabel: Record<string, string> = {
          DEPOSIT: 'Wpłata',
          WITHDRAWAL: 'Wypłata',
          BUY: 'Kupno',
          SELL: 'Sprzedaż',
          DIVIDEND: 'Dywidenda',
          FEE: 'Opłata',
        };
        return typeLabel[value] || value;
      },
    },
    {
      key: 'description',
      label: 'Opis',
      width: '35%',
    },
    {
      key: 'amount',
      label: 'Kwota',
      width: '15%',
      render: (value: number, item: Transaction) => {
        const isPositive = value >= 0;
        return (
          <span className={`amount ${isPositive ? 'positive' : 'negative'}`}>
            {isPositive ? '+' : ''}{value.toLocaleString('pl-PL', {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })} {item.currency}
          </span>
        );
      },
    },
    {
      key: 'status',
      label: 'Status',
      width: '15%',
      render: (value: string) => {
        const statusLabel: Record<string, string> = {
          COMPLETED: 'Ukończona',
          PENDING: 'Oczekująca',
          FAILED: 'Nieudana',
          CANCELLED: 'Anulowana',
        };
        return (
          <span className={`status-badge status-${value.toLowerCase()}`}>
            {statusLabel[value] || value}
          </span>
        );
      },
    },
  ];

  const handlePageChange = (newPage: number) => {
    setCurrentPage(newPage);
    refetch(newPage);
  };

  const pageCount = Math.ceil(total / 20);

  return (
    <div className="transaction-history-container">
      <div className="transaction-history-header">
        <h2>Historia Transakcji</h2>
        <p className="transaction-count">Razem: {total} transakcji</p>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="transaction-table-wrapper">
        <DataTable<Transaction>
          columns={columns}
          data={transactions}
          keyExtractor={(item) => item.id}
          loading={loading}
          emptyMessage="Brak transakcji"
        />
      </div>

      {pageCount > 1 && (
        <div className="transaction-pagination">
          <button
            onClick={() => handlePageChange(currentPage - 1)}
            disabled={currentPage === 1}
            className="pagination-btn"
          >
            ← Poprzednia
          </button>

          <div className="pagination-info">
            Strona {currentPage} z {pageCount}
          </div>

          <button
            onClick={() => handlePageChange(currentPage + 1)}
            disabled={currentPage === pageCount}
            className="pagination-btn"
          >
            Następna →
          </button>
        </div>
      )}
    </div>
  );
};
