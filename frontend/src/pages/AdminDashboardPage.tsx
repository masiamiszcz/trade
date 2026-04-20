
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAdminAuth } from '../hooks/admin/useAdminAuth';
import { isTokenExpired } from '../services/adminService';
import { AdminSidebar } from '../components/admin/AdminSidebar';
import { AdminNavbar } from '../components/admin/AdminNavbar';
import { DashboardContent } from '../components/admin/Dashboard/DashboardContent';
import { ApprovalsContent } from '../components/admin/Approvals/ApprovalsContent';
import { InstrumentsContent } from '../components/admin/Instruments/InstrumentsContent';
import { AuditLogsContent } from '../components/admin/Auditlogs/AuditLogsContent';
import { UsersContent } from '../components/admin/Users/UsersContent';
import './AdminDashboardPage.css';

type TabType = 'dashboard' | 'approvals' | 'instruments' | 'audit-logs' | 'users';

export const AdminDashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { session, clearSession } = useAdminAuth();
  const [activeTab, setActiveTab] = useState<TabType>('dashboard');

  // Protected route - check admin authentication and token expiration
  useEffect(() => {
    if (!session.token || session.isTempToken) {
      clearSession();
      navigate('/admin/login', { replace: true });
      return;
    }

    // Check if token is expired
    if (isTokenExpired(session.token)) {
      clearSession();
      navigate('/admin/login', { replace: true });
    }
  }, [session.token, session.isTempToken, navigate, clearSession]);

  const handleLogout = () => {
    clearSession();
    navigate('/admin/login', { replace: true });
  };

  const renderContent = () => {
    switch (activeTab) {
      case 'dashboard':
        return <DashboardContent />;
      case 'approvals':
        return <ApprovalsContent />;
      case 'instruments':
        return <InstrumentsContent />;
      case 'audit-logs':
        return <AuditLogsContent />;
      case 'users':
        return <UsersContent />;
      default:
        return <DashboardContent />;
    }
  };

  return (
    <div className="admin-dashboard">
      <AdminNavbar onLogout={handleLogout} />
      <div className="admin-layout">
        <AdminSidebar activeTab={activeTab} onTabChange={setActiveTab} onLogout={handleLogout} />
        <div className="admin-main">
          <div className="admin-content">
            {renderContent()}
          </div>
        </div>
      </div>
    </div>
  );
};
