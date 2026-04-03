import React, { useState } from 'react';
import './App.css';

type Mode = 'start' | 'register' | 'login' | 'loggedIn';

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

  const [loginData, setLoginData] = useState({
    userNameOrEmail: '',
    password: ''
  });

  const apiBase = 'http://localhost:5001/api/auth';

  const openForm = (target: Mode) => {
    setError('');
    setMessage('');
    setMode(target);
  };

  const handleRegister = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    setMessage('');

    if (!registerData.userName || !registerData.email || !registerData.password) {
      setError('UserName, Email i Password są wymagane.');
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
        setError(payload.message || 'Błąd podczas rejestracji');
        return;
      }

      setMessage('Rejestracja zakończona sukcesem. Teraz możesz się zalogować.');
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
      setError('UserName/Email i Password są wymagane.');
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
          <input value={registerData.userName} onChange={(e) => setRegisterData({ ...registerData, userName: e.target.value })} placeholder="Nazwa użytkownika" />
          <input value={registerData.email} onChange={(e) => setRegisterData({ ...registerData, email: e.target.value })} type="email" placeholder="Email" />
          <input value={registerData.firstName} onChange={(e) => setRegisterData({ ...registerData, firstName: e.target.value })} placeholder="Imię" />
          <input value={registerData.lastName} onChange={(e) => setRegisterData({ ...registerData, lastName: e.target.value })} placeholder="Nazwisko" />
          <input value={registerData.password} onChange={(e) => setRegisterData({ ...registerData, password: e.target.value })} type="password" placeholder="Hasło" />
          <button type="submit" disabled={loading}>{loading ? 'Przesyłanie...' : 'Zarejestruj się'}</button>
        </form>
      );
    }

    if (mode === 'login') {
      return (
        <form className="auth-form" onSubmit={handleLogin}>
          <h2>Logowanie</h2>
          <input value={loginData.userNameOrEmail} onChange={(e) => setLoginData({ ...loginData, userNameOrEmail: e.target.value })} placeholder="Nazwa użytkownika lub email" />
          <input value={loginData.password} onChange={(e) => setLoginData({ ...loginData, password: e.target.value })} type="password" placeholder="Hasło" />
          <button type="submit" disabled={loading}>{loading ? 'Przesyłanie...' : 'Zaloguj się'}</button>
        </form>
      );
    }

    if (mode === 'loggedIn') {
      return (
        <div className="auth-success">
          <h2>Zalogowano</h2>
          {token && (
            <pre className="token-output">{token}</pre>
          )}
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
              <button className={mode === 'register' ? 'active' : ''} onClick={() => openForm('register')} type="button">Rejestracja</button>
              <button className={mode === 'login' ? 'active' : ''} onClick={() => openForm('login')} type="button">Logowanie</button>
            </div>

            {renderForm()}

            {(message || error) && (
              <div className={`message ${error ? 'error' : 'success'}`}>
                {error || message}
              </div>
            )}
          </div>
        )}
      </header>
    </div>
  );
}

export default App;
