import { useState } from 'react';
import { AdminUser } from '../../types/admin';

export const useAdminUsers = () => {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const changeUserRole = async (userId: string, newRole: 'User' | 'Admin') => {
    // Stub implementation
    console.log(`Change user ${userId} to role ${newRole}`);
  };

  return {
    users,
    loading,
    error,
    changeUserRole,
  };
};
