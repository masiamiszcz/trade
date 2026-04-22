import { useCallback, useState, useEffect, useRef } from 'react';
import { httpClient } from '../../services/http/HttpClient';
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
  const prevUsersRef = useRef<string>(''); // ✅ MEMO: Track previous data

  const fetchUsers = useCallback(async () => {
    if (!token) {
      setError('Not authenticated');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const data: UserListItem[] = await httpClient.fetch<UserListItem[]>({
        url: '/admin/users',
        method: 'GET',
      });

      const newUsersStr = JSON.stringify(data);
      
      // ✅ MEMO: Only update state if data actually changed
      if (newUsersStr !== prevUsersRef.current) {
        console.log('📝 Users CHANGED:', data.length, 'records - updating component');
        prevUsersRef.current = newUsersStr;
        setUsers(Array.isArray(data) ? data : []);
      } else {
        console.log('🔄 Users unchanged - NOT re-rendering component');
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch users';
      setError(message);
      console.error('Error fetching users:', err);
    } finally {
      setLoading(false);
    }
  }, [token]);

  // ✅ AUTO-REFRESH: Fetch every 10 seconds for FRESH data
  useEffect(() => {
    console.log('🎯 useEffect triggered - fetching users');
    fetchUsers();
    
    const refreshInterval = setInterval(() => {
      console.log('🔄 Auto-refresh triggered for users');
      fetchUsers();
    }, 10000); // 10 seconds
    
    return () => clearInterval(refreshInterval);
  }, [fetchUsers]);

  return { users, loading, error, fetchUsers };
};
