import React from 'react';
import './Header.css';

interface HeaderProps {
  children?: React.ReactNode;
}

export const Header: React.FC<HeaderProps> = ({ children }) => {
  return (
    <header className="app-header">
      <div className="header-content">
        {children}
      </div>
    </header>
  );
};
