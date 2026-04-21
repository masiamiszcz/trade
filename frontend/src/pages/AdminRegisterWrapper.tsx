import React from 'react';
import { useSearchParams } from 'react-router-dom';
import { AdminRegisterPage } from './AdminRegisterPage';
import { AdminRegisterViaInvitePage } from './AdminRegisterViaInvitePage';

/**
 * Smart wrapper that routes between:
 * - AdminRegisterPage: Bootstrap (create super admin) - no token
 * - AdminRegisterViaInvitePage: Register invited admin - with token
 */
export const AdminRegisterWrapper: React.FC = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  // If token present → show invitation registration form
  // If no token → show bootstrap registration form
  return token ? <AdminRegisterViaInvitePage /> : <AdminRegisterPage />;
};
