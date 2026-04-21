import React from 'react';

interface InstrumentModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave?: (data: any) => void;
}

export const InstrumentModal: React.FC<InstrumentModalProps> = ({ isOpen, onClose, onSave }) => {
  if (!isOpen) return null;

  return (
    <div className="modal">
      <div className="modal-content">
        <h2>Instrument</h2>
        <button onClick={onClose}>Close</button>
      </div>
    </div>
  );
};
