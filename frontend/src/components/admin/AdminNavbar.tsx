
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAdminAuth } from '../../hooks/admin/useAdminAuth';
import './AdminNavbar.css';

interface AdminNavbarProps {
  onLogout?: () => void;
}

export const AdminNavbar: React.FC<AdminNavbarProps> = ({ onLogout }) => {
  const navigate = useNavigate();
  const { username, clearSession } = useAdminAuth();
  const [currentTime, setCurrentTime] = useState<string>('');

  useEffect(() => {
    const updateTime = () => {
      const now = new Date();
      setCurrentTime(
        now.toLocaleString('pl-PL', {
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
          year: 'numeric',
          month: '2-digit',
          day: '2-digit'
        })
      );
    };

    updateTime();
    const interval = setInterval(updateTime, 1000);

    return () => clearInterval(interval);
  }, []);

  const handleLogout = () => {
    clearSession();
    if (onLogout) {
      onLogout();
    }
    navigate('/admin/login', { replace: true });
  };

  return (
    <div className="admin-nav">
      <div className="admin-brand">
        <h1 className="brand-title">💼 Trading Platform Admin Panel</h1>
      </div>

      <div className="admin-nav-center">
        <div className="time-display">
          <span className="time-label">🕐</span>
          <span className="time-value">{currentTime}</span>
        </div>
      </div>

      <div className="admin-nav-right">
        <div className="user-info">
          <span className="user-icon">👤</span>
          <span className="username">{username || 'Admin'}</span>
        </div>

        <button 
          className="nav-button logout-btn" 
          onClick={handleLogout} 
          type="button"
          title="Wyloguj się z panelu administracyjnego"
        >
          🚪 Wyloguj
        </button>
      </div>
    </div>
  );
};
