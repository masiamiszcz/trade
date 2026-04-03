import React from 'react';
import './Footer.css';

interface FooterProps {
  children?: React.ReactNode;
}

export const Footer: React.FC<FooterProps> = ({ children }) => {
  return (
    <footer className="app-footer">
      <div className="footer-content">
        {children || (
          <p>&copy; 2026 Trading Platform. All rights reserved.</p>
        )}
      </div>
    </footer>
  );
};
