import { useEffect, useState } from 'react';
import { useAuth } from './useAuth';
import { httpClient } from '../services/http/HttpClient';
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

    try {
      const response = await httpClient.fetch<AccountDto | ApiResponse<AccountDto>>({
        url: '/account/main',
        method: 'GET',
      });

      let accountData: AccountDto | null = null;
      
      if (response && typeof response === 'object') {
        if ('data' in response && response.data) {
          accountData = response.data as AccountDto;
        } else if ('accountNumber' in response && 'availableBalance' in response) {
          accountData = response as AccountDto;
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
