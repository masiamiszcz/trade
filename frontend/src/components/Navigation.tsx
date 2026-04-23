import React from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useAccount } from '../hooks/useAccount';
import { UserAccountDropdown } from './common/UserAccountDropdown';
import './Navigation.css';


interface NavigationProps {
  onMenuClick?: () => void;
}

export const Navigation: React.FC<NavigationProps> = ({ onMenuClick }) => {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { account } = useAccount();

  const getUserName = (): string => {
    // Try to extract from token (JWT decoded)
    if (auth.token) {
      try {
        const payload = JSON.parse(atob(auth.token.split('.')[1]));
        return payload.unique_name || payload.name || 'User';
      } catch {
        return 'User';
      }
    }
    return 'User';
  };

  const isActive = (path: string): boolean => {
    return location.pathname === path;
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
        <Link to="/dashboard" className="brand-link">
          Trading Platform
        </Link>
      </div>

      {/* Market Navigation Buttons */}
      {auth.isAuthenticated && (
        <div className="market-nav">
          <Link
            to="/dashboard/stock"
            className={`market-button ${isActive('/dashboard/stock') ? 'active' : ''}`}
          >
            📈 Stock
          </Link>
          <Link
            to="/dashboard/crypto"
            className={`market-button ${isActive('/dashboard/crypto') ? 'active' : ''}`}
          >
            ₿ Crypto
          </Link>
          <Link
            to="/dashboard/cfd"
            className={`market-button ${isActive('/dashboard/cfd') ? 'active' : ''}`}
          >
            💱 CFD
          </Link>
        </div>
      )}

      <nav className="nav-links">
        {auth.isAuthenticated ? (
          <>
            {/* Balance/Wallet Section - Link to Account Page */}
            <Link
              to="/dashboard/account"
              className={`wallet-section ${isActive('/dashboard/account') ? 'active' : ''}`}
              aria-label="Account details"
            >
              <span className="wallet-label">Saldo:</span>
              <span className="wallet-amount">
                {account ? (
                  <>
                    {account.availableBalance.toLocaleString('pl-PL', {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2,
                    })}
                    <span className="wallet-currency">{account.currency}</span>
                  </>
                ) : (
                  '-- PLN'
                )}
              </span>
            </Link>

            {/* Account Dropdown */}
            <UserAccountDropdown userName={getUserName()} />
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
