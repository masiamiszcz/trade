import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authService } from '../services/AuthenticationService';
import { useAuth } from '../hooks/useAuth';
import { validateRegisterForm } from '../utils/validators';
import { UserRegisterInitialRequest } from '../types/userAuth';
import { CustomSelect, SelectOption } from '../components/CustomSelect';
import './RegisterPage.css';


export const RegisterPage: React.FC = () => {
  const auth = useAuth();
  const navigate = useNavigate();

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
  const [successMessage, setSuccessMessage] = useState<string>('');
  const [loading, setLoading] = useState(false);

  const handleChange = (field: keyof UserRegisterInitialRequest, value: string) => {
    setFormData((current) => ({ ...current, [field]: value }));

    if (touched.has(field)) {
      setFieldErrors((current) => ({
        ...current,
        [field]: validateRegisterForm({ ...formData, [field]: value }).find((err) => err.field === field)?.message || '',
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
    setSuccessMessage('');

    const errors = validateRegisterForm(formData);
    if (errors.length > 0) {
      setFieldErrors(Object.fromEntries(errors.map((err) => [err.field, err.message])));
      return;
    }

    setLoading(true);
    console.log('🔵 RegisterPage: Button clicked, form submitted');
    console.log('📤 RegisterPage: Sending registration request:', formData);

    try {
      console.log('⏳ RegisterPage: Waiting for API response...');
      const response = await authService.userRegisterInitial(formData);

      console.log('✅ RegisterPage: API response received:', response);
      console.log('🔐 RegisterPage: Response contains - token:', !!response.token, ', sessionId:', !!response.sessionId);
      console.log('📋 RegisterPage: Response contains - qrCodeDataUrl:', !!response.qrCodeDataUrl, ', manualKey:', !!response.manualKey);

      // Store temp session data in context + navigate to 2FA setup
      console.log('💾 RegisterPage: Storing temp session in context');
      auth.setTempSession(response.token, response.sessionId);

      console.log('🚀 RegisterPage: Navigating to /register/2fa-setup');
      navigate('/register/2fa-setup', {
        state: {
          qrCodeDataUrl: response.qrCodeDataUrl,
          manualKey: response.manualKey,
          backupCodes: response.backupCodes,
          message: response.message,
        },
        replace: true,
      });
      console.log('✨ RegisterPage: Navigation triggered!');
    } catch (error) {
      console.error('❌ RegisterPage: Error occurred:', error);
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
        <h1>Rejestracja</h1>
        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="username">Nazwa użytkownika</label>
            <input
              id="username"
              type="text"
              value={formData.username}
              onChange={(e) => handleChange('username', e.target.value)}
              onBlur={() => handleBlur('username')}
              className={fieldErrors.username ? 'input-error' : ''}
            />
            {fieldErrors.username && <span className="error-text">{fieldErrors.username}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              onBlur={() => handleBlur('email')}
              className={fieldErrors.email ? 'input-error' : ''}
            />
            {fieldErrors.email && <span className="error-text">{fieldErrors.email}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="firstName">Imię</label>
            <input
              id="firstName"
              type="text"
              value={formData.firstName}
              onChange={(e) => handleChange('firstName', e.target.value)}
              onBlur={() => handleBlur('firstName')}
              className={fieldErrors.firstName ? 'input-error' : ''}
            />
            {fieldErrors.firstName && <span className="error-text">{fieldErrors.firstName}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="lastName">Nazwisko</label>
            <input
              id="lastName"
              type="text"
              value={formData.lastName}
              onChange={(e) => handleChange('lastName', e.target.value)}
              onBlur={() => handleBlur('lastName')}
              className={fieldErrors.lastName ? 'input-error' : ''}
            />
            {fieldErrors.lastName && <span className="error-text">{fieldErrors.lastName}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="password">Hasło</label>
            <input
              id="password"
              type="password"
              value={formData.password}
              onChange={(e) => handleChange('password', e.target.value)}
              onBlur={() => handleBlur('password')}
              className={fieldErrors.password ? 'input-error' : ''}
            />
            {fieldErrors.password && <span className="error-text">{fieldErrors.password}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="baseCurrency">Waluta bazowa</label>
            <CustomSelect
              id="baseCurrency"
              value={formData.baseCurrency || 'PLN'}
              options={currencyOptions}
              onChange={(value) => handleChange('baseCurrency', value)}
              className={fieldErrors.baseCurrency ? 'input-error' : ''}
            />
            {fieldErrors.baseCurrency && <span className="error-text">{fieldErrors.baseCurrency}</span>}
          </div>  

          <button type="submit" disabled={loading || !isFormValid()}>
            {loading ? 'Rejestruję...' : 'Zarejestruj się'}
          </button>

          {errorMessage && <div className="form-error">{errorMessage}</div>}
          {successMessage && <div className="form-success">{successMessage}</div>}
        </form>

        <p className="form-footer">
          Masz już konto? <Link to="/login">Zaloguj się</Link>
        </p>
      </div>
    </div>
  );
};
