import React from 'react';
import './AdminHeader.css';

interface AdminHeaderProps {
  title: string;
}

export const AdminHeader: React.FC<AdminHeaderProps> = ({ title }) => {
  return (
    <header className="admin-header">
      <div className="header-left">
        <h1>{title}</h1>
      </div>
    </header>
  );
};
