import { Fragment, useEffect, useMemo, useState } from "react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import {
  Pagination,
  SearchToolbar,
  SortableHeader,
  usePagedItems,
  useSortedMembers,
} from "./MembersPage.jsx";
import { Link, navigate } from "../routes/navigation.jsx";
import { membersApi } from "../services/api.js";
import { financeApi } from "../services/financeApi.js";
import "../styles/finance.css";

const months = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "Maj",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Okt",
  "Nov",
  "Dec",
];
const money = (v) => {
  const n = Number(v || 0);
  if (Math.abs(n) < 0.005) return "";
  return n < 0
    ? `(${new Intl.NumberFormat("da-DK").format(-n)} kr.)`
    : `${new Intl.NumberFormat("da-DK").format(n)} kr.`;
};
const cls = (v) =>
  Number(v || 0) < 0
    ? "finance-live-amount is-negative"
    : "finance-live-amount is-positive";
const filters = (s) => {
  const p = new URLSearchParams(s);
  return {
    year: Number(p.get("year")) || new Date().getFullYear(),
    accountId: p.get("accountId") || "",
    bankKey: p.get("bankKey") || "",
    mobilePayKey: p.get("mobilePayKey") || "",
    query: p.get("search") || "",
  };
};

function SearchableSelect({ label, options, value, onChange, placeholder }) {
  const [query, setQuery] = useState("");
  const selected = options.find((option) => option.id === value);
  const filteredOptions = options.filter((option) =>
    option.label.toLowerCase().includes(query.toLowerCase()),
  );
  return (
    <div className="finance-admin-detail-field finance-admin-detail-field-full">
      <span>{label}</span>
      <details className="admin-multi-select finance-admin-single-select">
        <summary>
          <strong>{selected?.label || placeholder}</strong>
        </summary>
        <div className="admin-multi-select-menu">
          <label className="admin-multi-select-search">
            <span>Søg</span>
            <input
              type="search"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={`Søg i ${label.toLowerCase()}`}
            />
          </label>
          <button
            type="button"
            className="finance-admin-select-option"
            onClick={(event) => {
              onChange("");
              event.currentTarget.closest("details")?.removeAttribute("open");
            }}
          >
            {placeholder}
          </button>
          {filteredOptions.map((option) => (
            <button
              type="button"
              className="finance-admin-select-option"
              key={option.id}
              onClick={(event) => {
                onChange(option.id);
                event.currentTarget.closest("details")?.removeAttribute("open");
              }}
            >
              {option.label}
            </button>
          ))}
        </div>
      </details>
    </div>
  );
}
const total = (rows, key) =>
  rows.reduce((sum, row) => sum + Number(row[key] || 0), 0);
const hasFinanceValue = (row) =>
  [row.realized, row.budget, row.previousYear].some(
    (value) => Math.abs(Number(value || 0)) >= 0.005,
  );
function accountGroups(accounts) {
  const groups = new Map();
  for (const account of (accounts || []).filter(hasFinanceValue)) {
    const mainKey = String(account.accountKey ?? account.mainAccount);
    if (!groups.has(mainKey))
      groups.set(mainKey, {
        key: mainKey,
        name: account.mainAccount || "Uden hovedkonto",
        subs: new Map(),
      });
    const main = groups.get(mainKey);
    const subKey = String(account.subAccountKey ?? account.subAccount);
    if (!main.subs.has(subKey))
      main.subs.set(subKey, {
        key: `${mainKey}-${subKey}`,
        name: account.subAccount || "Uden underkonto",
        rows: [],
      });
    const sub = main.subs.get(subKey);
    const existingContext = sub.rows.find(
      (row) =>
        row.contextKey === account.contextKey &&
        row.context === account.context,
    );
    if (existingContext) {
      existingContext.realized += Number(account.realized || 0);
      existingContext.previousYear += Number(account.previousYear || 0);
      existingContext.budget += Number(account.budget || 0);
    } else {
      sub.rows.push({ ...account });
    }
  }
  return [...groups.values()]
    .map((main) => ({ ...main, subs: [...main.subs.values()] }))
    .filter((main) => main.subs.length > 0);
}

function AccountTable({ overview, year }) {
  const [open, setOpen] = useState(new Set());
  const groups = useMemo(
    () => accountGroups(overview.accounts),
    [overview.accounts],
  );
  const toggle = (key) =>
    setOpen((old) => {
      const next = new Set(old);
      next.has(key) ? next.delete(key) : next.add(key);
      return next;
    });
  const realizedLabel = overview.isCurrentYear
    ? "Realiseret YTD"
    : `Realiseret (${overview.year})`;
  const previousLabel = overview.isCurrentYear
    ? "Sidste år YTD"
    : `Sidste år (${overview.previousYear})`;
  return (
    <section className="finance-live-panel finance-live-pnl-panel">
      <div className="finance-live-panel-heading">
        <div>
          <p className="finance-live-kicker">Resultatopgørelse</p>
        </div>
      </div>
      <div className="finance-live-table-scroll">
        <table className="finance-live-table">
          <thead>
            <tr>
              <th>Konto</th>
              <th>{realizedLabel}</th>
              <th>Budget ({overview.year})</th>
              <th>{previousLabel}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {groups.map((main) => (
              <Fragment key={main.key}>
                <tr className="finance-live-section-row">
                  <th colSpan="5">{main.name}</th>
                </tr>
                {main.subs.map((sub) => (
                  <Fragment key={sub.key}>
                    <tr className="finance-live-parent-row">
                      <th>
                        <button
                          type="button"
                          className="finance-live-toggle"
                          onClick={() => toggle(sub.key)}
                        >
                          {open.has(sub.key) ? "−" : "+"}
                        </button>
                        {sub.name}
                      </th>
                      <td className={cls(total(sub.rows, "realized"))}>
                        {money(total(sub.rows, "realized"))}
                      </td>
                      <td className={cls(total(sub.rows, "budget"))}>
                        {money(total(sub.rows, "budget"))}
                      </td>
                      <td className={cls(total(sub.rows, "previousYear"))}>
                        {money(total(sub.rows, "previousYear"))}
                      </td>
                      <td>
                        <Link
                          className="finance-admin-table-link"
                          href={`/react/admin/finance/posteringer?year=${year}&accountId=${encodeURIComponent(sub.rows[0].accountId)}`}
                        >
                          Se posteringer
                        </Link>
                      </td>
                    </tr>
                    {open.has(sub.key) &&
                      sub.rows.map((row) => (
                        <tr
                          className="finance-live-context-row"
                          key={row.accountId}
                        >
                          <td>{row.context}</td>
                          <td className={cls(row.realized)}>
                            {money(row.realized)}
                          </td>
                          <td className={cls(row.budget)}>
                            {money(row.budget)}
                          </td>
                          <td className={cls(row.previousYear)}>
                            {money(row.previousYear)}
                          </td>
                          <td>
                            <Link
                              className="finance-admin-table-link"
                              href={`/react/admin/finance/posteringer?year=${year}&accountId=${encodeURIComponent(row.accountId)}`}
                            >
                              Se posteringer
                            </Link>
                          </td>
                        </tr>
                      ))}
                  </Fragment>
                ))}
              </Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
function MonthlyChart({ monthly, current, year }) {
  const visible = months.slice(0, current ? new Date().getMonth() + 1 : 12);
  const values = visible.map((_, index) => Number(monthly?.[index]?.net ?? 0));
  const max = Math.max(...values.map((value) => Math.abs(value)), 1);
  return (
    <section className="finance-live-panel finance-live-chart-panel">
      <div className="finance-live-panel-heading">
        <div>
          <p className="finance-live-kicker">Månedlig udvikling</p>
        </div>
        <span className="finance-live-badge">{year}</span>
      </div>
      <div
        className="finance-live-chart"
        style={{
          gridTemplateColumns: `repeat(${values.length}, minmax(0, 1fr))`,
        }}
      >
        {values.map((value, index) => (
          <div className="finance-live-chart-column" key={visible[index]}>
            <div className="finance-live-chart-value">{money(value)}</div>
            <div className="finance-live-chart-track">
              <span
                className={value < 0 ? "is-negative" : ""}
                style={{
                  height: `${value === 0 ? 3 : Math.max(8, (Math.abs(value) / max) * 100)}%`,
                }}
              />
            </div>
            <span className="finance-live-chart-label">{visible[index]}</span>
          </div>
        ))}
      </div>
    </section>
  );
}
function SourceTable({ title, rows, year }) {
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState({ key: "date", direction: "desc" });
  const [pageSize, setPageSize] = useState(10);
  const [page, setPage] = useState(1);
  const filtered = useMemo(() => {
    const term = search.toLowerCase();
    return rows.filter((row) =>
      [row.date, row.text, row.id, row.amount].some((value) =>
        String(value ?? "")
          .toLowerCase()
          .includes(term),
      ),
    );
  }, [rows, search]);
  const sorted = useSortedMembers(filtered, sort);
  const { currentPage, pageCount, visibleItems } = usePagedItems(
    sorted,
    page,
    pageSize,
  );

  useEffect(
    () => setPage(1),
    [search, pageSize, sort.key, sort.direction, rows],
  );

  return (
    <section className="finance-live-panel finance-live-pnl-panel">
      <div className="finance-live-panel-heading">
        <div>
          <p className="finance-live-kicker">Seneste bevægelser</p>
        </div>
      </div>
      <SearchToolbar
        search={search}
        setSearch={setSearch}
        pageSize={pageSize}
        setPageSize={setPageSize}
        searchPlaceholder="Tekst eller reference"
      />
      <div className="finance-live-table-scroll">
        <table className="finance-live-table finance-admin-source-table">
          <thead>
            <tr>
              <SortableHeader
                label="Dato"
                sortKey="date"
                sort={sort}
                setSort={setSort}
              />
              <SortableHeader
                label="Tekst"
                sortKey="text"
                sort={sort}
                setSort={setSort}
              />
              <SortableHeader
                label="Reference"
                sortKey="id"
                sort={sort}
                setSort={setSort}
              />
              <SortableHeader
                label="Beløb"
                sortKey="amount"
                sort={sort}
                setSort={setSort}
              />
              <th />
            </tr>
          </thead>
          <tbody>
            {visibleItems.map((r) => (
              <tr key={r.id}>
                <td>{r.date}</td>
                <td>{r.text}</td>
                <td>{r.id}</td>
                <td className={cls(r.amount)}>{money(r.amount)}</td>
                <td>
                  <Link
                    className="finance-admin-table-link"
                    href={`/react/admin/finance/posteringer?year=${year}&${r.sourceType === "Bank" ? "bankKey" : "mobilePayKey"}=${r.sourceId}`}
                  >
                    Se posteringer
                  </Link>
                </td>
              </tr>
            ))}
            {visibleItems.length === 0 && (
              <tr>
                <td className="finance-admin-empty" colSpan="5">
                  Ingen posteringer fundet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      <Pagination
        page={currentPage}
        pageCount={pageCount}
        total={filtered.length}
        pageSize={pageSize}
        setPage={setPage}
      />
    </section>
  );
}
function GroupTable({ rows, year, isCurrentYear, previousYear }) {
  const [expanded, setExpanded] = useState(() => new Set());
  const groups = useMemo(() => {
    const map = new Map();
    for (const row of rows) {
      if (!map.has(row.name)) {
        map.set(row.name, { contexts: [], amount: 0, previousAmount: 0 });
      }
      const group = map.get(row.name);
      group.contexts.push(row);
      group.amount += Number(row.amount || 0);
      group.previousAmount += Number(row.previousAmount || 0);
    }
    return [...map.entries()];
  }, [rows]);
  const toggle = (name) =>
    setExpanded((current) => {
      const next = new Set(current);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });
  const previousLabel = isCurrentYear
    ? "Sidste år YTD"
    : `Sidste år (${previousYear})`;
  return (
    <section className="finance-live-panel finance-live-pnl-panel">
      <div className="finance-live-panel-heading">
        <div>
          <p className="finance-live-kicker">Posteringsgrupper</p>
        </div>
      </div>
      <div className="finance-live-table-scroll">
        <table className="finance-live-table">
          <thead>
            <tr>
              <th>Gruppe / kontekst</th>
              <th>{isCurrentYear ? "Beløb YTD" : `Beløb (${year})`}</th>
              <th>{previousLabel}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {groups.map(([name, group]) => (
              <Fragment key={name}>
                <tr className="finance-live-section-row">
                  <th>
                    <button
                      type="button"
                      className="finance-live-toggle"
                      onClick={() => toggle(name)}
                      aria-expanded={expanded.has(name)}
                    >
                      {expanded.has(name) ? "−" : "+"}
                    </button>
                    {name}
                  </th>
                  <td className={cls(group.amount)}>{money(group.amount)}</td>
                  <td className={cls(group.previousAmount)}>
                    {money(group.previousAmount)}
                  </td>
                  <td>
                    <Link
                      className="finance-admin-table-link"
                      href={`/react/admin/finance/posteringer?year=${year}&search=${encodeURIComponent(name)}`}
                    >
                      Se posteringer
                    </Link>
                  </td>
                </tr>
                {expanded.has(name) &&
                  group.contexts.map((row) => (
                    <tr
                      className="finance-live-parent-row"
                      key={row.id || row.context}
                    >
                      <th>{row.context || "Uden kontekst"}</th>
                      <td className={cls(row.amount)}>{money(row.amount)}</td>
                      <td className={cls(row.previousAmount)}>
                        {money(row.previousAmount)}
                      </td>
                      <td>
                        <Link
                          className="finance-admin-table-link"
                          href={`/react/admin/finance/posteringer?year=${year}&search=${encodeURIComponent(row.context)}`}
                        >
                          Se posteringer
                        </Link>
                      </td>
                    </tr>
                  ))}
              </Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export function FinanceAdminOverviewPage({ isAdmin, search }) {
  const now = new Date().getFullYear();
  const [year, setYear] = useState(filters(search).year),
    [data, setData] = useState(null),
    [tab, setTab] = useState("result"),
    [error, setError] = useState("");
  useEffect(() => {
    if (isAdmin) {
      setData(null);
      financeApi
        .adminOverview(year)
        .then(setData)
        .catch((e) => setError(e.message));
    }
  }, [isAdmin, year]);
  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Kun ADMIN har adgang til Finans admin.
        </p>
      </AdminLayout>
    );
  return (
    <AdminLayout active="" canWrite={true}>
      <div className="menu-panel-header finance-live-hero">
        <div>
          <p className="menu-section-title">Finans overview</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            Foreningens økonomi og datakvalitet
          </p>
        </div>
        <div className="finance-live-year-toggle">
          <button
            className={year === now ? "is-active" : ""}
            onClick={() => setYear(now)}
          >
            Dette år
          </button>
          <button
            className={year === now - 1 ? "is-active" : ""}
            onClick={() => setYear(now - 1)}
          >
            Sidste år
          </button>
        </div>
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {!data && !error && (
        <div className="finance-live-loading">Henter finansoversigt...</div>
      )}
      {data && (
        <>
          <div className="finance-live-metrics">
            <article className="finance-live-card">
              <h2>Indbetalinger</h2>
              <strong className={cls(data.summary.income)}>
                {money(data.summary.income)}
              </strong>
            </article>
            <article className="finance-live-card">
              <h2>Udgifter</h2>
              <strong className={cls(data.summary.expense)}>
                {money(data.summary.expense)}
              </strong>
            </article>
            <article className="finance-live-card">
              <h2>Datakvalitet</h2>
              <strong>
                {data.dataQuality.categorizedPostings} /{" "}
                {data.dataQuality.totalPostings}
              </strong>
            </article>
          </div>
          <MonthlyChart
            monthly={data.monthly}
            current={data.isCurrentYear}
            year={year}
          />
          <div className="finance-admin-tabs">
            {[
              ["result", "Resultatopgørelse"],
              ["groups", "Posteringsgrupper"],
              ["bank", "Bankoverførelse"],
              ["mobile", "MobilePay"],
            ].map(([id, label]) => (
              <button
                key={id}
                className={tab === id ? "is-active" : ""}
                onClick={() => setTab(id)}
              >
                {label}
              </button>
            ))}
          </div>
          {tab === "result" && <AccountTable overview={data} year={year} />}
          {tab === "groups" && (
            <GroupTable
              rows={data.postingGroups || []}
              year={year}
              isCurrentYear={data.isCurrentYear}
              previousYear={data.previousYear}
            />
          )}
          {tab === "bank" && (
            <SourceTable
              title="Bankoverførelser"
              rows={data.bankTransfers || []}
              year={year}
            />
          )}
          {tab === "mobile" && (
            <SourceTable
              title="MobilePay"
              rows={data.mobilePayTransfers || []}
              year={year}
            />
          )}
        </>
      )}
    </AdminLayout>
  );
}

export function FinanceAdminPostingsPage({ isAdmin, search }) {
  const initial = filters(search),
    now = new Date().getFullYear();
  const [year, setYear] = useState(initial.year),
    [accountId, setAccountId] = useState(initial.accountId),
    [bankKey, setBankKey] = useState(initial.bankKey),
    [mobilePayKey, setMobilePayKey] = useState(initial.mobilePayKey),
    [query, setQuery] = useState(initial.query),
    [status, setStatus] = useState(""),
    [account, setAccount] = useState(""),
    [group, setGroup] = useState(""),
    [user, setUser] = useState(""),
    [sort, setSort] = useState({ key: "postingDate", direction: "desc" }),
    [pageSize, setPageSize] = useState(25),
    [page, setPage] = useState(1),
    [data, setData] = useState(null),
    [members, setMembers] = useState([]),
    [error, setError] = useState("");
  useEffect(() => {
    if (isAdmin)
      financeApi
        .adminPostings(year, accountId, bankKey, mobilePayKey)
        .then(setData)
        .catch((e) => setError(e.message));
  }, [isAdmin, year, accountId, bankKey, mobilePayKey]);
  useEffect(() => {
    if (!isAdmin) return;
    membersApi
      .listAdmin()
      .then(setMembers)
      .catch(() => setMembers([]));
  }, [isAdmin]);
  const postings = data?.postings || [];
  const userNames = useMemo(
    () =>
      new Map(
        members.map((member) => [
          member.id ?? member.Id,
          member.name ??
            member.Name ??
            member.email ??
            member.Email ??
            member.id ??
            member.Id,
        ]),
      ),
    [members],
  );
  const userLabel = (userId) => userNames.get(userId) || userId;
  const values = (key) =>
    [...new Set(postings.map((p) => p[key]).filter(Boolean))].sort();
  const shown = postings.filter(
    (p) =>
      (!query ||
        [p.id, p.text, p.account, p.postingGroup]
          .join(" ")
          .toLowerCase()
          .includes(query.toLowerCase())) &&
      (!status || p.status === status) &&
      (!account || p.account === account) &&
      (!group || p.postingGroup === group) &&
      (!user || p.userId === user),
  );
  const sortedPostings = useSortedMembers(shown, sort);
  const { currentPage, pageCount, visibleItems } = usePagedItems(
    sortedPostings,
    page,
    pageSize,
  );
  useEffect(
    () => setPage(1),
    [
      query,
      status,
      account,
      group,
      user,
      year,
      pageSize,
      sort.key,
      sort.direction,
    ],
  );
  const reset = () => {
    setYear(now);
    setQuery("");
    setStatus("");
    setAccount("");
    setGroup("");
    setUser("");
    setAccountId("");
    setBankKey("");
    setMobilePayKey("");
  };
  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Kun ADMIN har adgang til Finans admin.
        </p>
      </AdminLayout>
    );
  return (
    <AdminLayout active="" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Posteringer</p>
        </div>
      </div>
      <div className="finance-admin-filters">
        <label>
          År
          <select
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
          >
            <option>{now}</option>
            <option>{now - 1}</option>
          </select>
        </label>
        <label>
          Søg
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="ID, tekst, konto..."
          />
        </label>
        <label>
          Status
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">Alle</option>
            <option>Ukategoriseret</option>
            <option>Mangler bilag</option>
            <option>Bogført</option>
          </select>
        </label>
        <label>
          Konto
          <select value={account} onChange={(e) => setAccount(e.target.value)}>
            <option value="">Alle konti</option>
            {values("account").map((x) => (
              <option key={x}>{x}</option>
            ))}
          </select>
        </label>
        <label>
          Posteringsgruppe
          <select value={group} onChange={(e) => setGroup(e.target.value)}>
            <option value="">Alle grupper</option>
            {values("postingGroup").map((x) => (
              <option key={x}>{x}</option>
            ))}
          </select>
        </label>
        <label>
          Bruger
          <select value={user} onChange={(e) => setUser(e.target.value)}>
            <option value="">Alle brugere</option>
            {values("userId").map((x) => (
              <option key={x} value={x}>
                {userLabel(x)}
              </option>
            ))}
          </select>
        </label>
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {!data && !error && (
        <div className="finance-live-loading">Henter posteringer...</div>
      )}
      {data && (
        <div className="finance-live-panel finance-admin-postings-table">
          <div className="finance-admin-table-controls">
            <label className="menu-table-page-size">
              <span>Vis</span>
              <select
                value={pageSize}
                onChange={(e) => setPageSize(Number(e.target.value))}
              >
                {[10, 25, 50, 100].map((size) => (
                  <option value={size} key={size}>
                    {size}
                  </option>
                ))}
              </select>
              <span>pr. side</span>
            </label>
            <button className="finance-admin-clear-filter" onClick={reset}>
              Nulstil
            </button>
          </div>
          <div className="finance-live-table-scroll">
            <table className="finance-live-table">
              <thead>
                <tr>
                  {[
                    ["ID", "id"],
                    ["Dato", "date"],
                    ["Posteringsdato", "postingDate"],
                    ["Tekst", "text"],
                    ["Beløb", "amount"],
                    ["Bruger", "userId"],
                    ["Konto", "account"],
                    ["Posteringsgruppe", "postingGroup"],
                    ["Bilag", "document"],
                    ["Kilde", "sourceType"],
                    ["Status", "status"],
                  ].map(([label, key]) => (
                    <SortableHeader
                      key={key}
                      label={label}
                      sortKey={key}
                      sort={sort}
                      setSort={setSort}
                    />
                  ))}
                </tr>
              </thead>
              <tbody>
                {visibleItems.map((p) => (
                  <tr
                    className="finance-admin-clickable-row"
                    key={p.id}
                    onClick={() =>
                      navigate(
                        `/react/admin/finance/posteringer/${encodeURIComponent(p.id)}`,
                      )
                    }
                    onKeyDown={(event) =>
                      event.key === "Enter" &&
                      navigate(
                        `/react/admin/finance/posteringer/${encodeURIComponent(p.id)}`,
                      )
                    }
                    role="link"
                    tabIndex="0"
                  >
                    <td>{p.id}</td>
                    <td>{p.date}</td>
                    <td>{p.postingDate}</td>
                    <td>{p.text}</td>
                    <td className={cls(p.amount)}>{money(p.amount)}</td>
                    <td>{p.userId ? userLabel(p.userId) : "—"}</td>
                    <td>{p.account || "—"}</td>
                    <td>{p.postingGroup || "—"}</td>
                    <td>{p.document ? "Ja" : "—"}</td>
                    <td>{p.sourceType}</td>
                    <td>{p.status}</td>
                  </tr>
                ))}
                {visibleItems.length === 0 && (
                  <tr>
                    <td colSpan="11" className="finance-admin-empty">
                      Ingen posteringer matcher filtrene.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination
            page={currentPage}
            pageCount={pageCount}
            total={shown.length}
            pageSize={pageSize}
            setPage={setPage}
          />
        </div>
      )}
    </AdminLayout>
  );
}

export function FinanceAdminPostingDetailPage({ isAdmin, id }) {
  const [posting, setPosting] = useState(null);
  const [options, setOptions] = useState(null);
  const [form, setForm] = useState(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin) return;
    Promise.all([
      financeApi.adminPosting(id),
      financeApi.adminPostingOptions(),
      membersApi.listAdmin(),
    ])
      .then(([detail, editorOptions, members]) => {
        const users = members.map((member) => ({
          id: member.id ?? member.Id,
          label:
            member.name ??
            member.Name ??
            member.email ??
            member.Email ??
            member.id ??
            member.Id,
        }));
        if (detail.userId && !users.some((user) => user.id === detail.userId)) {
          users.unshift({
            id: detail.userId,
            label: `Ukendt bruger (${detail.userId})`,
          });
        }
        setPosting(detail);
        setOptions({ ...editorOptions, users });
        setForm({
          accountId: detail.accountId || "",
          postingGroupId: detail.postingGroupId || "",
          userId: detail.userId || "",
          postingDate: detail.postingDate || "",
          document: detail.document || "",
        });
      })
      .catch((requestError) => setError(requestError.message));
  }, [id, isAdmin]);

  const update = (field, value) =>
    setForm((current) => ({ ...current, [field]: value }));
  async function save(event) {
    event.preventDefault();
    setMessage("");
    try {
      await financeApi.updateAdminPosting(id, form);
      setMessage("Ændringerne er gemt.");
      const refreshed = await financeApi.adminPosting(id);
      setPosting(refreshed);
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Kun ADMIN har adgang til Finans admin.
        </p>
      </AdminLayout>
    );
  return (
    <AdminLayout
      active=""
      canWrite={true}
      contentClassName="finance-admin-detail-content"
    >
      <div className="menu-panel-header finance-admin-detail-header">
        <div>
          <p className="menu-section-title">Postering</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            Redigér kategorisering og manuelle oplysninger
          </p>
        </div>
        <Link
          className="finance-live-profile-link"
          href="/react/admin/finance/posteringer"
        >
          Tilbage til posteringer
        </Link>
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {!posting && !error && (
        <div className="finance-live-loading">Henter postering...</div>
      )}
      {posting && form && options && (
        <div className="finance-admin-detail-grid">
          <form className="finance-admin-detail-form" onSubmit={save}>
            <div className="finance-admin-detail-form-heading">
              <h2>{posting.text || posting.id}</h2>
              <span
                className={`finance-admin-status is-${posting.status
                  .toLowerCase()
                  .replaceAll(" ", "-")}`}
              >
                {posting.status}
              </span>
            </div>
            <div className="finance-admin-detail-fields">
              <label>
                Postering ID
                <input value={posting.id} readOnly />
              </label>
              <label>
                Kilde
                <input value={posting.sourceType} readOnly />
              </label>
              <label>
                Dato
                <input value={posting.date} readOnly />
              </label>
              <label>
                Beløb
                <input value={money(posting.amount)} readOnly />
              </label>
              <label>
                Bankoverførsel
                <input
                  value={
                    posting.bankReference
                      ? `Bank · BA-${posting.bankReference}`
                      : ""
                  }
                  readOnly
                />
              </label>
              <label>
                MobilePay
                <input
                  value={
                    posting.mobilePayReference
                      ? `MobilePay · MP-${posting.mobilePayReference}`
                      : ""
                  }
                  readOnly
                />
              </label>
              <label className="finance-admin-detail-field-full">
                Posteringsdato
                <input
                  type="date"
                  value={form.postingDate}
                  onChange={(event) =>
                    update("postingDate", event.target.value)
                  }
                />
              </label>
              <SearchableSelect
                label="Konto"
                options={options.accounts}
                value={form.accountId}
                onChange={(value) => update("accountId", value)}
                placeholder="Vælg konto…"
              />
              <SearchableSelect
                label="Posteringsgruppe"
                options={options.postingGroups}
                value={form.postingGroupId}
                onChange={(value) => update("postingGroupId", value)}
                placeholder="Vælg posteringsgruppe…"
              />
              <SearchableSelect
                label="Bruger"
                options={options.users}
                value={form.userId}
                onChange={(value) => update("userId", value)}
                placeholder="Vælg bruger…"
              />
              {posting.sourceType !== "MobilePay" && (
                <label className="finance-admin-detail-field-full">
                  Dokumentations-URL
                  <input
                    type="text"
                    inputMode="url"
                    value={form.document}
                    onChange={(event) => update("document", event.target.value)}
                    placeholder="https://"
                  />
                </label>
              )}
            </div>
            <div className="finance-admin-detail-actions">
              <button className="profile-button" type="submit">
                Gem ændringer
              </button>
              {message && (
                <p className="status-message status-message-success">
                  {message}
                </p>
              )}
            </div>
          </form>
          <aside className="finance-admin-original-data">
            <p className="finance-live-kicker">Originaldata</p>
            <h2>Importdetaljer</h2>
            <dl>
              <div>
                <dt>Kildetype</dt>
                <dd>{posting.sourceType}</dd>
              </div>
              <div>
                <dt>Reference</dt>
                <dd>{posting.id}</dd>
              </div>
              <div>
                <dt>Oprindelig tekst</dt>
                <dd>{posting.text}</dd>
              </div>
            </dl>
            <p>
              Originale importdata er skrivebeskyttede. Kun kategorisering,
              bruger og dokumentation kan ændres.
            </p>
          </aside>
        </div>
      )}
    </AdminLayout>
  );
}
