import { useCallback, useState } from 'react';
import { httpClient } from '../../services/http/HttpClient';
import { API_CONFIG } from '../../config/apiConfig';
import { useAdminAuth } from './useAdminAuth';

export interface DeleteUserResponse {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  status: string;
  twoFactorEnabled: boolean;
  createdAtUtc: string;
}

export interface DeleteUserError {
  error: string;
}

export const useDeleteUser = () => {
  const { token } = useAdminAuth();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const deleteUser = useCallback(
    async (userId: string): Promise<DeleteUserResponse | null> => {
      if (!token) {
        const msg = 'Not authenticated';
        setError(msg);
        throw new Error(msg);
      }

      setLoading(true);
      setError(null);

      try {
        console.log('🔴 Requesting user deletion for:', userId);

        // DELETE /api/admin/users/{id}
        const response = await httpClient.fetch<DeleteUserResponse>({
          url: `${API_CONFIG.endpoints.adminUsers.all}/${userId}`,
          method: 'DELETE',
        });

        console.log('✅ User successfully deleted:', response);
        return response;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to delete user';
        setError(message);
        console.error('Error deleting user:', err);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    [token]
  );

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return { deleteUser, loading, error, clearError };
};
