import React from 'react';
import React, { useEffect, useState } from 'react';
import { useAdminAuth } from '../../hooks/admin/useAdminAuth';
import './AdminHeader.css';

interface AdminHeaderProps {
  title: string;
}

export const AdminHeader: React.FC<AdminHeaderProps> = ({ title }) => {
  const [currentTime, setCurrentTime] = useState<string>('');
  const { session } = useAdminAuth();

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

  return (
    <header className="admin-header">
      <div className="header-left">
        <h1>{title}</h1>
      </div>

      <div className="header-right">
        <div className="time-display">
          <span className="time-label">🕐</span>
          <span className="time-value">{currentTime}</span>
        </div>

        <div className="user-info">
          <span className="user-icon">👤</span>
          <span className="username">{session.username || 'Admin'}</span>
        </div>
      </div>
    </header>
  );
};
