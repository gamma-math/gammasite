import { Fragment, useEffect, useMemo, useState } from "react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { financeApi } from "../services/financeApi.js";
import { LoginRequired } from "./MembersPage.jsx";
import "../styles/finance.css";

const monthNames = ["Jan", "Feb", "Mar", "Apr", "Maj", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dec"];

function formatAmount(value) {
  const amount = Number(value ?? 0);
  if (Math.abs(amount) < 0.005) return "—";
  const formatted = new Intl.NumberFormat("da-DK", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2
  }).format(Math.abs(amount));
  return amount < 0 ? `(${formatted} kr.)` : `${formatted} kr.`;
}

function amountClass(value) {
  return Number(value ?? 0) < 0 ? "finance-live-amount is-negative" : "finance-live-amount is-positive";
}

function sumRows(rows, property) {
  return rows.reduce((sum, row) => sum + Number(row[property] ?? 0), 0);
}

function groupAccounts(accounts) {
  const mainGroups = new Map();

  for (const account of accounts ?? []) {
    const mainKey = String(account.accountKey ?? account.mainAccount ?? "");
    let main = mainGroups.get(mainKey);
    if (!main) {
      main = { key: mainKey, labels: [account.mainAccount], subGroups: new Map() };
      mainGroups.set(mainKey, main);
    } else if (account.mainAccount) {
      main.labels.push(account.mainAccount);
    }

    const subKey = String(account.subAccountKey ?? account.subAccount ?? "");
    let sub = main.subGroups.get(subKey);
    if (!sub) {
      sub = { key: `${mainKey}-${subKey}`, labels: [account.subAccount], rows: [] };
      main.subGroups.set(subKey, sub);
    } else if (account.subAccount) {
      sub.labels.push(account.subAccount);
    }

    const existingContext = sub.rows.find((row) => row.contextKey === account.contextKey);
    if (existingContext) {
      existingContext.realized += Number(account.realized ?? 0);
      existingContext.previousYear += Number(account.previousYear ?? 0);
      if (existingContext.budget === 0) existingContext.budget = Number(account.budget ?? 0);
    } else {
      sub.rows.push({ ...account, realized: Number(account.realized ?? 0), previousYear: Number(account.previousYear ?? 0), budget: Number(account.budget ?? 0) });
    }
  }

  return [...mainGroups.values()].map((main) => ({
    ...main,
    name: readableLabel(main.labels) || "Uden hovedkonto",
    subGroups: [...main.subGroups.values()]
      .map((sub) => ({ ...sub, name: readableLabel(sub.labels) || "Uden underkonto" }))
  }));
}

function readableLabel(labels) {
  return labels.find((label) => label && !label.includes("?") && !label.includes("Ã") && !label.includes("�")) || labels.find(Boolean) || "";
}

function MetricCard({ label, value, negative = false }) {
  return (
    <article className="finance-live-card">
      <div className="finance-live-card-heading"><h2>{label}</h2><span className={`finance-live-dot ${negative ? "is-negative" : ""}`} /></div>
      <strong className={`${amountClass(value)} finance-live-metric`}>{formatAmount(value)}</strong>
    </article>
  );
}

function FinanceHeader({ currentYear, lastUpdated, setYear, year }) {
  return (
    <div className="menu-panel-header finance-live-hero">
      <div>
        <p className="menu-section-title">Finans</p>
        <p className="menu-panel-lead menu-panel-lead-inline">Et enkelt overblik over din økonomi i det valgte år.</p>
      </div>
      <div className="finance-live-hero-meta">
        <p className="finance-live-last-updated">Senest opdateret<br /><strong>{lastUpdated ?? "Henter..."}</strong></p>
        <div className="finance-live-year-toggle" aria-label="Vælg år">
          <button className={year === currentYear ? "is-active" : ""} onClick={() => setYear(currentYear)}>Dette år</button>
          <button className={year === currentYear - 1 ? "is-active" : ""} onClick={() => setYear(currentYear - 1)}>Sidste år</button>
        </div>
      </div>
    </div>
  );
}

function MonthlyChart({ monthly, isCurrentYear }) {
  const visibleMonthCount = isCurrentYear ? new Date().getMonth() + 1 : 12;
  const visibleMonths = monthNames.slice(0, visibleMonthCount);
  const values = visibleMonths.map((_, index) => Number(monthly?.[index]?.net ?? 0));
  const maxValue = Math.max(...values.map((value) => Math.abs(value)), 1);

  return (
    <section className="finance-live-panel finance-live-chart-panel">
      <div className="finance-live-panel-heading"><div><p className="finance-live-kicker">Månedlig udvikling</p><h2>Nettoresultat pr. måned</h2></div><span className="finance-live-badge">DKK</span></div>
      <div
        className="finance-live-chart"
        style={{ gridTemplateColumns: `repeat(${values.length}, minmax(0, 1fr))` }}
        aria-label="Månedligt nettoresultat"
      >
        {values.map((value, index) => (
          <div className="finance-live-chart-column" key={visibleMonths[index]}>
            <div className="finance-live-chart-value">{formatAmount(value)}</div>
            <div className="finance-live-chart-track"><span className={value < 0 ? "is-negative" : ""} style={{ height: `${value === 0 ? 3 : Math.max(8, Math.abs(value) / maxValue * 100)}%` }} /></div>
            <span className="finance-live-chart-label">{visibleMonths[index]}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

function Resultatopgørelse({ overview }) {
  const [expanded, setExpanded] = useState(() => new Set());
  const groups = useMemo(() => groupAccounts(overview.accounts), [overview.accounts]);
  const toggle = (key) => setExpanded((current) => {
    const next = new Set(current);
    if (next.has(key)) next.delete(key); else next.add(key);
    return next;
  });
  const realizedLabel = overview.isCurrentYear ? "Realiseret YTD" : `Realiseret (${overview.year})`;
  const budgetLabel = `Budget (${overview.year})`;
  const previousLabel = overview.isCurrentYear ? "Sidste år YTD" : `Sidste år (${overview.previousYear})`;

  return (
    <section className="finance-live-panel finance-live-pnl-panel">
      <div className="finance-live-panel-heading"><div><p className="finance-live-kicker">Resultatopgørelse</p><h2>Regnskab pr. konto</h2></div></div>
      <div className="finance-live-table-scroll">
        <table className="finance-live-table">
          <thead><tr><th>Konto</th><th>{realizedLabel}</th><th>{budgetLabel}</th><th>{previousLabel}</th></tr></thead>
          <tbody>
            {groups.map((main) => {
              const mainRows = main.subGroups.flatMap((sub) => sub.rows);
              return (
                <Fragment key={main.key}>
                  <tr className="finance-live-section-row"><th colSpan="4">{main.name}</th></tr>
                  {main.subGroups.map((sub) => {
                    const isExpanded = expanded.has(sub.key);
                    const realized = sumRows(sub.rows, "realized");
                    const budget = sumRows(sub.rows, "budget");
                    const previous = sumRows(sub.rows, "previousYear");
                    return (
                      <Fragment key={sub.key}>
                        <tr className="finance-live-parent-row">
                          <th><button type="button" className="finance-live-toggle" onClick={() => toggle(sub.key)} aria-expanded={isExpanded}>{isExpanded ? "−" : "+"}</button>{sub.name}</th>
                          <td className={amountClass(realized)}>{formatAmount(realized)}</td><td className={amountClass(budget)}>{formatAmount(budget)}</td><td className={amountClass(previous)}>{formatAmount(previous)}</td>
                        </tr>
                        {isExpanded && sub.rows.map((row) => (
                          <tr className="finance-live-context-row" key={`${sub.key}-${row.accountId}`}>
                            <td>{row.context}</td><td className={amountClass(row.realized)}>{formatAmount(row.realized)}</td><td className={amountClass(row.budget)}>{formatAmount(row.budget)}</td><td className={amountClass(row.previousYear)}>{formatAmount(row.previousYear)}</td>
                          </tr>
                        ))}
                      </Fragment>
                    );
                  })}
                  <tr className="finance-live-total-row"><th>Total {main.name}</th><td className={amountClass(sumRows(mainRows, "realized"))}>{formatAmount(sumRows(mainRows, "realized"))}</td><td className={amountClass(sumRows(mainRows, "budget"))}>{formatAmount(sumRows(mainRows, "budget"))}</td><td className={amountClass(sumRows(mainRows, "previousYear"))}>{formatAmount(sumRows(mainRows, "previousYear"))}</td></tr>
                </Fragment>
              );
            })}
            <tr className="finance-live-grand-total"><th>Total</th><td className={amountClass(sumRows(overview.accounts, "realized"))}>{formatAmount(sumRows(overview.accounts, "realized"))}</td><td className={amountClass(sumRows(overview.accounts, "budget"))}>{formatAmount(sumRows(overview.accounts, "budget"))}</td><td className={amountClass(sumRows(overview.accounts, "previousYear"))}>{formatAmount(sumRows(overview.accounts, "previousYear"))}</td></tr>
          </tbody>
        </table>
      </div>
    </section>
  );
}

export function FinancePage({ user }) {
  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [overview, setOverview] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!user.isAuthenticated) return;
    setOverview(null);
    setError("");
    financeApi.overview(year).then(setOverview).catch((requestError) => setError(requestError.message));
  }, [user.isAuthenticated, year]);

  if (!user.isAuthenticated) return <LoginRequired title="Finans" />;

  return (
    <MenuLayout isAuthenticated={user.isAuthenticated}>
      <FinanceHeader currentYear={currentYear} lastUpdated={overview?.lastUpdated} setYear={setYear} year={year} />

      {error && <p className="finance-live-error">{error}</p>}
      {!overview && !error && <div className="finance-live-loading">Henter finansoversigt...</div>}
      {overview && <>
        <div className="finance-live-metrics"><MetricCard label="Indbetalinger" value={overview.summary.income} /><MetricCard label="Udgifter" value={overview.summary.expense} negative /><MetricCard label="Nettoresultat" value={overview.summary.net} /></div>
        <MonthlyChart monthly={overview.monthly} isCurrentYear={overview.isCurrentYear} />
        <Resultatopgørelse overview={overview} />
      </>}
    </MenuLayout>
  );
}
