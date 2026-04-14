import { useEffect, useState } from 'react';
import { useAuth } from './useAuth';
import { ApiResponse, AccountDto } from '../types';

export const useAccount = () => {
  const { token } = useAuth();
  const [account, setAccount] = useState<AccountDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchMainAccount = async () => {
    if (!token) {
      setLoading(false);
      setError('Not authenticated');
      return;
    }

    setLoading(true);
    setError(null);

    const baseUrl = import.meta.env.VITE_API_URL?.trim().replace(/\/$/, '') || 'http://localhost:5001';
    const url = `${baseUrl}/api/account/main`;

    try {
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const text = await response.text();
        if (response.status === 404) {
          setError('Main account not found');
        } else if (response.status === 401) {
          setError('Unauthorized');
        } else {
          setError(`Error: ${response.status} ${response.statusText} - ${text}`);
        }
        setLoading(false);
        return;
      }

      const responseText = await response.text();
      let parsed: any;
      try {
        parsed = JSON.parse(responseText);
      } catch {
        setError('Invalid server response: expected JSON.');
        setLoading(false);
        return;
      }

      let accountData: AccountDto | null = null;
      if (parsed && typeof parsed === 'object') {
        if ('data' in parsed && parsed.data) {
          accountData = parsed.data as AccountDto;
        } else if ('accountNumber' in parsed && 'availableBalance' in parsed) {
          accountData = parsed as AccountDto;
        }
      }

      if (accountData) {
        setAccount(accountData);
      } else {
        setError('Invalid account response from server.');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (token) {
      fetchMainAccount();
    }
  }, [token]);

  return {
    account,
    loading,
    error,
    refetch: fetchMainAccount,
  };
};
