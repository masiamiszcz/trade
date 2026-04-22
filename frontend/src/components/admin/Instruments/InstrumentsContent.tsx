
import React, { useState, useEffect } from 'react';
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
    pageSize,
    fetchInstruments,
    createInstrument,
    updateInstrument,
    deleteInstrument
  } = useInstruments();

  useEffect(() => {
    fetchInstruments();
  }, []);

  const [modalOpen, setModalOpen] = useState(false);
  const [editingInstrument, setEditingInstrument] = useState<Instrument | null>(null);

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
        await createInstrument(formData);
      }
      setModalOpen(false);
      setEditingInstrument(null);
      await fetchInstruments();
    } catch (err) {
      console.error('Error:', err);
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm('Czy napewno chcesz usunąć ten instrument?')) {
      try {
        await deleteInstrument(id);
        await fetchInstruments();
      } catch (err) {
        console.error('Error:', err);
      }
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
          Draft: 'Szkic',
          PendingApproval: 'Oczekujący',
          Approved: 'Zatwierdzony',
          Rejected: 'Odrzucony',
          Blocked: 'Zablokowany',
          Archived: 'Archiwizowany'
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
      key: 'createdAtUtc',
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
        onPageChange={(page) => fetchInstruments()}
        onPageSizeChange={(size) => fetchInstruments()}
        loading={loading}
        actions={getRowActions}
      />

      {modalOpen && (
        <InstrumentModal
          isOpen={modalOpen}
          instrument={editingInstrument}
          onSubmit={handleSubmit}
          onClose={() => {
            setModalOpen(false);
            setEditingInstrument(null);
          }}
        />
      )}
    </div>
  );
};
