import React, { useState, useEffect } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { validateLoginForm } from '../utils/validators';
import { LoginRequest } from '../types';
import './LoginPage.css';


interface LocationState {
  from?: { pathname: string };
}

export const LoginPage: React.FC = () => {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as LocationState)?.from?.pathname || '/portfolio';

  const [loginData, setLoginData] = useState<LoginRequest>({
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

  const handleChange = (field: keyof LoginRequest, value: string) => {
    setLoginData((current) => ({ ...current, [field]: value }));

    if (touched.has(field)) {
      setFieldErrors((current) => ({
        ...current,
        [field]: validateLoginForm({ ...loginData, [field]: value }).find((err) => err.field === field)?.message || '',
      }));
    }
  };

  const handleBlur = (field: keyof LoginRequest) => {
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
    const result = await auth.login(loginData);
    setLoading(false);

    if (result.error) {
      setErrorMessage(result.error.message);
      return;
    }

    navigate(from, { replace: true });
  };

  if (auth.isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Logowanie</h1>
        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="userNameOrEmail">Nazwa użytkownika lub email</label>
            <input
              id="userNameOrEmail"
              type="text"
              value={loginData.userNameOrEmail}
              onChange={(e) => handleChange('userNameOrEmail', e.target.value)}
              onBlur={() => handleBlur('userNameOrEmail')}
              className={fieldErrors.userNameOrEmail ? 'input-error' : ''}
            />
            {fieldErrors.userNameOrEmail && <span className="error-text">{fieldErrors.userNameOrEmail}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="password">Hasło</label>
            <input
              id="password"
              type="password"
              value={loginData.password}
              onChange={(e) => handleChange('password', e.target.value)}
              onBlur={() => handleBlur('password')}
              className={fieldErrors.password ? 'input-error' : ''}
            />
            {fieldErrors.password && <span className="error-text">{fieldErrors.password}</span>}
          </div>

          <button type="submit" disabled={loading || !isFormValid()}>
            {loading ? 'Logowanie...' : 'Zaloguj się'}
          </button>

          {errorMessage && <div className="form-error">{errorMessage}</div>}
        </form>

        <p className="form-footer">
          Nie masz konta? <Link to="/register">Zarejestruj się</Link>
        </p>
      </div>
    </div>
  );
};
