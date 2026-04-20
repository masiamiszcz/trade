
import React from 'react';

interface BackupCodesModalProps {
  backupCodes: string[];
  onConfirm: () => void;
  isOpen: boolean;
}

export const BackupCodesModal: React.FC<BackupCodesModalProps> = ({
  backupCodes,
  onConfirm,
  isOpen,
}) => {
  if (!isOpen) return null;

  const downloadBackupCodes = () => {
    const content = `Kody zapasowe do 2FA:\n\n${backupCodes.join('\n')}\n\nZachowaj je w bezpiecznym miejscu!`;
    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(content));
    element.setAttribute('download', 'backup-codes.txt');
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content backup-codes-modal">
        <div className="modal-header">
          <h2>🔐 Kody zapasowe</h2>
          <p className="modal-subtitle">Zapisz je w bezpiecznym miejscu!</p>
        </div>

        <div className="modal-body">
          <div className="warning-box">
            <p>
              ⚠️ <strong>WAŻNE:</strong> Te kody mogą być użyte zamiast kodu 2FA. Każdy kod można użyć tylko raz!
            </p>
          </div>

          <div className="backup-codes-list">
            {backupCodes.map((code, index) => (
              <div key={index} className="backup-code-item">
                <span className="code-number">{index + 1}.</span>
                <code className="code-value">{code}</code>
                <button
                  type="button"
                  className="btn-small"
                  onClick={() => {
                    navigator.clipboard.writeText(code);
                    alert('Kod skopiowany!');
                  }}
                >
                  Kopiuj
                </button>
              </div>
            ))}
          </div>

          <div className="backup-codes-actions">
            <button
              type="button"
              className="btn-secondary"
              onClick={downloadBackupCodes}
            >
              📥 Pobierz jako plik
            </button>
            <button
              type="button"
              className="btn-secondary"
              onClick={() => {
                const allCodes = backupCodes.join('\n');
                navigator.clipboard.writeText(allCodes);
                alert('Wszystkie kody skopiowane!');
              }}
            >
              📋 Skopiuj wszystkie
            </button>
          </div>
        </div>

        <div className="modal-footer">
          <button
            type="button"
            className="btn-primary"
            onClick={onConfirm}
          >
            ✅ Zrozumiałem - Przejdź dalej
          </button>
        </div>
      </div>
    </div>
  );
};
