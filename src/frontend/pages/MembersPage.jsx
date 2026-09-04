import { useEffect, useMemo, useState } from "react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { membersApi } from "../services/api.js";

/**
 * Member directory with search, sorting, and pagination.
 */
export function MembersPage({ user }) {
  const [members, setMembers] = useState([]);
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState({ key: "name", direction: "asc" });
  const [pageSize, setPageSize] = useState(10);
  const [page, setPage] = useState(1);

  useEffect(() => {
    if (user.isAuthenticated) {
      membersApi.list().then(setMembers);
    }
  }, [user.isAuthenticated]);

  const filtered = useFilteredMembers(members, search, ["name", "occupation", "email", "phoneNumber"]);
  const sorted = useSortedMembers(filtered, sort);
  const { currentPage, pageCount, visibleItems } = usePagedItems(sorted, page, pageSize);

  useEffect(() => {
    setPage(1);
  }, [search, pageSize, sort.key, sort.direction]);

  if (!user.isAuthenticated) {
    return <LoginRequired title="Medlemmer" />;
  }

  return (
    <MenuLayout active="/react/members" isAuthenticated={user.isAuthenticated}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Medlemmer</p>
          <p className="menu-panel-lead menu-panel-lead-inline">Her kan du få overblik over foreningens medlemmer og deres kontaktoplysninger, når de har valgt synlighed.</p>
        </div>
      </div>
      <SearchToolbar search={search} setSearch={setSearch} pageSize={pageSize} setPageSize={setPageSize} />
      <div className="menu-table-wrap">
        <table className="menu-member-table">
          <thead>
            <tr>
              <SortableHeader label="Navn" sortKey="name" sort={sort} setSort={setSort} />
              <SortableHeader label="Årgang" sortKey="graduationYear" sort={sort} setSort={setSort} />
              <SortableHeader label="Beskæftigelse" sortKey="occupation" sort={sort} setSort={setSort} />
              <SortableHeader label="Email" sortKey="email" sort={sort} setSort={setSort} />
              <SortableHeader label="Telefon" sortKey="phoneNumber" sort={sort} setSort={setSort} />
            </tr>
          </thead>
          <tbody>
            {visibleItems.map((member) => (
              <tr key={member.id}>
                <td>{member.name}</td>
                <td>{member.graduationYear || ""}</td>
                <td>{member.occupation || ""}</td>
                <td>{member.email || ""}</td>
                <td>{member.phoneNumber || ""}</td>
              </tr>
            ))}
            {visibleItems.length === 0 && (
              <tr>
                <td colSpan="5">Ingen medlemmer matcher søgningen.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      <Pagination page={currentPage} pageCount={pageCount} total={sorted.length} pageSize={pageSize} setPage={setPage} />
    </MenuLayout>
  );
}

/**
 * Shared prompt shown when a member-only page requires login.
 */
export function LoginRequired({ title }) {
  return (
    <main className="frontpage-main">
      <section className="page-shell frontpage-section">
        <h1>{title}</h1>
        <p className="muted">Du skal være logget ind for at se denne side.</p>
        <a className="frontpage-button frontpage-button-primary" href="/react/account/login">Login</a>
      </section>
    </main>
  );
}

/**
 * Search and page-size controls reused by member tables.
 */
export function SearchToolbar({ search, setSearch, pageSize, setPageSize }) {
  return (
    <div className="menu-table-toolbar">
      {pageSize && setPageSize && (
        <label className="menu-table-page-size">
          <span>Vis</span>
          <select value={pageSize} onChange={(event) => setPageSize(Number(event.target.value))}>
            {[10, 25, 50, 100].map((size) => <option value={size} key={size}>{size}</option>)}
          </select>
          <span>pr. side</span>
        </label>
      )}
      <label className="menu-table-search">
        <span>Søg</span>
        <input type="search" placeholder="Navn" value={search} onChange={(event) => setSearch(event.target.value)} />
      </label>
    </div>
  );
}

/**
 * Table header button that cycles sorting for one column.
 */
export function SortableHeader({ label, sortKey, sort, setSort }) {
  const active = sort.key === sortKey;
  const indicator = active ? (sort.direction === "asc" ? "↑" : "↓") : "↕";

  function toggleSort() {
    setSort((current) => ({
      key: sortKey,
      direction: current.key === sortKey && current.direction === "asc" ? "desc" : "asc"
    }));
  }

  return (
    <th>
      <button className="menu-table-sort-button" type="button" onClick={toggleSort}>
        {label}
        <span>{indicator}</span>
      </button>
    </th>
  );
}

/**
 * Compact pagination controls for table-based pages.
 */
export function Pagination({ page, pageCount, total, pageSize, setPage }) {
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  return (
    <div className="menu-table-pagination">
      <span>Viser {from}-{to} af {total}</span>
      <div className="menu-table-pagination-actions">
        <button className="frontpage-button" type="button" disabled={page <= 1} onClick={() => setPage((current) => Math.max(1, current - 1))}>Forrige</button>
        <span>Side {page} af {pageCount}</span>
        <button className="frontpage-button" type="button" disabled={page >= pageCount} onClick={() => setPage((current) => Math.min(pageCount, current + 1))}>Næste</button>
      </div>
    </div>
  );
}

export function useFilteredMembers(members, search, keys) {
  return useMemo(() => {
    const term = search.toLowerCase();
    return members.filter((member) => keys.some((key) => String(member[key] ?? "").toLowerCase().includes(term)));
  }, [members, search, keys]);
}

export function useSortedMembers(members, sort) {
  return useMemo(() => {
    return [...members].sort((left, right) => {
      const result = compareValues(left[sort.key], right[sort.key]);
      return sort.direction === "asc" ? result : -result;
    });
  }, [members, sort]);
}

export function usePagedItems(items, page, pageSize) {
  const pageCount = Math.max(1, Math.ceil(items.length / pageSize));
  const currentPage = Math.min(page, pageCount);
  const visibleItems = items.slice((currentPage - 1) * pageSize, currentPage * pageSize);
  return { currentPage, pageCount, visibleItems };
}

function compareValues(left, right) {
  if (typeof left === "number" || typeof right === "number") {
    return Number(left || 0) - Number(right || 0);
  }

  return String(left ?? "").localeCompare(String(right ?? ""), "da-DK", { sensitivity: "base" });
}
