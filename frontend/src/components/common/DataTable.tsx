import React from 'react';
import './DataTable.css';

export interface Column<T> {
  key: keyof T;
  label: string;
  width?: string;
  render?: (value: any, item: T) => React.ReactNode;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  data: T[];
  keyExtractor: (item: T) => string | number;
  loading?: boolean;
  emptyMessage?: string;
  onRowClick?: (item: T) => void;
}

export const DataTable = React.forwardRef<HTMLDivElement, DataTableProps<any>>(
  (
    { columns, data, keyExtractor, loading = false, emptyMessage = 'Brak danych', onRowClick },
    ref
  ) => {
    if (loading) {
      return <div className="datatable-loading">Ładowanie...</div>;
    }

    if (!data || data.length === 0) {
      return <div className="datatable-empty">{emptyMessage}</div>;
    }

    return (
      <div ref={ref} className="datatable">
        <table>
          <thead>
            <tr>
              {columns.map((col) => (
                <th key={String(col.key)} style={{ width: col.width }}>
                  {col.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map((item) => (
              <tr
                key={keyExtractor(item)}
                onClick={onRowClick ? () => onRowClick(item) : undefined}
                className={onRowClick ? 'clickable-row' : undefined}
              >
                {columns.map((col) => (
                  <td key={String(col.key)} style={{ width: col.width }}>
                    {col.render ? col.render(item[col.key], item) : String(item[col.key])}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }
);

DataTable.displayName = 'DataTable';
