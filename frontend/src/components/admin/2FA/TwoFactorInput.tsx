import React, { useState, useRef, useEffect } from 'react';

interface TwoFactorInputProps {
  onCodeSubmit: (code: string) => void;
  isLoading: boolean;
  error?: string;
  label?: string;
  placeholder?: string;
  maxLength?: number;
}

export const TwoFactorInput: React.FC<TwoFactorInputProps> = ({
  onCodeSubmit,
  isLoading,
  error,
  label = 'Kod z Authenticator\'a',
  placeholder = '000000',
  maxLength = 6,
}) => {
  const [code, setCode] = useState('');
  const [displayError, setDisplayError] = useState<string>('');
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setDisplayError(error || '');
  }, [error]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value.replace(/\D/g, '').slice(0, maxLength);
    setCode(value);
    setDisplayError('');

    // Auto-submit gdy wpiszemy 6 cyfr
    if (value.length === maxLength) {
      setTimeout(() => onCodeSubmit(value), 100);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && code.length === maxLength) {
      onCodeSubmit(code);
    }
  };

  const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
    const pastedText = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, maxLength);
    if (pastedText.length === maxLength) {
      setCode(pastedText);
      setTimeout(() => onCodeSubmit(pastedText), 100);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (code.length === maxLength) {
      onCodeSubmit(code);
    } else {
      setDisplayError('Wpisz kompletny kod');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="two-factor-form">
      <div className="form-group">
        <label htmlFor="2fa-code" className="form-label">
          {label}
        </label>
        <div className="code-input-wrapper">
          <input
            ref={inputRef}
            id="2fa-code"
            type="text"
            inputMode="numeric"
            value={code}
            onChange={handleChange}
            onKeyDown={handleKeyDown}
            onPaste={handlePaste}
            placeholder={placeholder}
            maxLength={maxLength}
            disabled={isLoading}
            className={`code-input ${displayError ? 'error' : ''} ${code.length === maxLength ? 'complete' : ''}`}
            autoComplete="off"
            autoFocus
          />
          <div className="code-indicator">
            <span className="code-counter">{code.length}/{maxLength}</span>
          </div>
        </div>

        {displayError && (
          <div className="form-error">
            <span>❌ {displayError}</span>
          </div>
        )}

        <button
          type="submit"
          disabled={code.length !== maxLength || isLoading}
          className="btn-primary btn-verify"
        >
          {isLoading ? 'Weryfikowanie...' : 'Weryfikuj'}
        </button>
      </div>

      <div className="help-text">
        <p>💡 Kod zostanie weryfikowany automatycznie po wpisaniu {maxLength} cyfr</p>
      </div>
    </form>
  );
};
