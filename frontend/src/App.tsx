import React from 'react';
import { Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { MainLayout } from './layout/MainLayout';
import { LoginPage } from './pages/LoginPage';
import { UserRegisterPage } from './pages/UserRegisterPage';
import { UserSetup2FAPage } from './pages/UserSetup2FAPage';
import { UserVerify2FAPage } from './pages/UserVerify2FAPage';
import { DashboardPage } from './pages/DashboardPage';
import { AccountPage } from './pages/AccountPage';
import { StockDashboard } from './pages/StockDashboard';
import { CryptoDashboard } from './pages/CryptoDashboard';
import { CryptoPage } from './pages/CryptoPage';
import { CFDDashboard } from './pages/CFDDashboard';
import { useAuth } from './hooks/useAuth';

// ===== ADMIN AUTH SYSTEM =====
import { AdminAuthProvider } from './hooks/admin/AdminAuthContext';
import { AdminLoginPage } from './pages/AdminLoginPage';
import { AdminRegisterPage } from './pages/AdminRegisterPage';
import { AdminRegisterViaInvitePage } from './pages/AdminRegisterViaInvitePage';
import { AdminSetup2FAPage } from './pages/AdminSetup2FAPage';
import { AdminVerify2FAPage } from './pages/AdminVerify2FAPage';
import { AdminDashboardPage } from './pages/AdminDashboardPage';
import { AdminRegisterWrapper } from './pages/AdminRegisterWrapper';

import './App.css';

const App: React.FC = () => {
  const auth = useAuth();
  const location = useLocation();

  const isDashboard = location.pathname === '/dashboard';

  return (
    <AdminAuthProvider>
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
        <Route
          path="/dashboard"
          element={
            <MainLayout isDashboard={isDashboard}>
              <DashboardPage />
            </MainLayout>
          }
        />

        {/* Account Page - Konto i Transakcje */}
        <Route
          path="/dashboard/account"
          element={
            <MainLayout>
              <AccountPage />
            </MainLayout>
          }
        />

        {/* Market Dashboards - Stock, Crypto, CFD */}
        <Route
          path="/dashboard/stock"
          element={
            <MainLayout>
              <StockDashboard />
            </MainLayout>
          }
        />
        <Route
          path="/dashboard/crypto"
          element={
            <MainLayout>
              <CryptoDashboard />
            </MainLayout>
          }
        />
        <Route
          path="/dashboard/crypto/:symbol"
          element={
            <MainLayout>
              <CryptoPage />
            </MainLayout>
          }
        />
        <Route
          path="/dashboard/cfd"
          element={
            <MainLayout>
              <CFDDashboard />
            </MainLayout>
          }
        />

        {/* ===== ADMIN AUTH ROUTES ===== */}
        {/* Admin registration - bootstrap (no token) or invitation (with token) */}
        <Route
          path="/admin/register"
          element={
            <MainLayout>
              <AdminRegisterWrapper />
            </MainLayout>
          }
        />

        {/* Admin login + 2FA flow */}
        <Route
          path="/admin/login"
          element={
            <MainLayout>
              <AdminLoginPage />
            </MainLayout>
          }
        />
        <Route
          path="/admin/verify-2fa"
          element={
            <MainLayout>
              <AdminVerify2FAPage />
            </MainLayout>
          }
        />

        {/* 2FA setup (after bootstrap/register) */}
        <Route
          path="/admin/setup-2fa"
          element={
            <MainLayout>
              <AdminSetup2FAPage />
            </MainLayout>
          }
        />

        {/* Admin dashboard (protected - NO MainLayout, standalone) */}
        <Route path="/admin/dashboard" element={<AdminDashboardPage />} />

        {/* Catch-all */}
        <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AdminAuthProvider>
    );
  };

  export default App;
