import { useState, useCallback } from 'react';
import { httpClient } from '../../services/http/HttpClient';
import { API_CONFIG } from '../../config/apiConfig';
import { useAdminAuth } from './useAdminAuth';
import type { UserStatus } from '../../types/admin';

export const useBlockUser = () => {
  const { token } = useAdminAuth();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const blockUser = useCallback(async (
    userId: string,
    reason: string,
    durationMs: number = 48 * 60 * 60 * 1000
  ): Promise<boolean> => {
    // ✅ GUARD: Check if token exists BEFORE making request
    if (!token) {
      setError('Brak autoryzacji - zaloguj się jako admin');
      return false;
    }

    setLoading(true);
    setError(null);

    try {
      await httpClient.fetch<UserStatus>({
        url: `${API_CONFIG.endpoints.adminUsers.all}/${userId}/block`,
        method: 'POST',
        body: JSON.stringify({
          reason,
          durationMs,
          isPermanent: durationMs === 0
        }),
      });
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Nie udało się zablokować użytkownika';
      setError(message);
      return false;
    } finally {
      setLoading(false);
    }
  }, [token]);

  const unblockUser = useCallback(async (
    userId: string,
    reason: string
  ): Promise<boolean> => {
    // ✅ GUARD: Check if token exists BEFORE making request
    if (!token) {
      setError('Brak autoryzacji - zaloguj się jako admin');
      return false;
    }

    setLoading(true);
    setError(null);

    try {
      await httpClient.fetch<UserStatus>({
        url: `${API_CONFIG.endpoints.adminUsers.all}/${userId}/unblock`,
        method: 'POST',
        body: JSON.stringify({ reason }),
      });
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Nie udało się odblokować użytkownika';
      setError(message);
      return false;
    } finally {
      setLoading(false);
    }
  }, [token]);

  return { blockUser, unblockUser, loading, error };
};
