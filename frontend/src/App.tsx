import React, { useState } from 'react';
import './App.css';
import {
  validateUsername,
  validateEmail,
  validatePassword,
  validateFirstName,
  validateLastName,
  validateRegisterForm
} from './utils/validators';

type Mode = 'start' | 'register' | 'login' | 'loggedIn';

interface FieldErrors {
  userName?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  password?: string;
}

function App() {
  const [mode, setMode] = useState<Mode>('start');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [token, setToken] = useState('');
  const [loading, setLoading] = useState(false);

  const [registerData, setRegisterData] = useState({
    userName: '',
    email: '',
    firstName: '',
    lastName: '',
    password: ''
  });

  const [registerFieldErrors, setRegisterFieldErrors] = useState<FieldErrors>({});
  const [fieldTouched, setFieldTouched] = useState<Set<string>>(new Set());

  const [loginData, setLoginData] = useState({
    userNameOrEmail: '',
    password: ''
  });

  const apiBase = 'http://localhost:5001/api/auth';

  const openForm = (target: Mode) => {
    setError('');
    setMessage('');
    setMode(target);
    setRegisterFieldErrors({});
    setFieldTouched(new Set());
  };

  // Real-time validation for each field
  const handleRegisterFieldChange = (field: keyof typeof registerData, value: string) => {
    setRegisterData({ ...registerData, [field]: value });

    // Validate only if field has been touched
    if (fieldTouched.has(field)) {
      let fieldError: string | null = null;
      switch (field) {
        case 'userName':
          fieldError = validateUsername(value);
          break;
        case 'email':
          fieldError = validateEmail(value);
          break;
        case 'password':
          fieldError = validatePassword(value);
          break;
        case 'firstName':
          fieldError = validateFirstName(value);
          break;
        case 'lastName':
          fieldError = validateLastName(value);
          break;
      }

      setRegisterFieldErrors({
        ...registerFieldErrors,
        [field]: fieldError || undefined
      });
    }
  };

  const handleFieldBlur = (field: string) => {
    setFieldTouched(new Set([...fieldTouched, field]));

    // Validate on blur
    let fieldError: string | null = null;
    const registerField = field as keyof typeof registerData;
    const value = registerData[registerField];

    switch (field) {
      case 'userName':
        fieldError = validateUsername(value);
        break;
      case 'email':
        fieldError = validateEmail(value);
        break;
      case 'password':
        fieldError = validatePassword(value);
        break;
      case 'firstName':
        fieldError = validateFirstName(value);
        break;
      case 'lastName':
        fieldError = validateLastName(value);
        break;
    }

    setRegisterFieldErrors({
      ...registerFieldErrors,
      [field]: fieldError || undefined
    });
  };

  const isRegisterFormValid = (): boolean => {
    const errors = validateRegisterForm(registerData);
    return errors.length === 0 && Object.keys(registerFieldErrors).length === 0;
  };

  const handleRegister = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    setMessage('');

    // Final validation
    const allErrors = validateRegisterForm(registerData);
    if (allErrors.length > 0) {
      const errorMap: FieldErrors = {};
      allErrors.forEach(err => {
        errorMap[err.field as keyof FieldErrors] = err.message;
      });
      setRegisterFieldErrors(errorMap);
      return;
    }

    try {
      setLoading(true);
      const response = await fetch(`${apiBase}/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(registerData)
      });

      const payload = await response.json();
      if (!response.ok) {
        // Check if error is about unique constraints
        const errorMsg = payload.message || 'Błąd podczas rejestracji';
        if (errorMsg.includes('username')) {
          setRegisterFieldErrors({ ...registerFieldErrors, userName: errorMsg });
        } else if (errorMsg.includes('email')) {
          setRegisterFieldErrors({ ...registerFieldErrors, email: errorMsg });
        } else {
          setError(errorMsg);
        }
        return;
      }

      setMessage('Rejestracja zakończona sukcesem. Teraz możesz się zalogować.');
      setRegisterData({ userName: '', email: '', firstName: '', lastName: '', password: '' });
      setRegisterFieldErrors({});
      setFieldTouched(new Set());
      setMode('login');
    } catch (err) {
      setError('Nie udało się połączyć z serwerem.');
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    setMessage('');

    if (!loginData.userNameOrEmail || !loginData.password) {
      setError('Nazwa użytkownika/Email i hasło są wymagane.');
      return;
    }

    try {
      setLoading(true);
      const response = await fetch(`${apiBase}/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(loginData)
      });

      const payload = await response.json();
      if (!response.ok) {
        setError(payload.message || 'Błąd logowania');
        return;
      }

      setToken(payload.token);
      setMessage('Zalogowano pomyślnie');
      setMode('loggedIn');
    } catch (err) {
      setError('Nie udało się połączyć z serwerem.');
    } finally {
      setLoading(false);
    }
  };

  const renderForm = () => {
    if (mode === 'register') {
      return (
        <form className="auth-form" onSubmit={handleRegister}>
          <h2>Rejestracja</h2>

          <div className="form-group">
            <input
              value={registerData.userName}
              onChange={(e) => handleRegisterFieldChange('userName', e.target.value)}
              onBlur={() => handleFieldBlur('userName')}
              placeholder="Nazwa użytkownika"
              className={registerFieldErrors.userName ? 'input-error' : ''}
            />
            {registerFieldErrors.userName && (
              <span className="error-text">{registerFieldErrors.userName}</span>
            )}
          </div>

          <div className="form-group">
            <input
              value={registerData.email}
              onChange={(e) => handleRegisterFieldChange('email', e.target.value)}
              onBlur={() => handleFieldBlur('email')}
              type="email"
              placeholder="Email"
              className={registerFieldErrors.email ? 'input-error' : ''}
            />
            {registerFieldErrors.email && (
              <span className="error-text">{registerFieldErrors.email}</span>
            )}
          </div>

          <div className="form-group">
            <input
              value={registerData.firstName}
              onChange={(e) => handleRegisterFieldChange('firstName', e.target.value)}
              onBlur={() => handleFieldBlur('firstName')}
              placeholder="Imię"
              className={registerFieldErrors.firstName ? 'input-error' : ''}
            />
            {registerFieldErrors.firstName && (
              <span className="error-text">{registerFieldErrors.firstName}</span>
            )}
          </div>

          <div className="form-group">
            <input
              value={registerData.lastName}
              onChange={(e) => handleRegisterFieldChange('lastName', e.target.value)}
              onBlur={() => handleFieldBlur('lastName')}
              placeholder="Nazwisko"
              className={registerFieldErrors.lastName ? 'input-error' : ''}
            />
            {registerFieldErrors.lastName && (
              <span className="error-text">{registerFieldErrors.lastName}</span>
            )}
          </div>

          <div className="form-group">
            <input
              value={registerData.password}
              onChange={(e) => handleRegisterFieldChange('password', e.target.value)}
              onBlur={() => handleFieldBlur('password')}
              type="password"
              placeholder="Hasło"
              className={registerFieldErrors.password ? 'input-error' : ''}
            />
            {registerFieldErrors.password && (
              <span className="error-text">{registerFieldErrors.password}</span>
            )}
            <small className="password-hint">
              Wymagania: min. 8 znaków, 1 wielka litera, 1 mała litera, 1 cyfra, 1 znak specjalny (@$!%*?&)
            </small>
          </div>

          <button type="submit" disabled={loading || !isRegisterFormValid()}>
            {loading ? 'Przesyłanie...' : 'Zarejestruj się'}
          </button>
        </form>
      );
    }

    if (mode === 'login') {
      return (
        <form className="auth-form" onSubmit={handleLogin}>
          <h2>Logowanie</h2>
          <input
            value={loginData.userNameOrEmail}
            onChange={(e) => setLoginData({ ...loginData, userNameOrEmail: e.target.value })}
            placeholder="Nazwa użytkownika lub email"
          />
          <input
            value={loginData.password}
            onChange={(e) => setLoginData({ ...loginData, password: e.target.value })}
            type="password"
            placeholder="Hasło"
          />
          <button type="submit" disabled={loading}>
            {loading ? 'Przesyłanie...' : 'Zaloguj się'}
          </button>
        </form>
      );
    }

    if (mode === 'loggedIn') {
      return (
        <div className="auth-success">
          <h2>Zalogowano</h2>
          {token && <pre className="token-output">{token}</pre>}
          <button onClick={() => setMode('start')}>Wyloguj / wróć</button>
        </div>
      );
    }

    return null;
  };

  return (
    <div className="App">
      <header className="App-header">
        <h1>🚀 Trading Platform</h1>
        <p>Witaj w aplikacji do handlu giełdowego!</p>

        {mode === 'start' ? (
          <button className="action-button" onClick={() => openForm('register')}>
            Rozpocznij handel
          </button>
        ) : (
          <div className="form-shell">
            <div className="toggle-bar">
              <button
                className={mode === 'register' ? 'active' : ''}
                onClick={() => openForm('register')}
                type="button"
              >
                Rejestracja
              </button>
              <button
                className={mode === 'login' ? 'active' : ''}
                onClick={() => openForm('login')}
                type="button"
              >
                Logowanie
              </button>
            </div>

            {renderForm()}

            {(message || error) && (
              <div className={`message ${error ? 'error' : 'success'}`}>{error || message}</div>
            )}
          </div>
        )}
      </header>
    </div>
  );
}

export default App;
