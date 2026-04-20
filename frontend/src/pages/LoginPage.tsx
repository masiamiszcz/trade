import React, { useState, useEffect } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { authService } from '../services/AuthenticationService';
import { validateLoginForm } from '../utils/validators';
import { UserLoginInitialRequest } from '../types/userAuth';
import './LoginPage.css';


interface LocationState {
  from?: { pathname: string };
}

export const LoginPage: React.FC = () => {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as LocationState)?.from?.pathname || '/dashboard';

  const [loginData, setLoginData] = useState<UserLoginInitialRequest>({
    userNameOrEmail: '',
    password: '',
  });
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Set<string>>(new Set());
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (auth.isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [auth.isAuthenticated, from, navigate]);

  const handleChange = (field: keyof UserLoginInitialRequest, value: string) => {
    setLoginData((current) => ({ ...current, [field]: value }));

    if (touched.has(field)) {
      setFieldErrors((current) => ({
        ...current,
        [field]: validateLoginForm({ ...loginData, [field]: value }).find((err) => err.field === field)?.message || '',
      }));
    }
  };

  const handleBlur = (field: keyof UserLoginInitialRequest) => {
    setTouched((current) => new Set(current).add(field));
    const errors = validateLoginForm(loginData);
    const fieldError = errors.find((err) => err.field === field)?.message;
    setFieldErrors((current) => ({
      ...current,
      [field]: fieldError || '',
    }));
  };

  const isFormValid = (): boolean => validateLoginForm(loginData).length === 0;

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage('');

    const errors = validateLoginForm(loginData);
    if (errors.length > 0) {
      setFieldErrors(Object.fromEntries(errors.map((err) => [err.field, err.message])));
      return;
    }

    setLoading(true);

    try {
      const response = await authService.userLoginInitial(loginData);

      if (response.requiresTwoFactor) {
        // 2FA enabled - store temp session and redirect to verification
        auth.setTempSession(response.token, response.sessionId);
        navigate('/user/verify-2fa', {
          state: {
            sessionId: response.sessionId,
            username: response.username,
          },
          replace: true,
        });
      } else {
        // 2FA disabled - token already set in AuthenticationService
        navigate(from, { replace: true });
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Błąd logowania';
      setErrorMessage(message);
      setLoading(false);
    }
  };

  if (auth.isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Logowanie</h1>
        <p className="auth-subtitle">Zaloguj się do Trading Platform</p>
        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="userNameOrEmail" className="form-label">
              Nazwa użytkownika lub email
            </label>
            <input
              id="userNameOrEmail"
              type="text"
              value={loginData.userNameOrEmail}
              onChange={(e) => handleChange('userNameOrEmail', e.target.value)}
              onBlur={() => handleBlur('userNameOrEmail')}
              className={`form-input ${fieldErrors.userNameOrEmail ? 'error' : ''}`}
              placeholder="np. trader123 lub email@example.com"
              disabled={loading}
            />
            {fieldErrors.userNameOrEmail && (
              <div className="field-error">{fieldErrors.userNameOrEmail}</div>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="password" className="form-label">
              Hasło
            </label>
            <input
              id="password"
              type="password"
              value={loginData.password}
              onChange={(e) => handleChange('password', e.target.value)}
              onBlur={() => handleBlur('password')}
              className={`form-input ${fieldErrors.password ? 'error' : ''}`}
              placeholder="••••••••"
              disabled={loading}
            />
            {fieldErrors.password && <div className="field-error">{fieldErrors.password}</div>}
          </div>

          {errorMessage && <div className="error-message">{errorMessage}</div>}

          <button type="submit" disabled={loading || !isFormValid()} className="btn-primary">
            {loading ? 'Logowanie...' : 'Zaloguj się'}
          </button>
        </form>

        <div className="auth-footer">
          <p>
            Nie masz konta?{' '}
            <Link to="/register" className="link">
              Zarejestruj się
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};
