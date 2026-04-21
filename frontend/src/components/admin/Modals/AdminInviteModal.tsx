import React, { useState } from 'react';
import { useAdminInvite } from '../../../hooks/admin/useAdminInvite';
import './AdminInviteModal.css';

interface AdminInviteModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: (invitationToken: string) => void;
}

export const AdminInviteModal: React.FC<AdminInviteModalProps> = ({
  isOpen,
  onClose,
  onSuccess
}) => {
  const { inviteAdmin, loading, error, successMessage } = useAdminInvite();
  const [formData, setFormData] = useState({
    email: '',
    firstName: '',
    lastName: ''
  });
  const [showSuccess, setShowSuccess] = useState(false);
  const [invitationToken, setInvitationToken] = useState('');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const token = await inviteAdmin(
      formData.email,
      formData.firstName,
      formData.lastName
    );

    if (token) {
      setInvitationToken(token);
      setShowSuccess(true);
      setFormData({ email: '', firstName: '', lastName: '' });
      onSuccess?.(token);
    }
  };

  const handleCloseSuccess = () => {
    setShowSuccess(false);
    setInvitationToken('');
    onClose();
  };

  const getInvitationUrl = () => {
    return `${window.location.origin}/admin/register?token=${invitationToken}`;
  };

  if (!isOpen) return null;

  if (showSuccess) {
    return (
      <div className="modal-overlay" onClick={handleCloseSuccess}>
        <div className="modal-container success-modal" onClick={(e) => e.stopPropagation()}>
          <button className="modal-close-btn" onClick={handleCloseSuccess}>✕</button>
          
          <div className="modal-content">
            <h2>✅ Link Zaproszenia Wygenerowany</h2>
            
            <div className="success-message">
              <p>Prześlij ten link do nowego administratora:</p>
              <div className="invitation-link-box">
                <input 
                  type="text"
                  value={getInvitationUrl()}
                  readOnly
                  className="invitation-url-input"
                />
                <button 
                  type="button"
                  className="copy-btn"
                  onClick={() => {
                    navigator.clipboard.writeText(getInvitationUrl());
                    alert('Link skopiowany do schowka! ✅');
                  }}
                >
                  📋 Kopiuj Link
                </button>
              </div>
              <p className="token-info">
                ⏱️ Link ważny przez <strong>48 godzin</strong>
              </p>
            </div>

            <button className="modal-action-btn" onClick={handleCloseSuccess}>
              Zamknij
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-container" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close-btn" onClick={onClose}>✕</button>
        
        <div className="modal-content">
          <h2>➕ Dodaj Nowego Admina</h2>
          
          {error && <div className="modal-error">{error}</div>}
          {successMessage && <div className="modal-success">{successMessage}</div>}

          <form onSubmit={handleSubmit} className="invite-form">
            <div className="form-group">
              <label htmlFor="email">Email:</label>
              <input
                id="email"
                type="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                required
                placeholder="admin@company.com"
                disabled={loading}
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="firstName">Imię:</label>
                <input
                  id="firstName"
                  type="text"
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleChange}
                  required
                  placeholder="John"
                  disabled={loading}
                />
              </div>

              <div className="form-group">
                <label htmlFor="lastName">Nazwisko:</label>
                <input
                  id="lastName"
                  type="text"
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleChange}
                  required
                  placeholder="Doe"
                  disabled={loading}
                />
              </div>
            </div>

            <div className="form-actions">
              <button 
                type="submit" 
                className="modal-submit-btn"
                disabled={loading}
              >
                {loading ? '⏳ Wysyłanie...' : '🔗 Generuj Link'}
              </button>
              <button 
                type="button" 
                className="modal-cancel-btn"
                onClick={onClose}
                disabled={loading}
              >
                Anuluj
              </button>
            </div>
          </form>
        </div>

        <button className="modal-close-btn-bottom" onClick={onClose}>
          Zamknij
        </button>
      </div>
    </div>
  );
};
