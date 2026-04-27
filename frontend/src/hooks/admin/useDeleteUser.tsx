import { useState, useCallback } from 'react';
import { httpClient } from '../../services/http/HttpClient';
import { API_CONFIG } from '../../config/apiConfig';
import { useAdminAuth } from './useAdminAuth';
import type { AdminRequest } from '../../types/admin';

export const useDeleteUser = () => {
  const { token } = useAdminAuth();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const deleteUser = useCallback(async (
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
      await httpClient.fetch<AdminRequest>({
        url: `${API_CONFIG.endpoints.adminUsers.all}/${userId}`,
        method: 'DELETE',
        body: JSON.stringify({ reason }),
      });
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Nie udało się usunąć użytkownika';
      setError(message);
      return false;
    } finally {
      setLoading(false);
    }
  }, [token]);

  return { deleteUser, loading, error };
};
