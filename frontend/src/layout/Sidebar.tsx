import React from 'react';
import './Sidebar.css';

interface SidebarProps {
  isOpen?: boolean;
  children?: React.ReactNode;
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen = false, children }) => {
  return (
    <aside className={`app-sidebar ${isOpen ? 'sidebar-open' : ''}`}>
      <div className="sidebar-content">
        {children}
      </div>
    </aside>
  );
};
