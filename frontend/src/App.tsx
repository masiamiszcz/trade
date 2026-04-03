import React from 'react';
import './App.css';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <h1>🚀 Trading Platform</h1>
        <p>
          Witaj w aplikacji do handlu giełdowego!
        </p>
        <div className="status">
          <div className="status-item">
            <span className="status-dot healthy"></span>
            Frontend: Aktywny
          </div>
          <div className="status-item">
            <span className="status-dot">?</span>
            Backend: Sprawdzam...
          </div>
        </div>
        <button className="action-button">
          Rozpocznij handel
        </button>
      </header>
    </div>
  );
}

export default App;
