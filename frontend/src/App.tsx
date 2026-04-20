import React from 'react';
import { Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { MainLayout } from './layout/MainLayout';
import { LoginPage } from './pages/LoginPage';
import { UserRegisterPage } from './pages/UserRegisterPage';
import { UserSetup2FAPage } from './pages/UserSetup2FAPage';
import { UserVerify2FAPage } from './pages/UserVerify2FAPage';
import { DashboardPage } from './pages/DashboardPage';
import { useAuth } from './hooks/useAuth';
import './App.css';

const App: React.FC = () => {
  const auth = useAuth();
  const location = useLocation();

  const isDashboard = location.pathname === '/dashboard';

  return (
    <MainLayout isDashboard={isDashboard}>
      <Routes>
        <Route
          path="/"
          element={
            auth.isAuthenticated ? (
              <Navigate to="/dashboard" replace />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />
        {/* Legacy routes (keep for backwards compatibility) */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<UserRegisterPage />} />
        <Route path="/register/2fa-setup" element={<UserSetup2FAPage />} />

        {/* User auth routes (new 2FA flow) - aliases */}
        <Route path="/user/login" element={<LoginPage />} />
        <Route path="/user/register" element={<UserRegisterPage />} />
        <Route path="/user/register/2fa-setup" element={<UserSetup2FAPage />} />
        <Route path="/user/verify-2fa" element={<UserVerify2FAPage />} />

        {/* Dashboard */}
        <Route path="/dashboard" element={<DashboardPage />} />

        {/* Catch-all */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </MainLayout>
  );
};

export default App;
