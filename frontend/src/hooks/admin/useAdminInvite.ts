import { useState, useCallback } from 'react';
import { useAdminAuth } from './useAdminAuth';

interface InviteResponse {
  token: string;
  email: string;
  expiresAt: string;
  invitationUrl: string;
}

export const useAdminInvite = () => {
  const { token } = useAdminAuth();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const inviteAdmin = useCallback(
    async (
      email: string,
      firstName: string,
      lastName: string
    ): Promise<string | null> => {
      if (!token) {
        setError('Nie jesteś zalogowany');
        return null;
      }

      setLoading(true);
      setError(null);
      setSuccessMessage(null);

      try {
        const response = await fetch('http://trading-backend:5001/api/auth/admin/invite', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify({
            email,
            firstName,
            lastName
          })
        });

        if (!response.ok) {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Nie udało się wygenerować linku zaproszenia');
        }

        const data: InviteResponse = await response.json();
        setSuccessMessage(`Link zaproszenia wysłany dla: ${email}`);
        return data.token;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Błąd sieciowy';
        setError(errorMessage);
        console.error('Admin invitation error:', err);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [token]
  );

  return { inviteAdmin, loading, error, successMessage };
};
