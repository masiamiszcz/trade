
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAdminAuth } from '../hooks/admin/useAdminAuth';
import { AdminSidebar } from '../components/admin/AdminSidebar';
import { AdminNavbar } from '../components/admin/AdminNavbar';
import { AdminHeader } from '../components/admin/AdminHeader';
import { DashboardContent } from '../components/admin/Dashboard/DashboardContent';
import { ApprovalsContent } from '../components/admin/Approvals/ApprovalsContent';
import { InstrumentsContent } from '../components/admin/Instruments/InstrumentsContent';
import { AuditLogsContent } from '../components/admin/Auditlogs/AuditLogsContent';
import { UsersContent } from '../components/admin/Users/UsersContent';
// import AdminAuditLogsContent from '../components/admin/Auditlogs/AdminAuditLogsContent';
import './AdminDashboardPage.css';

type TabType = 'dashboard' | 'approvals' | 'instruments' | 'audit-logs' | 'users';

export const AdminDashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { token, isTempToken, clearSession, isTokenExpired } = useAdminAuth();
  const [activeTab, setActiveTab] = useState<TabType>('dashboard');

  // Protected route - check admin authentication and token expiration
  useEffect(() => {
    // Check if not authenticated or has temp token (not final JWT)
    if (!token || isTempToken) {
      clearSession();
      navigate('/admin/login', { replace: true });
      return;
    }

    // Check if token is expired
    if (isTokenExpired()) {
      clearSession();
      navigate('/admin/login', { replace: true });
    }
  }, [token, isTempToken, navigate, clearSession, isTokenExpired]);

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
      // case 'history':
      //   return <AdminAuditLogsContent />;
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
          <AdminHeader title={`${activeTab.charAt(0).toUpperCase() + activeTab.slice(1)} Panel`} />
          <div className="admin-content">
            {renderContent()}
          </div>
        </div>
      </div>
    </div>
  );
};
