import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/AuthenticationService';
import { useAuth } from '../hooks/useAuth';
import { validateRegisterForm } from '../utils/validators';
import { UserRegisterInitialRequest } from '../types/userAuth';
import { CustomSelect, SelectOption } from '../components/CustomSelect';
import './RegisterPage.css';

export const UserRegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const auth = useAuth();

  // Clear old temp session on component mount (from previous registration attempts)
  useEffect(() => {
    auth.clearTempSession();
  }, []);

  const currencyOptions: SelectOption[] = [
    { value: 'PLN', label: 'PLN (Polski Złoty)' },
    { value: 'EUR', label: 'EUR (Euro)' },
    { value: 'USD', label: 'USD (Dolar USA)' },
    { value: 'GBP', label: 'GBP (Funt Brytyjski)' },
    { value: 'CHF', label: 'CHF (Frank Szwajcarski)' },
  ];

  const [formData, setFormData] = useState<UserRegisterInitialRequest>({
    username: '',
    email: '',
    firstName: '',
    lastName: '',
    password: '',
    baseCurrency: 'PLN',
  });

  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Set<string>>(new Set());
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [loading, setLoading] = useState(false);

  const handleChange = (field: keyof UserRegisterInitialRequest, value: string) => {
    setFormData((current) => ({ ...current, [field]: value }));

    if (touched.has(field)) {
      // Validate on change if field was already touched
      const errors = validateRegisterForm(formData);
      const fieldError = errors.find((err) => err.field === field)?.message;
      setFieldErrors((current) => ({
        ...current,
        [field]: fieldError || '',
      }));
    }
  };

  const handleBlur = (field: keyof UserRegisterInitialRequest) => {
    setTouched((current) => new Set(current).add(field));
    const errors = validateRegisterForm(formData);
    const fieldError = errors.find((err) => err.field === field)?.message;
    setFieldErrors((current) => ({
      ...current,
      [field]: fieldError || '',
    }));
  };

  const isFormValid = (): boolean => validateRegisterForm(formData).length === 0;

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage('');

    const errors = validateRegisterForm(formData);
    if (errors.length > 0) {
      setFieldErrors(Object.fromEntries(errors.map((err) => [err.field, err.message])));
      return;
    }

    setLoading(true);

    try {
      const response = await authService.userRegisterInitial(formData);

      // Store temp session data in context + navigate to 2FA setup
      auth.setTempSession(response.token, response.sessionId);

      navigate('/register/2fa-setup', {
        state: {
          qrCodeDataUrl: response.qrCodeDataUrl,
          manualKey: response.manualKey,
          backupCodes: response.backupCodes,
          message: response.message,
        },
        replace: true,
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Błąd podczas rejestracji';
      const lowerMessage = message.toLowerCase();

      if (lowerMessage.includes('username')) {
        setFieldErrors({ ...fieldErrors, username: message });
      } else if (lowerMessage.includes('email')) {
        setFieldErrors({ ...fieldErrors, email: message });
      } else {
        setErrorMessage(message);
      }
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Zarejestruj się</h1>
        <p className="auth-subtitle">Utwórz konto w Trading Platform</p>

        {errorMessage && <div className="error-message">{errorMessage}</div>}

        <form onSubmit={handleSubmit}>
          {/* Username */}
          <div className="form-group">
            <label htmlFor="username" className="form-label">
              Nazwa użytkownika
            </label>
            <input
              id="username"
              type="text"
              value={formData.username}
              onChange={(e) => handleChange('username', e.target.value)}
              onBlur={() => handleBlur('username')}
              className={`form-input ${fieldErrors.username ? 'error' : ''}`}
              placeholder="np. trader123"
              autoComplete="username"
              disabled={loading}
            />
            {fieldErrors.username && (
              <div className="field-error">{fieldErrors.username}</div>
            )}
          </div>

          {/* Email */}
          <div className="form-group">
            <label htmlFor="email" className="form-label">
              Email
            </label>
            <input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              onBlur={() => handleBlur('email')}
              className={`form-input ${fieldErrors.email ? 'error' : ''}`}
              placeholder="twoj@email.com"
              autoComplete="email"
              disabled={loading}
            />
            {fieldErrors.email && <div className="field-error">{fieldErrors.email}</div>}
          </div>

          {/* First Name */}
          <div className="form-group">
            <label htmlFor="firstName" className="form-label">
              Imię
            </label>
            <input
              id="firstName"
              type="text"
              value={formData.firstName}
              onChange={(e) => handleChange('firstName', e.target.value)}
              onBlur={() => handleBlur('firstName')}
              className={`form-input ${fieldErrors.firstName ? 'error' : ''}`}
              placeholder="Jan"
              autoComplete="given-name"
              disabled={loading}
            />
            {fieldErrors.firstName && (
              <div className="field-error">{fieldErrors.firstName}</div>
            )}
          </div>

          {/* Last Name */}
          <div className="form-group">
            <label htmlFor="lastName" className="form-label">
              Nazwisko
            </label>
            <input
              id="lastName"
              type="text"
              value={formData.lastName}
              onChange={(e) => handleChange('lastName', e.target.value)}
              onBlur={() => handleBlur('lastName')}
              className={`form-input ${fieldErrors.lastName ? 'error' : ''}`}
              placeholder="Kowalski"
              autoComplete="family-name"
              disabled={loading}
            />
            {fieldErrors.lastName && (
              <div className="field-error">{fieldErrors.lastName}</div>
            )}
          </div>

          {/* Password */}
          <div className="form-group">
            <label htmlFor="password" className="form-label">
              Hasło
            </label>
            <input
              id="password"
              type="password"
              value={formData.password}
              onChange={(e) => handleChange('password', e.target.value)}
              onBlur={() => handleBlur('password')}
              className={`form-input ${fieldErrors.password ? 'error' : ''}`}
              placeholder="••••••••"
              autoComplete="new-password"
              disabled={loading}
            />
            {fieldErrors.password && (
              <div className="field-error">{fieldErrors.password}</div>
            )}
          </div>

          {/* Base Currency */}
          <div className="form-group">
            <label htmlFor="baseCurrency" className="form-label">
              Waluta bazowa
            </label>
            <CustomSelect
              id="baseCurrency"
              options={currencyOptions}
              value={formData.baseCurrency}
              onChange={(value) => handleChange('baseCurrency', value)}
              disabled={loading}
            />
            {fieldErrors.baseCurrency && (
              <div className="field-error">{fieldErrors.baseCurrency}</div>
            )}
          </div>

          {/* Submit Button */}
          <button
            type="submit"
            className="btn-primary"
            disabled={!isFormValid() || loading}
          >
            {loading ? 'Przetwarzanie...' : 'Przejdź do konfiguracji 2FA'}
          </button>
        </form>

        {/* Login Link */}
        <div className="auth-footer">
          <p>
            Masz już konto?{' '}
            <a href="/user/login" className="link">
              Zaloguj się
            </a>
          </p>
        </div>
      </div>
    </div>
  );
};
