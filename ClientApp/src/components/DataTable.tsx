import { Fragment, useEffect, useState } from 'react';
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  flexRender,
  type ColumnDef,
  type SortingState,
  type Row,
} from '@tanstack/react-table';

interface DataTableProps<T extends object> {
  data: T[];
  columns: ColumnDef<T, any>[];
  initialPageSize?: number;
  searchPlaceholder?: string;
  renderExpandedRow?: (row: Row<T>) => React.ReactNode;
}

export function DataTable<T extends object>({
  data,
  columns,
  initialPageSize = 25,
  searchPlaceholder = 'Søg...',
  renderExpandedRow,
}: DataTableProps<T>) {
  const [inputValue, setInputValue] = useState('');
  const [globalFilter, setGlobalFilter] = useState('');
  const [sorting, setSorting] = useState<SortingState>([]);

  // Debounce: only update the real filter 200 ms after the user stops typing
  useEffect(() => {
    const id = setTimeout(() => setGlobalFilter(inputValue), 200);
    return () => clearTimeout(id);
  }, [inputValue]);

  const table = useReactTable({
    data,
    columns,
    state: { globalFilter, sorting },
    onGlobalFilterChange: setGlobalFilter,
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    initialState: { pagination: { pageSize: initialPageSize } },
  });

  // Reset to page 0 whenever the filter changes
  useEffect(() => {
    table.setPageIndex(0);
  }, [globalFilter]);

  const { pageIndex, pageSize } = table.getState().pagination;
  const pageCount = table.getPageCount();
  const totalCount = data.length;
  const filteredCount = table.getFilteredRowModel().rows.length;
  const isFiltered = globalFilter.length > 0;

  // "Viser X–Y af Z poster"
  const rangeFrom = filteredCount === 0 ? 0 : pageIndex * pageSize + 1;
  const rangeTo = Math.min((pageIndex + 1) * pageSize, filteredCount);
  const rangeLabel = filteredCount <= pageSize
    ? isFiltered
      ? `${filteredCount} poster (ud af ${totalCount})`
      : `${filteredCount} poster`
    : isFiltered
      ? `Viser ${rangeFrom}–${rangeTo} af ${filteredCount} poster (ud af ${totalCount})`
      : `Viser ${rangeFrom}–${rangeTo} af ${filteredCount} poster`;

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2">
        <input
          className="form-control"
          style={{ maxWidth: '300px' }}
          placeholder={searchPlaceholder}
          value={inputValue}
          onChange={e => setInputValue(e.target.value)}
        />
        <span className="text-muted small">{rangeLabel}</span>
      </div>

      <div className="table-responsive">
        <table className="table table-striped table-bordered mb-2">
          <thead className="table-dark">
            {table.getHeaderGroups().map(hg => (
              <tr key={hg.id}>
                {hg.headers.map(header => {
                  const sorted = header.column.getIsSorted();
                  return (
                    <th
                      key={header.id}
                      onClick={header.column.getToggleSortingHandler()}
                      role={header.column.getCanSort() ? 'button' : undefined}
                      aria-sort={sorted === 'asc' ? 'ascending' : sorted === 'desc' ? 'descending' : undefined}
                      style={{ userSelect: 'none', whiteSpace: 'nowrap' }}
                    >
                      {flexRender(header.column.columnDef.header, header.getContext())}
                      {' '}
                      {sorted === 'asc' ? '▲' : sorted === 'desc' ? '▼' : header.column.getCanSort() ? '⇅' : ''}
                    </th>
                  );
                })}
              </tr>
            ))}
          </thead>
          <tbody>
            {table.getRowModel().rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="text-center text-muted py-3">
                  Ingen poster
                </td>
              </tr>
            ) : (
              table.getRowModel().rows.map(row => (
                <Fragment key={row.id}>
                  <tr>
                    {row.getVisibleCells().map(cell => (
                      <td key={cell.id}>
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                  {renderExpandedRow?.(row)}
                </Fragment>
              ))
            )}
          </tbody>
        </table>
      </div>

      {pageCount > 1 && (
        <div className="d-flex align-items-center gap-3 flex-wrap mb-3">
          <div className="btn-group btn-group-sm">
            <button
              className="btn btn-outline-secondary"
              onClick={() => table.setPageIndex(0)}
              disabled={!table.getCanPreviousPage()}
              aria-label="Første side"
            >«</button>
            <button
              className="btn btn-outline-secondary"
              onClick={() => table.previousPage()}
              disabled={!table.getCanPreviousPage()}
              aria-label="Forrige side"
            >‹</button>
            <button
              className="btn btn-outline-secondary"
              onClick={() => table.nextPage()}
              disabled={!table.getCanNextPage()}
              aria-label="Næste side"
            >›</button>
            <button
              className="btn btn-outline-secondary"
              onClick={() => table.setPageIndex(pageCount - 1)}
              disabled={!table.getCanNextPage()}
              aria-label="Sidste side"
            >»</button>
          </div>
          <span className="text-muted small">
            Side {pageIndex + 1} af {pageCount}
          </span>
          <select
            className="form-select form-select-sm w-auto"
            value={pageSize}
            onChange={e => table.setPageSize(Number(e.target.value))}
            aria-label="Rækker per side"
          >
            {[10, 25, 50, 100].map(ps => (
              <option key={ps} value={ps}>{ps} per side</option>
            ))}
          </select>
        </div>
      )}
    </>
  );
}
