
import React, { useState } from 'react';
import { useInstruments } from '../../../hooks/admin/useInstruments';
import { Instrument } from '../../../types/admin';
import { DataTable, Column } from '../../common/DataTable';
import { InstrumentModal } from './InstrumentModal';
import './InstrumentsContent.css';

export const InstrumentsContent: React.FC = () => {
  const {
    instruments,
    totalCount,
    currentPage,
    totalPages,
    loading,
    error,
    page,
    pageSize,
    setPage,
    setPageSize,
    addInstrument,
    updateInstrument,
    deleteInstrument,
    submitForApproval
  } = useInstruments();

  const [modalOpen, setModalOpen] = useState(false);
  const [editingInstrument, setEditingInstrument] = useState<Instrument | null>(null);
  const [actionModal, setActionModal] = useState<'submit' | null>(null);
  const [selectedInstrument, setSelectedInstrument] = useState<Instrument | null>(null);
  const [reason, setReason] = useState('');

  const handleAdd = () => {
    setEditingInstrument(null);
    setModalOpen(true);
  };

  const handleEdit = (instrument: Instrument) => {
    setEditingInstrument(instrument);
    setModalOpen(true);
  };

  const handleSubmit = async (formData: Partial<Instrument>) => {
    try {
      if (editingInstrument) {
        await updateInstrument(editingInstrument.id, formData);
      } else {
        await addInstrument(formData);
      }
      setModalOpen(false);
      setEditingInstrument(null);
    } catch (err) {
      console.error('Error:', err);
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm('Czy napewno chcesz usunąć ten instrument?')) {
      try {
        await deleteInstrument(id);
      } catch (err) {
        console.error('Error:', err);
      }
    }
  };

  const handleSubmitApproval = (instrument: Instrument) => {
    setSelectedInstrument(instrument);
    setActionModal('submit');
    setReason('');
  };

  const handleConfirmSubmit = async () => {
    if (!selectedInstrument) return;
    try {
      await submitForApproval(selectedInstrument.id, reason);
      setActionModal(null);
      setSelectedInstrument(null);
      setReason('');
    } catch (err) {
      console.error('Error:', err);
    }
  };

  const columns: Column<Instrument>[] = [
    {
      key: 'name',
      label: 'Nazwa',
      width: '120px'
    },
    {
      key: 'symbol',
      label: 'Symbol',
      width: '100px',
      render: (value) => <span style={{ fontWeight: 600, color: '#00d4ff' }}>{value}</span>
    },
    {
      key: 'type',
      label: 'Typ',
      width: '100px',
      render: (value) => <span className="type-badge">{value}</span>
    },
    {
      key: 'status',
      label: 'Status',
      width: '110px',
      render: (value) => {
        const statusClass = `status-badge status-${value}`;
        const statusLabel = {
          draft: 'Szkic',
          pending: 'Oczekujący',
          approved: 'Zatwierdzony',
          rejected: 'Odrzucony'
        }[value as string] || value;
        return <span className={statusClass}>{statusLabel}</span>;
      }
    },
    {
      key: 'description',
      label: 'Opis',
      width: '150px',
      render: (value) => <span>{String(value).substring(0, 50)}...</span>
    },
    {
      key: 'createdAt',
      label: 'Data',
      width: '130px',
      render: (value) => new Date(value).toLocaleDateString('pl-PL')
    }
  ];

  const getRowActions = (instrument: Instrument) => (
    <div className="action-buttons">
      <button className="btn-edit" onClick={() => handleEdit(instrument)}>
        ✏️ Edytuj
      </button>
      {(instrument.status === 'draft' || instrument.status === 'rejected') && (
        <button className="btn-submit" onClick={() => handleSubmitApproval(instrument)}>
          📤 Wyślij
        </button>
      )}
      <button className="btn-delete" onClick={() => handleDelete(instrument.id)}>
        🗑️ Usuń
      </button>
    </div>
  );

  return (
    <div className="instruments-content">
      <div className="content-header">
        <h2>🛠️ Instrumenty</h2>
        <button className="btn-add-new" onClick={handleAdd}>
          + Dodaj Nowy Instrument
        </button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      <DataTable
        columns={columns}
        data={instruments}
        totalCount={totalCount}
        currentPage={currentPage}
        totalPages={totalPages}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        loading={loading}
        actions={getRowActions}
      />

      {modalOpen && (
        <InstrumentModal
          instrument={editingInstrument}
          onSubmit={handleSubmit}
          onCancel={() => {
            setModalOpen(false);
            setEditingInstrument(null);
          }}
        />
      )}

      {actionModal === 'submit' && selectedInstrument && (
        <div className="modal-overlay" onClick={() => setActionModal(null)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>📤 Wyślij do Zatwierdzenia</h2>
            </div>
            <div className="modal-body">
              <p className="modal-description">
                Wysyłasz instrument <strong>{selectedInstrument.name}</strong> do zatwierdzenia
              </p>
              <div className="form-group">
                <label>Powód/Notatka (opcjonalnie):</label>
                <textarea
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder="Dodaj notatkę..."
                  rows={4}
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn-cancel" onClick={() => setActionModal(null)}>
                Anuluj
              </button>
              <button className="btn-confirm" onClick={handleConfirmSubmit}>
                📤 Wyślij
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
