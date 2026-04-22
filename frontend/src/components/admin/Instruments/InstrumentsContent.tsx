
import React, { useState, useEffect } from 'react';
import { useInstruments } from '../../../hooks/admin/useInstruments';
import { Instrument } from '../../../types/admin';
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
  }, [fetchInstruments]);

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

  const columns = [
    {
      key: 'name',
      label: 'Nazwa',
      width: '120px'
    },
    {
      key: 'symbol',
      label: 'Symbol',
      width: '100px',
      render: (value) => (
        <span style={{ fontWeight: 600, color: '#00d4ff' }}>
          {value || '-'}
        </span>
      )
    },
    {
      key: 'type',
      label: 'Typ',
      width: '100px',
      render: (value) => {
        const typeValue = value || '-';
        return <span className="type-badge">{typeValue}</span>;
      }
    },
    {
      key: 'status',
      label: 'Status',
      width: '110px',
      render: (value) => {
        if (!value) return <span>-</span>;
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
      render: (value) => {
        if (!value) return '-';
        return <span>{String(value).substring(0, 50)}...</span>;
      }
    },
    {
      key: 'createdAtUtc',
      label: 'Data',
      width: '130px',
      render: (value) => {
        try {
          if (!value) return '-';
          const date = new Date(value);
          if (isNaN(date.getTime())) return '-';
          return date.toLocaleDateString('pl-PL');
        } catch {
          return '-';
        }
      }
    }
  ];

  return (
    <div className="instruments-content">
      <div className="content-header">
        <h2>🛠️ Instrumenty</h2>
        <button className="btn-add-new" onClick={handleAdd}>
          + Dodaj Nowy Instrument
        </button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {loading ? (
        <div style={{ padding: '20px', textAlign: 'center', color: '#00d4ff' }}>
          ⏳ Ładowanie instrumentów...
        </div>
      ) : instruments.length === 0 ? (
        <div style={{ padding: '20px', textAlign: 'center', color: '#a4b5d6' }}>
          Brak instrumentów. Dodaj pierwszy aby zacząć! 🚀
        </div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid rgba(0, 212, 255, 0.2)' }}>
                {columns.map((col) => (
                  <th
                    key={String(col.key)}
                    style={{
                      width: col.width,
                      padding: '12px',
                      textAlign: 'left',
                      color: '#00d4ff',
                      fontWeight: 600,
                      fontSize: '13px'
                    }}
                  >
                    {col.label}
                  </th>
                ))}
                <th
                  style={{
                    padding: '12px',
                    textAlign: 'right',
                    color: '#00d4ff',
                    fontWeight: 600,
                    fontSize: '13px'
                  }}
                >
                  Akcje
                </th>
              </tr>
            </thead>
            <tbody>
              {instruments.map((instrument) => (
                <tr
                  key={instrument.id}
                  style={{
                    borderBottom: '1px solid rgba(0, 212, 255, 0.1)',
                    transition: 'background-color 0.2s'
                  }}
                  onMouseEnter={(e) =>
                    (e.currentTarget.style.backgroundColor = 'rgba(0, 212, 255, 0.05)')
                  }
                  onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                >
                  {columns.map((col) => (
                    <td
                      key={String(col.key)}
                      style={{
                        width: col.width,
                        padding: '12px',
                        color: '#a4b5d6',
                        fontSize: '13px'
                      }}
                    >
                      {col.render
                        ? col.render(instrument[col.key], instrument)
                        : String(instrument[col.key])}
                    </td>
                  ))}
                  <td
                    style={{
                      padding: '12px',
                      textAlign: 'right',
                      display: 'flex',
                      gap: '8px',
                      justifyContent: 'flex-end'
                    }}
                  >
                    <button
                      className="btn-edit"
                      onClick={() => handleEdit(instrument)}
                      style={{ marginRight: '4px' }}
                    >
                      ✏️ Edytuj
                    </button>
                    <button className="btn-delete" onClick={() => handleDelete(instrument.id)}>
                      🗑️ Usuń
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

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
