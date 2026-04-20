
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { adminAuthService } from '../services/AdminAuthService';
import { useAdminAuth } from '../hooks/admin/useAdminAuth';
import { AdminBootstrapRequest } from '../types/adminAuth';

export const AdminRegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const { setSession } = useAdminAuth();

  const [formData, setFormData] = useState<AdminBootstrapRequest>({
    username: '',
    email: '',
    password: '',
  });

  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!formData.username.trim()) {
      errors.username = 'Nazwa użytkownika jest wymagana';
    } else if (formData.username.length < 3) {
      errors.username = 'Nazwa użytkownika musi mieć co najmniej 3 znaki';
    }

    if (!formData.email.trim()) {
      errors.email = 'Email jest wymagany';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = 'Podaj prawidłowy email';
    }

    if (!formData.password) {
      errors.password = 'Hasło jest wymagane';
    } else if (formData.password.length < 8) {
      errors.password = 'Hasło musi mieć co najmniej 8 znaków';
    } else if (!/(?=.*[a-z])/.test(formData.password)) {
      errors.password = 'Hasło musi zawierać małe litery';
    } else if (!/(?=.*[A-Z])/.test(formData.password)) {
      errors.password = 'Hasło musi zawierać wielkie litery';
    } else if (!/(?=.*\d)/.test(formData.password)) {
      errors.password = 'Hasło musi zawierać cyfry';
    } else if (!/(?=.*[!@#$%^&*])/.test(formData.password)) {
      errors.password = 'Hasło musi zawierać znaki specjalne (!@#$%^&*)';
    }

    if (formData.password !== confirmPassword) {
      errors.confirmPassword = 'Hasła się nie zgadzają';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleChange = (field: keyof AdminBootstrapRequest, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (fieldErrors[field]) {
      setFieldErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!validateForm()) {
      return;
    }

    setLoading(true);

    try {
      const result = await adminAuthService.adminBootstrap(formData);

      if (result.error) {
        setError(result.error.message || 'Błąd przy tworzeniu Super Admina');
        return;
      }

      if (!result.data) {
        setError('Nieznany błąd');
        return;
      }

      // Zapisz sesję (temp token do setup 2FA)
      setSession({
        token: result.data.token,
        sessionId: result.data.sessionId,
        isTempToken: true,
        requiresTwoFactor: result.data.requiresTwoFactorSetup,
        username: formData.username,
      });

      // Redirect do setup 2FA
      navigate('/admin/setup-2fa', {
        state: {
          manualKey: result.data.manualKey,
          backupCodes: result.data.backupCodes,
        },
      });
    } catch (err) {
      setError('Nieznany błąd - sprawdź konsolę');
      console.error('Bootstrap error:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card admin-card">
        <div className="admin-badge">🔐 REJESTRACJA</div>
        <h1>Tworzenie Super Admina</h1>
        <p className="admin-subtitle">Pierwsza rejestracja - bootstrap systemu</p>

        {error && <div className="error-message">{error}</div>}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="username">Nazwa użytkownika</label>
            <input
              id="username"
              type="text"
              value={formData.username}
              onChange={(e) => handleChange('username', e.target.value)}
              placeholder="admin"
              disabled={loading}
              className={fieldErrors.username ? 'error' : ''}
            />
            {fieldErrors.username && <span className="field-error">{fieldErrors.username}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              placeholder="admin@company.com"
              disabled={loading}
              className={fieldErrors.email ? 'error' : ''}
            />
            {fieldErrors.email && <span className="field-error">{fieldErrors.email}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="password">Hasło</label>
            <input
              id="password"
              type="password"
              value={formData.password}
              onChange={(e) => handleChange('password', e.target.value)}
              placeholder="••••••••"
              disabled={loading}
              className={fieldErrors.password ? 'error' : ''}
            />
            {fieldErrors.password && <span className="field-error">{fieldErrors.password}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword">Potwierdź hasło</label>
            <input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="••••••••"
              disabled={loading}
              className={fieldErrors.confirmPassword ? 'error' : ''}
            />
            {fieldErrors.confirmPassword && <span className="field-error">{fieldErrors.confirmPassword}</span>}
          </div>

          <button
            type="submit"
            disabled={loading}
            className="btn-primary btn-large"
          >
            {loading ? 'Tworzenie...' : 'Utwórz Super Admina'}
          </button>
        </form>

        <div className="password-requirements">
          <h4>Wymagania hasła:</h4>
          <ul>
            <li>✓ Minimum 8 znaków</li>
            <li>✓ Małe litery (a-z)</li>
            <li>✓ Wielkie litery (A-Z)</li>
            <li>✓ Cyfry (0-9)</li>
            <li>✓ Znaki specjalne (!@#$%^&*)</li>
          </ul>
        </div>
      </div>
    </div>
  );
};
