
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/AuthenticationService';
import { useAdminAuth } from '../hooks/admin/useAdminAuth';
import { AdminLoginRequest } from '../types/adminAuth';

/**
 * Admin Login Page - Step 1 of 2FA
 * VPN RESTRICTED (10.8.0.0/24)
 * Only admins with 2FA can login
 */
export const AdminLoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { setSession, isAuthenticated } = useAdminAuth();

  const [loginData, setLoginData] = useState<AdminLoginRequest>({
    usernameOrEmail: '',
    password: '',
  });

  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [loading, setLoading] = useState(false);

  // Redirect if already authenticated
  if (isAuthenticated) {
    navigate('/admin/dashboard', { replace: true });
    return null;
  }

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!loginData.usernameOrEmail.trim()) {
      errors.usernameOrEmail = 'Podaj nazwę użytkownika lub email';
    }

    if (!loginData.password) {
      errors.password = 'Podaj hasło';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleChange = (field: keyof AdminLoginRequest, value: string) => {
    setLoginData((prev) => ({ ...prev, [field]: value }));
    if (fieldErrors[field]) {
      setFieldErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage('');

    if (!validateForm()) {
      return;
    }

    setLoading(true);

    try {
      const result = await authService.adminLogin(loginData);

      if (!result.token) {
        setErrorMessage('Nieznany błąd - brak tokenu');
        setLoading(false);
        return;
      }

      if (result.requiresTwoFactor) {
        // 2FA required - navigate to 2FA verification
        navigate('/admin/verify-2fa', {
          state: {
            sessionId: result.sessionId,
            username: loginData.usernameOrEmail,
          },
        });
      } else {
        // No 2FA, redirect to dashboard
        navigate('/admin/dashboard', { replace: true });
      }
    } catch (err: any) {
      setErrorMessage(err?.message || 'Nieznany błąd');
      console.error('Login error:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card admin-card">
        <div className="admin-badge">🔐 ADMIN PANEL</div>
        <h1>Panel Administratora</h1>
        <p className="admin-subtitle">Dostęp restricted do VPN (10.8.0.0/24)</p>

        {errorMessage && <div className="error-message">{errorMessage}</div>}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="usernameOrEmail">Nazwa użytkownika lub email</label>
            <input
              id="usernameOrEmail"
              type="text"
              value={loginData.usernameOrEmail}
              onChange={(e) => handleChange('usernameOrEmail', e.target.value)}
              placeholder="admin"
              disabled={loading}
              className={fieldErrors.usernameOrEmail ? 'error' : ''}
            />
            {fieldErrors.usernameOrEmail && (
              <span className="field-error">{fieldErrors.usernameOrEmail}</span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="password">Hasło</label>
            <input
              id="password"
              type="password"
              value={loginData.password}
              onChange={(e) => handleChange('password', e.target.value)}
              placeholder="••••••••"
              disabled={loading}
              className={fieldErrors.password ? 'error' : ''}
            />
            {fieldErrors.password && <span className="field-error">{fieldErrors.password}</span>}
          </div>

          <button
            type="submit"
            disabled={loading || !loginData.usernameOrEmail || !loginData.password}
            className="btn-primary btn-large"
          >
            {loading ? 'Logowanie...' : 'Dalej do 2FA'}
          </button>
        </form>

        <div className="admin-info">
          <p>⚠️ Krok 1 z 2: Weryfikacja hasła</p>
          <p>Po zatwierdzeniu przejdziesz do weryfikacji 2FA</p>
        </div>

        <div className="admin-links">
          <button
            type="button"
            className="btn-link"
            onClick={() => navigate('/admin/register')}
          >
            Czy to Twoja pierwsza rejestracja? Utwórz Super Admina
          </button>
        </div>
      </div>
    </div>
  );
};
