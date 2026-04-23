import { useEffect, useState } from 'react';
import { useAuth } from './useAuth';
import { httpClient } from '../services/http/HttpClient';

export interface Transaction {
  id: string;
  accountId: string;
  type: string;
  amount: number;
  currency: string;
  description: string;
  createdAtUtc: string;
  status: string;
}

export interface TransactionResponse {
  transactions: Transaction[];
  total: number;
  page: number;
  pageSize: number;
}

// TODO: Backend endpoint /api/account/transactions nie istnieje
// TYMCZASOWY MOCK - do usunięcia gdy backend będzie gotowy
const MOCK_TRANSACTIONS: Transaction[] = [
  {
    id: '1',
    accountId: 'acc-001',
    type: 'DEPOSIT',
    amount: 1000,
    currency: 'PLN',
    description: 'Wpłata pieniędzy na konto',
    createdAtUtc: new Date(Date.now() - 86400000).toISOString(),
    status: 'COMPLETED',
  },
  {
    id: '2',
    accountId: 'acc-001',
    type: 'BUY',
    amount: -500,
    currency: 'PLN',
    description: 'Zakup akcji PKOBP',
    createdAtUtc: new Date(Date.now() - 172800000).toISOString(),
    status: 'COMPLETED',
  },
  {
    id: '3',
    accountId: 'acc-001',
    type: 'SELL',
    amount: 250,
    currency: 'PLN',
    description: 'Sprzedaż akcji KGHM',
    createdAtUtc: new Date(Date.now() - 259200000).toISOString(),
    status: 'COMPLETED',
  },
  {
    id: '4',
    accountId: 'acc-001',
    type: 'DIVIDEND',
    amount: 150,
    currency: 'PLN',
    description: 'Dywidenda z akcji PKNORLEN',
    createdAtUtc: new Date(Date.now() - 345600000).toISOString(),
    status: 'COMPLETED',
  },
  {
    id: '5',
    accountId: 'acc-001',
    type: 'FEE',
    amount: -10,
    currency: 'PLN',
    description: 'Opłata za prowadzenie rachunku',
    createdAtUtc: new Date(Date.now() - 432000000).toISOString(),
    status: 'COMPLETED',
  },
];

export const useTransactions = (pageSize: number = 20, page: number = 1) => {
  const { token } = useAuth();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [total, setTotal] = useState(0);

  const fetchTransactions = async (currentPage: number = page) => {
    if (!token) {
      setError('Not authenticated');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      // TYMCZASOWO: Użyj mock'ów zamiast backendu
      // TODO: Zmienić na httpClient.fetch gdy backend będzie gotowy
      const startIndex = (currentPage - 1) * pageSize;
      const endIndex = startIndex + pageSize;
      const paginatedTransactions = MOCK_TRANSACTIONS.slice(startIndex, endIndex);

      setTransactions(paginatedTransactions);
      setTotal(MOCK_TRANSACTIONS.length);

      // ALTERNATYWA - prawdziwy backend call (gdy będzie endpoint):
      /*
      const response = await httpClient.fetch<TransactionResponse>({
        url: `/account/transactions?page=${currentPage}&pageSize=${pageSize}`,
        method: 'GET',
      });

      if (response && typeof response === 'object') {
        if ('transactions' in response) {
          setTransactions(response.transactions || []);
          setTotal(response.total || 0);
        } else {
          setTransactions([]);
          setError('Invalid transaction response from server.');
        }
      }
      */
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTransactions(page);
  }, [token, page, pageSize]);

  return { transactions, loading, error, total, refetch: fetchTransactions };
};
