import { useCallback, useState } from 'react';
import { useAdminAuth } from './useAdminAuth';

export interface UserListItem {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  status: string;
  createdAtUtc: string;
}

export interface GetUsersResponse {
  $values: UserListItem[];
}

export const useGetUsers = () => {
  const { token } = useAdminAuth();
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchUsers = useCallback(async () => {
    if (!token) {
      setError('Not authenticated');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/admin/users', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch users: ${response.status}`);
      }

      const data: UserListItem[] = await response.json();
      setUsers(Array.isArray(data) ? data : []);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch users';
      setError(message);
      console.error('Error fetching users:', err);
    } finally {
      setLoading(false);
    }
  }, [token]);

  return { users, loading, error, fetchUsers };
};
