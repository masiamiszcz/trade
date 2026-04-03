import React from 'react';


interface ErrorMessageProps {
  message: string;
  onRetry?: () => void;
}

export const ErrorMessage: React.FC<ErrorMessageProps> = ({
  message,
  onRetry
}) => {
  return (
    <div className="error-message">
      <div className="error-content">
        <h3 className="error-title">Oops! Something went wrong</h3>
        <p className="error-text">{message}</p>
        {onRetry && (
          <button className="retry-button" onClick={onRetry}>
            Try Again
          </button>
        )}
      </div>
    </div>
  );
};
