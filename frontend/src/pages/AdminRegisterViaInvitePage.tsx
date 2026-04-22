
import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { authService } from '../services/AuthenticationService';
import { useAdminAuth } from '../hooks/admin/useAdminAuth';
import { AdminRegisterViaInviteRequest } from '../types/adminAuth';

export const AdminRegisterViaInvitePage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { setSession } = useAdminAuth();

  const invitationToken = searchParams.get('token');

  const [formData, setFormData] = useState<Omit<AdminRegisterViaInviteRequest, 'token'>>({
    username: '',
    password: '',
  });

  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!invitationToken) {
      setError('Brak tokenu zaproszenia');
    }
  }, [invitationToken]);

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!formData.username.trim()) {
      errors.username = 'Nazwa użytkownika jest wymagana';
    } else if (formData.username.length < 3) {
      errors.username = 'Nazwa użytkownika musi mieć co najmniej 3 znaki';
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

  const handleChange = (field: string, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (fieldErrors[field]) {
      setFieldErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!invitationToken) {
      setError('Brak tokenu zaproszenia');
      return;
    }

    if (!validateForm()) {
      return;
    }

    setLoading(true);

    try {
      const request: AdminRegisterViaInviteRequest = {
        token: invitationToken,
        username: formData.username,
        password: formData.password,
      };

      const result = await authService.adminRegisterViaInvite(request);

      if (!result.token) {
        setError('Nieznany błąd - brak tokenu');
        setLoading(false);
        return;
      }

      // Admin registered, redirect to dashboard
      navigate('/admin/dashboard', { replace: true });
    } catch (err: any) {
      setError(err?.message || 'Nieznany błąd');
      console.error('Registration error:', err);
    } finally {
      setLoading(false);
    }
  };

  if (!invitationToken) {
    return (
      <div className="auth-page">
        <div className="auth-card">
          <div className="error-message">{error || 'Nieprawidłowy link zaproszenia'}</div>
          <button
            onClick={() => navigate('/admin/login')}
            className="btn-primary"
          >
            Wróć do logowania
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="auth-page">
      <div className="auth-card admin-card">
        <div className="admin-badge">💌 ZAPROSZENIE</div>
        <h1>Rejestracja Administratora</h1>
        <p className="admin-subtitle">Zostałeś zaproszony do panelu administratora</p>

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
            {loading ? 'Rejestracja...' : 'Zarejestruj się'}
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
