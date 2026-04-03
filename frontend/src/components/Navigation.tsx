import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import './Navigation.css';


interface NavigationProps {
  onMenuClick?: () => void;
}

export const Navigation: React.FC<NavigationProps> = ({ onMenuClick }) => {
  const auth = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    auth.logout();
    navigate('/login');
  };

  return (
    <div className="app-nav">
      {onMenuClick && (
        <button className="menu-toggle" onClick={onMenuClick} aria-label="Toggle menu">
          <span className="hamburger-line"></span>
          <span className="hamburger-line"></span>
          <span className="hamburger-line"></span>
        </button>
      )}

      <div className="app-brand">
        <Link to="/" className="brand-link">
          Trading Platform
        </Link>
      </div>

      <nav className="nav-links">
        {auth.isAuthenticated ? (
          <>
            <button className="nav-button" onClick={handleLogout} type="button">
              Wyloguj
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="nav-link">
              Logowanie
            </Link>
            <Link to="/register" className="nav-link">
              Rejestracja
            </Link>
          </>
        )}
      </nav>
    </div>
  );
};
