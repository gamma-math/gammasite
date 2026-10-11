import { Fragment, useEffect, useMemo, useRef, useState } from "react";
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
import { effectivePostingDate } from "../utils/financePostingDates.js";
import * as XLSX from "xlsx";
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

function usePostingYears(isAdmin) {
  const [years, setYears] = useState([]);

  useEffect(() => {
    if (!isAdmin) return;
    financeApi.adminPostingYears().then(setYears).catch(() => setYears([]));
  }, [isAdmin]);

  return years;
}

const excelEscape = (value) =>
  String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&apos;");

const excelFormula = (formula, value, style = "Number") => ({
  __excelFormula: true,
  formula,
  value,
  style,
});
const excelNumericValue = (value) =>
  value && typeof value === "object" && value.__excelFormula ? value.value : value;

function excelCell(value, numeric = false, style = "") {
  const isFormula = value && typeof value === "object" && value.__excelFormula;
  const rawValue = isFormula ? value.value : value;
  const resolvedStyle = isFormula ? value.style || style : style;
  if (rawValue === null || rawValue === undefined || rawValue === "") {
    return `<Cell${resolvedStyle ? ` ss:StyleID="${resolvedStyle}"` : ""} />`;
  }
  const type = numeric ? "Number" : "String";
  const data = numeric && Number.isFinite(Number(rawValue)) ? Number(rawValue) : rawValue;
  const formulaAttribute = isFormula ? ` ss:Formula="${excelEscape(value.formula)}"` : "";
  return `<Cell${resolvedStyle ? ` ss:StyleID="${resolvedStyle}"` : ""}${formulaAttribute}><Data ss:Type="${type}">${excelEscape(data)}</Data></Cell>`;
}

function excelSheet(name, headers, rows, numericColumns = new Set()) {
  const headerXml = `<Row>${headers.map((header) => excelCell(header, false, "Header")).join("")}</Row>`;
  const rowXml = rows
    .map(
      (row) =>
        `<Row>${row
          .map((value, index) =>
            excelCell(value, numericColumns.has(index), numericColumns.has(index) ? "Number" : ""),
          )
          .join("")}</Row>`,
    )
    .join("");
  return `<Worksheet ss:Name="${excelEscape(name)}"><Table>${headerXml}${rowXml}</Table></Worksheet>`;
}

function buildFinanceWorkbook({ year, postings, overview }) {
  const accountRows = (overview?.accounts || []).map((row) => [
    row.mainAccount || row.accountId,
    row.subAccount,
    row.context,
    row.realized,
    row.budget,
    row.previousYear,
  ]);
  if (accountRows.length) {
    const totalRow = accountRows.length + 2;
    accountRows.push([
      "",
      "",
      "TOTAL",
      excelFormula(`=SUM(R2C4:R${totalRow - 1}C4)`, accountRows.reduce((sum, row) => sum + Number(row[3] || 0), 0)),
      excelFormula(`=SUM(R2C5:R${totalRow - 1}C5)`, accountRows.reduce((sum, row) => sum + Number(row[4] || 0), 0)),
      excelFormula(`=SUM(R2C6:R${totalRow - 1}C6)`, accountRows.reduce((sum, row) => sum + Number(row[5] || 0), 0)),
    ]);
  }

  const groupRows = (overview?.postingGroups || []).map((row) => [
    row.name,
    row.context,
    row.amount,
    row.previousAmount,
  ]);
  if (groupRows.length) {
    const totalRow = groupRows.length + 2;
    groupRows.push([
      "",
      "TOTAL",
      excelFormula(`=SUM(R2C3:R${totalRow - 1}C3)`, groupRows.reduce((sum, row) => sum + Number(row[2] || 0), 0)),
      excelFormula(`=SUM(R2C4:R${totalRow - 1}C4)`, groupRows.reduce((sum, row) => sum + Number(row[3] || 0), 0)),
    ]);
  }

  const sourceRows = (rows) =>
    (rows || []).map((row) => [
      row.id,
      row.date,
      row.text,
      row.amount,
      row.postedAmount,
      excelFormula(
        "=RC[-2]-RC[-1]",
        Number(row.amount || 0) - Number(row.postedAmount || 0),
      ),
    ]);
  const sourceWithTotal = (rows) => {
    const values = sourceRows(rows);
    if (values.length) {
      const totalRow = values.length + 2;
      values.push([
        "",
        "",
        "TOTAL",
        excelFormula(`=SUM(R2C4:R${totalRow - 1}C4)`, values.reduce((sum, row) => sum + Number(row[3] || 0), 0)),
        excelFormula(`=SUM(R2C5:R${totalRow - 1}C5)`, values.reduce((sum, row) => sum + Number(row[4] || 0), 0)),
        excelFormula(
          `=SUM(R2C6:R${totalRow - 1}C6)`,
          values.reduce((sum, row) => sum + Number(excelNumericValue(row[5]) || 0), 0),
        ),
      ]);
    }
    return values;
  };

  const postingRows = (postings || []).map((row) => [
    row.id,
    row.date,
    row.postingDate,
    row.text,
    row.amount,
    row.userName ?? "",
    row.account,
    row.postingGroup,
    row.status,
  ]);
  const xml = `<?xml version="1.0"?><?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
<Styles><Style ss:ID="Header"><Font ss:Bold="1"/><Interior ss:Color="#EAF0FA" ss:Pattern="Solid"/></Style><Style ss:ID="Number"><NumberFormat ss:Format="#,##0.00"/></Style></Styles>
${excelSheet("Posteringer", ["ID", "Dato", "Posteringsdato", "Tekst", "Beløb", "Bruger", "Konto", "Posteringsgruppe", "Status"], postingRows, new Set([4]))}
${excelSheet("Resultatopgørelse", ["Konto", "Underkonto", "Kontekst", "Realiseret", "Budget", "Sidste år"], accountRows, new Set([3, 4, 5]))}
${excelSheet("Posteringsgrupper", ["Gruppe", "Kontekst", "Realiseret", "Sidste år"], groupRows, new Set([2, 3]))}
${excelSheet("Bankoverførsler", ["ID", "Dato", "Tekst", "Beløb fra bank", "Bogført beløb", "Difference"], sourceWithTotal(overview?.bankTransfers), new Set([3, 4, 5]))}
${excelSheet("MobilePay", ["ID", "Dato", "Tekst", "Beløb fra MobilePay", "Bogført beløb", "Difference"], sourceWithTotal(overview?.mobilePayTransfers), new Set([3, 4, 5]))}
</Workbook>`;
  return xml;
}

const xlsxFormula = (formula, value) => ({
  __xlsxFormula: true,
  formula: formula.replace(/^=/, ""),
  value: Number(value || 0),
});

const xlsxValue = (value) =>
  value && typeof value === "object"
    ? value.__xlsxFormula
      ? value.value
      : value.__xlsxHyperlink
        ? value.text
        : value
    : value;

const xlsxHyperlink = (url) => ({
  __xlsxHyperlink: true,
  text: url,
  url,
});

function buildUserNameMap(members = []) {
  return new Map(
    members.map((member) => [
      member.id ?? member.Id,
      member.name ?? member.Name ?? member.email ?? member.Email ?? "",
    ]),
  );
}

const NO_USER_FILTER = "__no_user__";

function buildPostingUserOptions(postings, userNames) {
  const userIds = [...new Set(postings.map((posting) => posting.userId).filter(Boolean))];
  userIds.sort((left, right) =>
    String(userNames.get(left) || left).localeCompare(
      String(userNames.get(right) || right),
      "da-DK",
      { sensitivity: "base" },
    ),
  );
  return [
    { id: "", label: "Alle brugere" },
    { id: NO_USER_FILTER, label: "Ingen bruger" },
    ...userIds.map((id) => ({ id, label: userNames.get(id) || id })),
  ];
}

function buildPostingValueOptions(postings, key, allLabel) {
  const values = [...new Set(postings.map((posting) => posting[key]).filter(Boolean))];
  values.sort((left, right) =>
    String(left).localeCompare(String(right), "da-DK", { sensitivity: "base" }),
  );
  return [{ id: "", label: allLabel }, ...values.map((value) => ({ id: value, label: value }))];
}

function appendXlsxSheet(workbook, name, headers, rows, numericColumns = new Set()) {
  const sheet = XLSX.utils.aoa_to_sheet([headers, ...rows]);
  const range = XLSX.utils.decode_range(sheet["!ref"] || "A1");
  rows.forEach((row, rowIndex) => {
    row.forEach((value, columnIndex) => {
      const address = XLSX.utils.encode_cell({ r: rowIndex + 1, c: columnIndex });
      if (value && typeof value === "object" && value.__xlsxFormula) {
        sheet[address] = { t: "n", v: value.value, f: value.formula, z: "#,##0.00" };
      } else if (value && typeof value === "object" && value.__xlsxHyperlink) {
        sheet[address] = { t: "s", v: value.text, l: { Target: value.url } };
      } else if (numericColumns.has(columnIndex) && value !== "" && value !== null && value !== undefined) {
        sheet[address] = { t: "n", v: Number(value), z: "#,##0.00" };
      }
    });
  });
  sheet["!autofilter"] = { ref: XLSX.utils.encode_range(range) };
  sheet["!cols"] = headers.map((header, index) => {
    const lengths = [header, ...rows.map((row) => row[index])]
      .map((value) => String(xlsxValue(value) ?? "").length);
    return { wch: Math.min(42, Math.max(12, ...lengths) + 2) };
  });
  XLSX.utils.book_append_sheet(workbook, sheet, name);
}

function buildFinanceWorkbookXlsx({ postings, overview }) {
  const workbook = XLSX.utils.book_new();
  const accountRows = (overview?.accounts || []).map((row) => [
    row.accountId,
    row.mainAccount,
    row.subAccount,
    row.context,
    row.realized,
    row.budget,
    row.previousYear,
  ]);
  if (accountRows.length) {
    const totalRow = accountRows.length + 2;
    accountRows.push([
      "", "", "", "TOTAL",
      xlsxFormula(`SUM(E2:E${totalRow - 1})`, accountRows.reduce((sum, row) => sum + Number(row[4] || 0), 0)),
      xlsxFormula(`SUM(F2:F${totalRow - 1})`, accountRows.reduce((sum, row) => sum + Number(row[5] || 0), 0)),
      xlsxFormula(`SUM(G2:G${totalRow - 1})`, accountRows.reduce((sum, row) => sum + Number(row[6] || 0), 0)),
    ]);
  }

  const groupRows = (overview?.postingGroups || []).map((row) => [
    row.id,
    row.name,
    row.context,
    row.amount,
    row.previousAmount,
  ]);
  if (groupRows.length) {
    const totalRow = groupRows.length + 2;
    groupRows.push([
      "", "", "TOTAL",
      xlsxFormula(`SUM(D2:D${totalRow - 1})`, groupRows.reduce((sum, row) => sum + Number(row[3] || 0), 0)),
      xlsxFormula(`SUM(E2:E${totalRow - 1})`, groupRows.reduce((sum, row) => sum + Number(row[4] || 0), 0)),
    ]);
  }

  const sourceRows = (rows, includeBalance = false) => (rows || []).map((row, index) => {
    const rowNumber = index + 2;
    const postedAmountColumn = includeBalance ? "F" : "E";
    return [
      row.id,
      row.date,
      row.text,
      row.amount,
      ...(includeBalance ? [row.balance] : []),
      row.postedAmount,
      xlsxFormula(`D${rowNumber}-${postedAmountColumn}${rowNumber}`, Number(row.amount || 0) - Number(row.postedAmount || 0)),
    ];
  });
  const sourceWithTotal = (rows, includeBalance = false) => {
    const values = sourceRows(rows, includeBalance);
    if (values.length) {
      const totalRow = values.length + 2;
      const postedAmountIndex = includeBalance ? 5 : 4;
      const differenceIndex = includeBalance ? 6 : 5;
      const postedAmountColumn = includeBalance ? "F" : "E";
      const differenceColumn = includeBalance ? "G" : "F";
      const total = [
        "", "", "TOTAL",
        xlsxFormula(`SUM(D2:D${totalRow - 1})`, values.reduce((sum, row) => sum + Number(row[3] || 0), 0)),
      ];
      if (includeBalance) total.push("");
      total.push(
        xlsxFormula(`SUM(${postedAmountColumn}2:${postedAmountColumn}${totalRow - 1})`, values.reduce((sum, row) => sum + Number(row[postedAmountIndex] || 0), 0)),
        xlsxFormula(`SUM(${differenceColumn}2:${differenceColumn}${totalRow - 1})`, values.reduce((sum, row) => sum + Number(xlsxValue(row[differenceIndex]) || 0), 0)),
      );
      values.push(total);
    }
    return values;
  };

  const mobilePayRows = (rows) => (rows || []).map((row, index) => {
    const rowNumber = index + 2;
    return [
      row.id,
      row.date,
      row.text,
      row.transferRef,
      row.transferDate,
      row.paymentTxId,
      row.paynerName,
      row.amount,
      row.postedAmount,
      xlsxFormula(`H${rowNumber}-I${rowNumber}`, Number(row.amount || 0) - Number(row.postedAmount || 0)),
    ];
  });
  const mobilePayWithTotal = (rows) => {
    const values = mobilePayRows(rows);
    if (values.length) {
      const totalRow = values.length + 2;
      values.push([
        "", "", "TOTAL", "", "", "", "",
        xlsxFormula(`SUM(H2:H${totalRow - 1})`, values.reduce((sum, row) => sum + Number(row[7] || 0), 0)),
        xlsxFormula(`SUM(I2:I${totalRow - 1})`, values.reduce((sum, row) => sum + Number(row[8] || 0), 0)),
        xlsxFormula(`SUM(J2:J${totalRow - 1})`, values.reduce((sum, row) => sum + Number(xlsxValue(row[9]) || 0), 0)),
      ]);
    }
    return values;
  };

  const postingRows = (postings || []).map((row) => [
    row.id,
    row.date,
    row.postingDate,
    row.text,
    row.amount,
    row.userName ?? "",
    row.accountId,
    row.account,
    row.postingGroupId,
    row.postingGroup,
    row.status,
    row.document ? xlsxHyperlink(row.document) : "",
  ]);

  appendXlsxSheet(workbook, "Posteringer", [
    "ID", "Dato", "Posteringsdato", "Tekst", "Beløb", "Bruger", "Konto ID", "Konto", "Posteringsgruppe ID", "Posteringsgruppe", "Status",
    "Bilag",
  ], postingRows, new Set([4]));
  appendXlsxSheet(workbook, "Resultatopgørelse", [
    "Konto ID", "Konto", "Underkonto", "Kontekst", "Realiseret", "Budget", "Sidste år",
  ], accountRows, new Set([4, 5, 6]));
  appendXlsxSheet(workbook, "Posteringsgrupper", [
    "Posteringsgruppe ID", "Posteringsgruppe", "Kontekst", "Realiseret", "Sidste år",
  ], groupRows, new Set([3, 4]));
  appendXlsxSheet(workbook, "Bankoverførsler", [
    "ID", "Dato", "Tekst", "Beløb fra bank", "Saldo", "Bogført beløb", "Difference",
  ], sourceWithTotal(overview?.bankTransfers, true), new Set([3, 4, 5, 6]));
  appendXlsxSheet(workbook, "MobilePay", [
    "ID", "Dato", "Tekst", "transfer_ref", "transfer_date", "payment_tx_id", "payner_name", "Beløb fra MobilePay", "Bogført beløb", "Difference",
  ], mobilePayWithTotal(overview?.mobilePayTransfers), new Set([7, 8, 9]));
  workbook.Workbook = {
    CalcPr: { calcMode: "auto", fullCalcOnLoad: true, forceFullCalc: true },
  };
  return XLSX.write(workbook, { bookType: "xlsx", type: "array", compression: true });
}

function TableExpansionButton({ expanded, onToggle }) {
  return (
    <button
      className={`finance-table-expand-button${expanded ? " is-close" : ""}`}
      type="button"
      onClick={onToggle}
      title={expanded ? "Luk udvidet tabel" : "Udvid tabel"}
      aria-label={expanded ? "Luk udvidet tabel" : "Udvid tabel"}
    >
      <span aria-hidden="true">{expanded ? "×" : "⛶"}</span>
      {expanded && <span>Luk tabel</span>}
    </button>
  );
}

function useExpandedTable(expanded, onClose) {
  useEffect(() => {
    if (!expanded) return undefined;
    const previousOverflow = document.body.style.overflow;
    const closeOnEscape = (event) => {
      if (event.key === "Escape") onClose();
    };
    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", closeOnEscape);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", closeOnEscape);
    };
  }, [expanded, onClose]);
}

function FilterSearchableSelect({ label, value, onChange, options, placeholder }) {
  const [query, setQuery] = useState("");
  const selected = options.find((option) => option.id === value);
  const filteredOptions = options.filter((option) =>
    option.label.toLowerCase().includes(query.toLowerCase()),
  );
  const closeOtherFilterSelects = (event) => {
    if (!event.currentTarget.open) {
      return;
    }
    const filters = event.currentTarget.closest(".finance-admin-filters");
    filters?.querySelectorAll("details.finance-admin-filter-select[open]").forEach((details) => {
      if (details !== event.currentTarget) {
        details.removeAttribute("open");
      }
    });
  };

  return (
    <label>
      {label}
      <div className="finance-admin-table-select">
        <details
          className="admin-multi-select finance-admin-filter-select"
          onToggle={closeOtherFilterSelects}
        >
          <summary>
            <strong>{selected?.label || placeholder}</strong>
          </summary>
          <div className="admin-multi-select-menu">
            <div className="admin-multi-select-search">
              <span>Søg</span>
              <input
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={`Søg i ${label.toLowerCase()}`}
              />
            </div>
            {filteredOptions.map((option) => (
              <button
                type="button"
                className="finance-admin-select-option"
                key={option.id || `all-${label}`}
                onClick={(event) => {
                  onChange(option.id);
                  setQuery("");
                  event.currentTarget.closest("details")?.removeAttribute("open");
                }}
              >
                {option.label}
              </button>
            ))}
          </div>
        </details>
      </div>
    </label>
  );
}

function SearchableSelect({ label, options = [], value, onChange, placeholder, compact = false, disabled = false }) {
  const [query, setQuery] = useState("");
  const selected = options.find((option) => option.id === value);
  const filteredOptions = options.filter((option) =>
    String(option.label ?? option.id ?? "").toLowerCase().includes(query.toLowerCase()),
  );
  const displayPlaceholder = compact && label === "Konto"
    ? "V\u00e6lg konto..."
    : compact && label === "Posteringsgruppe"
      ? "V\u00e6lg gruppe..."
      : placeholder;
  return (
    <div className={compact ? "finance-admin-table-select" : "finance-admin-detail-field finance-admin-detail-field-full"}>
      {!compact && <span>{label}</span>}
      <details
        className={`admin-multi-select finance-admin-single-select${compact ? " finance-admin-table-select-details" : ""}${disabled ? " is-disabled" : ""}`}
        aria-disabled={disabled}
        onClick={(event) => {
          if (disabled) event.preventDefault();
        }}
        onKeyDown={(event) => {
          if (disabled && (event.key === "Enter" || event.key === " ")) {
            event.preventDefault();
          }
        }}
        onToggle={(event) => {
          if (disabled) event.currentTarget.open = false;
        }}
      >
        <summary tabIndex={disabled ? -1 : undefined}>
          <strong>{selected?.label || displayPlaceholder}</strong>
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
            {displayPlaceholder}
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

function FinanceResizableHeader({
  label,
  sortKey,
  sort,
  setSort,
  width,
  onResizeStart,
}) {
  const active = sort.key === sortKey;
  const indicator = active
    ? sort.direction === "asc"
      ? "↑"
      : "↓"
    : "↕";
  return (
    <th style={{ width }}>
      <button
        className="menu-table-sort-button"
        type="button"
        onClick={() =>
          setSort((current) => ({
            key: sortKey,
            direction:
              current.key === sortKey && current.direction === "asc"
                ? "desc"
                : "asc",
          }))
        }
      >
        {label}
        <span>{indicator}</span>
      </button>
      <span
        className="finance-admin-column-resize-handle"
        onMouseDown={(event) => onResizeStart(sortKey, event)}
        role="separator"
        aria-label={`Juster bredden på ${label}`}
      />
    </th>
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
    ? "Realiseret (år til dato)"
    : `Realiseret (${overview.year})`;
  const previousLabel = overview.isCurrentYear
    ? "Sidste år (år til dato)"
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
                          href={`/react/admin/finance/postings?year=${year}&accountId=${encodeURIComponent(sub.rows.map((row) => row.accountId).filter(Boolean).join(","))}`}
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
                              href={`/react/admin/finance/postings?year=${year}&accountId=${encodeURIComponent(row.accountId)}`}
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
          <div
            className="finance-live-chart-column"
            key={visible[index]}
            data-tooltip={`${visible[index]}: ${money(value)}`}
          >
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
                    href={`/react/admin/finance/postings?year=${year}&${r.sourceType === "Bank" ? "bankKey" : "mobilePayKey"}=${r.sourceId}`}
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
    ? "Sidste år (år til dato)"
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
              <th>{isCurrentYear ? "Beløb (år til dato)" : `Beløb (${year})`}</th>
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
                      href={`/react/admin/finance/postings?year=${year}&search=${encodeURIComponent(name)}`}
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
                          href={`/react/admin/finance/postings?year=${year}&search=${encodeURIComponent(row.context)}`}
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
  const tabOptions = [
    ["result", "Resultatopgørelse"],
    ["groups", "Posteringsgrupper"],
    ["bank", "Bankoverførelse"],
    ["mobile", "MobilePay"],
  ];
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
          Du har ikke rettigheder til at se finansadministrationen.
        </p>
      </AdminLayout>
    );
  return (
    <AdminLayout active="" canWrite={true}>
      <div className="menu-panel-header finance-live-hero">
        <div>
          <p className="menu-section-title">Finans overblik</p>
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
              <strong className="finance-live-data-quality">
                {data.dataQuality.categorizedPostings} /{" "}
                {data.dataQuality.totalPostings} posteringer med kategori
              </strong>
            </article>
          </div>
          <MonthlyChart
            monthly={data.monthly}
            current={data.isCurrentYear}
            year={year}
          />
          <div className="finance-admin-tabs finance-admin-tabs-desktop">
            {tabOptions.map(([id, label]) => (
              <button
                key={id}
                className={tab === id ? "is-active" : ""}
                onClick={() => setTab(id)}
              >
                {label}
              </button>
            ))}
          </div>
          <label className="finance-admin-tabs-select">
            <span>Visning</span>
            <details className="finance-admin-tabs-dropdown">
              <summary>{tabOptions.find(([id]) => id === tab)?.[1]}</summary>
              <div className="finance-admin-tabs-dropdown-menu">
                {tabOptions.map(([id, label]) => (
                  <button
                    type="button"
                    className={tab === id ? "is-active" : ""}
                    key={id}
                    onClick={(event) => {
                      setTab(id);
                      event.currentTarget.closest("details").open = false;
                    }}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </details>
          </label>
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

export function FinanceAdminCashierPostingsPage({ isAdmin, search }) {
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
    [rows, setRows] = useState([]),
    [options, setOptions] = useState(null),
    [members, setMembers] = useState([]),
    [saving, setSaving] = useState(""),
    [error, setError] = useState("");
  const [isTableExpanded, setIsTableExpanded] = useState(false);
  useExpandedTable(isTableExpanded, () => setIsTableExpanded(false));
  const postingYears = usePostingYears(isAdmin);
  const selectableYears = postingYears.length ? postingYears : [now, now - 1];
  useEffect(() => {
    if (year !== null && postingYears.length && !postingYears.includes(year)) {
      setYear(postingYears[0]);
    }
  }, [postingYears, year]);
  const [columnWidths, setColumnWidths] = useState({
    id: 150,
    date: 150,
    postingDate: 170,
    text: 250,
    amount: 130,
    userId: 180,
    account: 190,
    postingGroup: 210,
    status: 155,
  });
  const resizeRef = useRef(null);
  const columns = [
    ["ID", "id"],
    ["Dato", "date"],
    ["Posteringsdato", "postingDate"],
    ["Tekst", "text"],
    ["Beløb", "amount"],
    ["Bruger", "userId"],
    ["Konto", "account"],
    ["Posteringsgruppe", "postingGroup"],
    ["Status", "status"],
  ];
  const startResize = (key, event) => {
    event.preventDefault();
    event.stopPropagation();
    resizeRef.current = {
      key,
      startX: event.clientX,
      startWidth: columnWidths[key],
    };
  };
  useEffect(() => {
    const handleMove = (event) => {
      if (!resizeRef.current) return;
      const { key, startX, startWidth } = resizeRef.current;
      setColumnWidths((current) => ({
        ...current,
        [key]: Math.max(90, startWidth + event.clientX - startX),
      }));
    };
    const handleUp = () => {
      resizeRef.current = null;
    };
    window.addEventListener("mousemove", handleMove);
    window.addEventListener("mouseup", handleUp);
    return () => {
      window.removeEventListener("mousemove", handleMove);
      window.removeEventListener("mouseup", handleUp);
    };
  }, []);
  const load = () =>
    Promise.all([
      financeApi.adminPostings(year, accountId, bankKey, mobilePayKey),
      financeApi.adminPostingOptions(),
    ]).then(([result, editorOptions]) => {
      setData(result);
      setRows(result.postings || []);
      setOptions(editorOptions);
    });
  useEffect(() => {
    if (isAdmin) load().catch((e) => setError(e.message));
  }, [isAdmin, year, accountId, bankKey, mobilePayKey]);
  useEffect(() => {
    if (!isAdmin) return;
    membersApi
      .listFinance()
      .then(setMembers)
      .catch(() => setMembers([]));
  }, [isAdmin]);
  const postings = rows;
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
  const userFilterOptions = useMemo(
    () => buildPostingUserOptions(postings, userNames),
    [postings, userNames],
  );
  const groupFilterOptions = useMemo(
    () => buildPostingValueOptions(postings, "postingGroup", "Alle grupper"),
    [postings],
  );
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
      (!user || (user === NO_USER_FILTER ? !p.userId : p.userId === user)),
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
    setYear(selectableYears.includes(now) ? now : selectableYears[0]);
    setQuery("");
    setStatus("");
    setAccount("");
    setGroup("");
    setUser("");
    setAccountId("");
    setBankKey("");
    setMobilePayKey("");
  };
  const updateRow = (row, field, value) =>
    setRows((current) =>
      current.map((item) => (item === row ? { ...item, [field]: value } : item)),
    );
  const saveRow = async (row) => {
    setSaving(row.id);
    setError("");
    try {
      const postingDate = effectivePostingDate(row.postingDate, row.date);
      if (!postingDate) {
        throw new Error("Posteringsdato skal udfyldes.");
      }
      await financeApi.updateAdminPosting(row.id, {
        accountId: row.accountId || "",
        postingGroupId: row.postingGroupId || "",
        userId: row.userId || "",
        postingDate,
        text: row.text || "",
        amount: Number(row.amount || 0),
        document: row.document || "",
      });
      await load();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setSaving("");
    }
  };
  const duplicateRow = async (row) => {
    setError("");
    try {
      await financeApi.duplicateAdminPosting(row.id);
      await load();
    } catch (requestError) {
      setError(requestError.message);
    }
  };
  const deleteRow = async (row) => {
    if (!window.confirm(`Slet posteringen ${row.id}?`)) return;
    setError("");
    try {
      await financeApi.deleteAdminPosting(row.id);
      await load();
    } catch (requestError) {
      setError(requestError.message);
    }
  };
  const updateSelectAndSave = (row, field, value) => {
    updateRow(row, field, value);
    saveRow({ ...row, [field]: value });
  };
  const userOptions = useMemo(() => {
    const values = members.map((member) => ({
      id: member.id ?? member.Id,
      label:
        member.name ??
        member.Name ??
        member.email ??
        member.Email ??
        member.id ??
        member.Id,
    }));
    for (const row of rows) {
      if (row.userId && !values.some((option) => option.id === row.userId)) {
        values.push({ id: row.userId, label: row.userId });
      }
    }
    return values;
  }, [members, rows]);
  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Du har ikke rettigheder til at redigere finanser.
        </p>
      </AdminLayout>
    );
  return (
    <AdminLayout active="" canWrite={true}>
      <div className="menu-panel-header finance-admin-postings-header">
        <div>
          <p className="menu-section-title">Posteringer</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            Redigér finansposteringer
          </p>
        </div>
        <div className="finance-admin-header-actions">
          <button
            className="menu-create-button"
            type="button"
            onClick={() => navigate("/react/admin/finance/postings/new")}
          >
            + Manuel postering
          </button>
        </div>
      </div>
      <div className="finance-admin-filters">
        <label>
          År
          <select
            value={year ?? ""}
            onChange={(e) => setYear(e.target.value === "" ? null : Number(e.target.value))}
          >
            <option value="">Alle år</option>
            {selectableYears.map((availableYear) => (
              <option key={availableYear} value={availableYear}>{availableYear}</option>
            ))}
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
        <FilterSearchableSelect
          label="Posteringsgruppe"
          value={group}
          onChange={setGroup}
          options={groupFilterOptions}
          placeholder="Alle grupper"
        />
        <FilterSearchableSelect
          label="Bruger"
          value={user}
          onChange={setUser}
          options={userFilterOptions}
          placeholder="Alle brugere"
        />
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {!data && !error && (
        <div className="finance-live-loading">Henter posteringer...</div>
      )}
      {data && (
        <div className={`finance-live-panel finance-admin-postings-table finance-admin-cashier-table${isTableExpanded ? " is-expanded" : ""}`}>
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
            <div className="finance-table-control-actions">
              <button className="finance-admin-clear-filter" onClick={reset}>
                Nulstil
              </button>
              <TableExpansionButton
                expanded={isTableExpanded}
                onToggle={() => setIsTableExpanded((current) => !current)}
              />
            </div>
          </div>
          <div className="finance-live-table-scroll">
            <table className="finance-live-table">
              <colgroup>
                {columns.map(([, key]) => (
                  <col key={key} style={{ width: columnWidths[key] }} />
                ))}
                <col style={{ width: 285 }} />
              </colgroup>
              <thead>
                <tr>
                  {columns.map(([label, key]) => (
                    <FinanceResizableHeader
                      key={key}
                      label={label}
                      sortKey={key}
                      sort={sort}
                      setSort={setSort}
                      width={columnWidths[key]}
                      onResizeStart={startResize}
                    />
                  ))}
                  <th style={{ width: 285 }}>Handling</th>
                </tr>
              </thead>
              <tbody>
                {visibleItems.map((p) => (
                  <tr key={p.id}>
                    <td>
                      <input
                        className="finance-admin-inline-input is-readonly"
                        value={p.id}
                        readOnly
                        aria-label={`ID for ${p.id}`}
                      />
                    </td>
                    <td>
                      <input
                        className="finance-admin-inline-input is-readonly"
                        value={p.date || ""}
                        readOnly
                        aria-label={`Dato for ${p.id}`}
                      />
                    </td>
                    <td>
                      <input
                        className="finance-admin-inline-input"
                        type="date"
                        value={effectivePostingDate(p.postingDate, p.date)}
                        onChange={(event) =>
                          updateRow(p, "postingDate", event.target.value)
                        }
                        onBlur={() => saveRow(p)}
                        aria-label={`Posteringsdato for ${p.id}`}
                      />
                    </td>
                    <td>
                      <input
                        className="finance-admin-inline-input"
                        value={p.text || ""}
                        onChange={(event) => updateRow(p, "text", event.target.value)}
                        onBlur={() => saveRow(p)}
                        aria-label={`Tekst for ${p.id}`}
                      />
                    </td>
                    <td>
                      <input
                        className="finance-admin-inline-input finance-admin-amount-input"
                        type="number"
                        step="0.01"
                        value={p.amount ?? ""}
                        onChange={(event) => updateRow(p, "amount", event.target.value)}
                        onBlur={() => saveRow(p)}
                        aria-label={`Beløb for ${p.id}`}
                      />
                    </td>
                    <td>
                      <SearchableSelect
                        label="Bruger"
                        options={userOptions}
                        value={p.userId || ""}
                        onChange={(value) => updateSelectAndSave(p, "userId", value)}
                        placeholder="Ingen bruger"
                        compact
                      />
                      <select
                        style={{ display: "none" }}
                        className="finance-admin-inline-input"
                        value={p.userId || ""}
                        onChange={(event) => updateRow(p, "userId", event.target.value)}
                        onBlur={() => saveRow(p)}
                        aria-label={`Bruger for ${p.id}`}
                      >
                        <option value="">Ingen bruger</option>
                        {userOptions.map((option) => (
                          <option key={option.id} value={option.id}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <SearchableSelect
                        label="Konto"
                        options={options?.accounts || []}
                        value={p.accountId || ""}
                        onChange={(value) => updateSelectAndSave(p, "accountId", value)}
                        placeholder="Vælg konto..."
                        compact
                      />
                      <select
                        style={{ display: "none" }}
                        className="finance-admin-inline-input"
                        value={p.accountId || ""}
                        onChange={(event) => updateRow(p, "accountId", event.target.value)}
                        onBlur={() => saveRow(p)}
                        aria-label={`Konto for ${p.id}`}
                      >
                        <option value="">Vælg konto...</option>
                        {(options?.accounts || []).map((option) => (
                          <option key={option.id} value={option.id}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <SearchableSelect
                        label="Posteringsgruppe"
                        options={options?.postingGroups || []}
                        value={p.postingGroupId || ""}
                        onChange={(value) => updateSelectAndSave(p, "postingGroupId", value)}
                        placeholder="Vælg gruppe..."
                        compact
                      />
                      <select
                        style={{ display: "none" }}
                        className="finance-admin-inline-input"
                        value={p.postingGroupId || ""}
                        onChange={(event) =>
                          updateRow(p, "postingGroupId", event.target.value)
                        }
                        onBlur={() => saveRow(p)}
                        aria-label={`Posteringsgruppe for ${p.id}`}
                      >
                        <option value="">Vælg gruppe...</option>
                        {(options?.postingGroups || []).map((option) => (
                          <option key={option.id} value={option.id}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <span
                        className={`finance-admin-status is-${p.status
                          .toLowerCase()
                          .replaceAll(" ", "-")}`}
                      >
                        {p.status}
                      </span>
                    </td>
                    <td className="finance-admin-posting-actions">
                      <button
                        className="finance-admin-row-more"
                        type="button"
                        onClick={() =>
                          navigate(
                            `/react/admin/finance/postings/${encodeURIComponent(p.id)}`,
                          )
                        }
                      >
                        Se mere
                      </button>
                      <button
                        className="finance-admin-row-copy"
                        type="button"
                        disabled={saving === p.id}
                        onClick={() => duplicateRow(p)}
                      >
                        Dupliker
                      </button>
                      <button
                        className="finance-admin-row-delete"
                        type="button"
                        onClick={() => deleteRow(p)}
                      >
                        Slet
                      </button>
                    </td>
                  </tr>
                ))}
                {visibleItems.length === 0 && (
                  <tr>
                    <td colSpan="10" className="finance-admin-empty">
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

export function FinanceAdminPostingsPage({ isAdmin, search }) {
  const initial = filters(search);
  const now = new Date().getFullYear();
  const [year, setYear] = useState(initial.year);
  const [accountId, setAccountId] = useState(initial.accountId);
  const [bankKey, setBankKey] = useState(initial.bankKey);
  const [mobilePayKey, setMobilePayKey] = useState(initial.mobilePayKey);
  const [query, setQuery] = useState(initial.query);
  const [status, setStatus] = useState("");
  const [account, setAccount] = useState("");
  const [group, setGroup] = useState("");
  const [user, setUser] = useState("");
  const [sort, setSort] = useState({ key: "postingDate", direction: "desc" });
  const [pageSize, setPageSize] = useState(25);
  const [page, setPage] = useState(1);
  const [data, setData] = useState(null);
  const [members, setMembers] = useState([]);
  const [error, setError] = useState("");
  const [exporting, setExporting] = useState(false);
  const [isTableExpanded, setIsTableExpanded] = useState(false);
  useExpandedTable(isTableExpanded, () => setIsTableExpanded(false));
  const postingYears = usePostingYears(isAdmin);
  const selectableYears = postingYears.length ? postingYears : [now, now - 1];
  useEffect(() => {
    if (year !== null && postingYears.length && !postingYears.includes(year)) {
      setYear(postingYears[0]);
    }
  }, [postingYears, year]);

  useEffect(() => {
    if (!isAdmin) return;
    financeApi
      .adminPostings(year, accountId, bankKey, mobilePayKey)
      .then(setData)
      .catch((requestError) => setError(requestError.message));
  }, [isAdmin, year, accountId, bankKey, mobilePayKey]);
  useEffect(() => {
    if (isAdmin) membersApi.listFinance().then(setMembers).catch(() => setMembers([]));
  }, [isAdmin]);

  const postings = data?.postings || [];
  const userNames = useMemo(() => buildUserNameMap(members), [members]);
  const userLabel = (userId) => userNames.get(userId) || userId;
  const userFilterOptions = useMemo(
    () => buildPostingUserOptions(postings, userNames),
    [postings, userNames],
  );
  const groupFilterOptions = useMemo(
    () => buildPostingValueOptions(postings, "postingGroup", "Alle grupper"),
    [postings],
  );
  const values = (key) => [...new Set(postings.map((p) => p[key]).filter(Boolean))].sort();
  const shown = postings.filter(
    (p) =>
      (!query || [p.id, p.text, p.account, p.postingGroup].join(" ").toLowerCase().includes(query.toLowerCase())) &&
      (!status || p.status === status) &&
      (!account || p.account === account) &&
      (!group || p.postingGroup === group) &&
      (!user || (user === NO_USER_FILTER ? !p.userId : p.userId === user)),
  );
  const sorted = useSortedMembers(shown, sort);
  const { currentPage, pageCount, visibleItems } = usePagedItems(sorted, page, pageSize);
  useEffect(() => setPage(1), [query, status, account, group, user, year, pageSize, sort.key, sort.direction]);
  const reset = () => {
    setYear(selectableYears.includes(now) ? now : selectableYears[0]);
    setQuery("");
    setStatus("");
    setAccount("");
    setGroup("");
    setUser("");
    setAccountId("");
    setBankKey("");
    setMobilePayKey("");
  };
  const exportRows = async () => {
    setExporting(true);
    setError("");
    if (year === null) {
      setError("Vælg et specifikt år før eksport.");
      setExporting(false);
      return;
    }
    try {
      const overview = await financeApi.adminOverview(year);
      const exportUserNames = members.length
        ? userNames
        : buildUserNameMap(await membersApi.listFinance());
      const workbook = buildFinanceWorkbookXlsx({
        year,
        postings: sorted.map((posting) => ({
          ...posting,
          userName: posting.userId
            ? exportUserNames.get(posting.userId) || "Ukendt bruger"
            : "",
        })),
        overview,
      });
      const blob = new Blob([workbook], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `finans-${year}-eksport.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setExporting(false);
    }
  };

  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">Du har ikke rettigheder til at redigere finanser.</p>
      </AdminLayout>
    );
  return (
    <AdminLayout active="" canWrite={true}>
      <div className="menu-panel-header">
        <div><p className="menu-section-title">Posteringer</p></div>
        <div className="finance-admin-header-actions">
          <button
            className="menu-create-button"
            type="button"
            onClick={exportRows}
            disabled={exporting}
          >
            {exporting ? "Eksporterer..." : "Eksporter"}
          </button>
        </div>
      </div>
      <div className="finance-admin-filters">
        <label>År<select value={year ?? ""} onChange={(event) => setYear(event.target.value === "" ? null : Number(event.target.value))}><option value="">Alle år</option>{selectableYears.map((availableYear) => <option key={availableYear} value={availableYear}>{availableYear}</option>)}</select></label>
        <label>Søg<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="ID, tekst, konto..." /></label>
        <label>Status<select value={status} onChange={(event) => setStatus(event.target.value)}><option value="">Alle</option><option>Ukategoriseret</option><option>Mangler bilag</option><option>Bogført</option></select></label>
        <label>Konto<select value={account} onChange={(event) => setAccount(event.target.value)}><option value="">Alle konti</option>{values("account").map((value) => <option key={value}>{value}</option>)}</select></label>
        <FilterSearchableSelect label="Posteringsgruppe" value={group} onChange={setGroup} options={groupFilterOptions} placeholder="Alle grupper" />
        <FilterSearchableSelect label="Bruger" value={user} onChange={setUser} options={userFilterOptions} placeholder="Alle brugere" />
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {!data && !error && <div className="finance-live-loading">Henter posteringer...</div>}
      {data && (
        <div className={`finance-live-panel finance-admin-postings-table${isTableExpanded ? " is-expanded" : ""}`}>
          <div className="finance-admin-table-controls">
            <label className="menu-table-page-size"><span>Vis</span><select value={pageSize} onChange={(event) => setPageSize(Number(event.target.value))}>{[10, 25, 50, 100].map((size) => <option value={size} key={size}>{size}</option>)}</select><span>pr. side</span></label>
            <div className="finance-table-control-actions">
              <button className="finance-admin-clear-filter" type="button" onClick={reset}>Nulstil</button>
              <TableExpansionButton
                expanded={isTableExpanded}
                onToggle={() => setIsTableExpanded((current) => !current)}
              />
            </div>
          </div>
          <div className="finance-live-table-scroll">
            <table className="finance-live-table">
              <thead><tr>{[["ID", "id"], ["Dato", "date"], ["Posteringsdato", "postingDate"], ["Tekst", "text"], ["Beløb", "amount"], ["Bruger", "userId"], ["Konto", "account"], ["Posteringsgruppe", "postingGroup"], ["Bilag", "document"], ["Kilde", "sourceType"], ["Status", "status"]].map(([label, key]) => <SortableHeader key={key} label={label} sortKey={key} sort={sort} setSort={setSort} />)}</tr></thead>
              <tbody>
                {visibleItems.map((posting) => (
                  <tr className="finance-admin-clickable-row" key={posting.id} onClick={() => navigate(`/react/admin/finance/postings/${encodeURIComponent(posting.id)}`)} onKeyDown={(event) => event.key === "Enter" && navigate(`/react/admin/finance/postings/${encodeURIComponent(posting.id)}`)} role="link" tabIndex="0">
                    <td>{posting.id}</td><td>{posting.date}</td><td>{posting.postingDate}</td><td>{posting.text}</td><td className={cls(posting.amount)}>{money(posting.amount)}</td><td>{posting.userId ? userLabel(posting.userId) : "—"}</td><td>{posting.account || "—"}</td><td>{posting.postingGroup || "—"}</td><td>{posting.document ? "Ja" : "—"}</td><td>{posting.sourceType}</td><td>{posting.status}</td>
                  </tr>
                ))}
                {visibleItems.length === 0 && <tr><td colSpan="11" className="finance-admin-empty">Ingen posteringer matcher filtrene.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={currentPage} pageCount={pageCount} total={shown.length} pageSize={pageSize} setPage={setPage} />
        </div>
      )}
    </AdminLayout>
  );
}

export function FinanceAdminAccountsPage({ isAdmin }) {
  const [rows, setRows] = useState([]);
  const [search, setSearch] = useState("");
  const [pageSize, setPageSize] = useState(10);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState({ key: "accountKey", direction: "asc" });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [isTableExpanded, setIsTableExpanded] = useState(false);

  useExpandedTable(isTableExpanded, () => setIsTableExpanded(false));

  useEffect(() => {
    if (!isAdmin) return;
    setLoading(true);
    financeApi
      .adminAccounts()
      .then(setRows)
      .catch((requestError) => setError(requestError.message))
      .finally(() => setLoading(false));
  }, [isAdmin]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return rows;
    return rows.filter((row) =>
      [
        row.id,
        row.mainAccount,
        row.accountKey,
        row.subAccount,
        row.subAccountKey,
        row.context,
        row.contextKey,
      ]
        .map((value) => String(value ?? ""))
        .join(" ")
        .toLowerCase()
        .includes(term),
    );
  }, [rows, search]);
  const sorted = useSortedMembers(filtered, sort);
  const { currentPage, pageCount, visibleItems } = usePagedItems(
    sorted,
    page,
    pageSize,
  );

  useEffect(() => setPage(1), [search, pageSize, sort.key, sort.direction]);

  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Du har ikke rettigheder til at redigere finanser.
        </p>
      </AdminLayout>
    );

  return (
    <AdminLayout active="" canWrite={true}>
      <div className="menu-panel-header finance-admin-account-plan-header">
        <div>
          <p className="menu-section-title">Konti</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            Se kontoplan
          </p>
        </div>
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {loading && !error && (
        <div className="finance-live-loading">Henter kontoplan...</div>
      )}
      {!loading && !error && (
        <>
          <SearchToolbar
            search={search}
            setSearch={setSearch}
            searchPlaceholder="Søg i kontoplan"
          />
          <div className={`finance-live-panel finance-admin-account-plan-table${isTableExpanded ? " is-expanded" : ""}`}>
            <div className="finance-admin-table-controls">
              <label className="menu-table-page-size finance-admin-expanded-page-size">
                <span>Vis</span>
                <select value={pageSize} onChange={(event) => setPageSize(Number(event.target.value))}>
                  {[10, 25, 50, 100].map((size) => <option value={size} key={size}>{size}</option>)}
                </select>
                <span>pr. side</span>
              </label>
              <TableExpansionButton
                expanded={isTableExpanded}
                onToggle={() => setIsTableExpanded((current) => !current)}
              />
            </div>
            <div className="finance-live-table-scroll">
              <table className="finance-live-table">
                <thead>
                  <tr>
                    <SortableHeader
                      label="ID"
                      sortKey="id"
                      sort={sort}
                      setSort={setSort}
                    />
                    <SortableHeader
                      label="Hovedkonto"
                      sortKey="mainAccount"
                      sort={sort}
                      setSort={setSort}
                    />
                    <SortableHeader
                      label="Kontonøgle"
                      sortKey="accountKey"
                      sort={sort}
                      setSort={setSort}
                    />
                    <SortableHeader
                      label="Underkonto"
                      sortKey="subAccount"
                      sort={sort}
                      setSort={setSort}
                    />
                    <SortableHeader
                      label="Underkontonøgle"
                      sortKey="subAccountKey"
                      sort={sort}
                      setSort={setSort}
                    />
                    <SortableHeader
                      label="Kontekst"
                      sortKey="context"
                      sort={sort}
                      setSort={setSort}
                    />
                    <SortableHeader
                      label="Kontekstnøgle"
                      sortKey="contextKey"
                      sort={sort}
                      setSort={setSort}
                    />
                  </tr>
                </thead>
                <tbody>
                  {visibleItems.map((row) => (
                    <tr key={row.id}>
                      <td>{row.id || "—"}</td>
                      <td>{row.mainAccount || "—"}</td>
                      <td>{row.accountKey ?? "—"}</td>
                      <td>{row.subAccount || "—"}</td>
                      <td>{row.subAccountKey ?? "—"}</td>
                      <td>{row.context || "—"}</td>
                      <td>{row.contextKey ?? "—"}</td>
                    </tr>
                  ))}
                  {visibleItems.length === 0 && (
                    <tr>
                      <td colSpan="7" className="finance-admin-empty">
                        Ingen konti matcher søgningen.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            <div className="finance-admin-expanded-pagination">
              <Pagination
                page={currentPage}
                pageCount={pageCount}
                total={sorted.length}
                pageSize={pageSize}
                setPage={setPage}
              />
            </div>
          </div>
          <Pagination
            page={currentPage}
            pageCount={pageCount}
            total={sorted.length}
            pageSize={pageSize}
            setPage={setPage}
          />
        </>
      )}
    </AdminLayout>
  );
}

function newBudgetRow() {
  return {
    id: "",
    accountId: "",
    postingGroupId: "",
    yearActual: new Date().getFullYear(),
    forecast: 0,
    forecastType: "",
    _new: true,
  };
}

export function FinanceAdminBudgetsPage({ isAdmin }) {
  const [data, setData] = useState(null);
  const [rows, setRows] = useState([]);
  const [search, setSearch] = useState("");
  const [pageSize, setPageSize] = useState(10);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState({ key: "yearActual", direction: "desc" });
  const [saving, setSaving] = useState("");
  const [error, setError] = useState("");
  const [rowErrors, setRowErrors] = useState({});
  const [isTableExpanded, setIsTableExpanded] = useState(false);
  useExpandedTable(isTableExpanded, () => setIsTableExpanded(false));

  useEffect(() => {
    if (!isAdmin) return;
    financeApi
      .adminBudgets()
      .then((result) => {
        setData(result);
        setRows(
          (result.budgets || []).map((row) => ({
            ...row,
            _originalId: row.id,
          })),
        );
      })
      .catch((requestError) => setError(requestError.message));
  }, [isAdmin]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return rows;
    return rows.filter((row) =>
      [
        row.id,
        row.accountId,
        row.postingGroupId,
        row.yearActual,
        row.forecast,
        row.forecastType,
      ]
        .join(" ")
        .toLowerCase()
        .includes(term),
    );
  }, [rows, search]);
  const sorted = useSortedMembers(filtered, sort);
  const { currentPage, pageCount, visibleItems } = usePagedItems(
    sorted,
    page,
    pageSize,
  );

  useEffect(() => setPage(1), [search, pageSize, sort.key, sort.direction]);

  const accountOptions = data?.accounts || [];
  const groupOptions = data?.postingGroups || [];
  const updateRow = (row, field, value) => {
    setRows((current) =>
      current.map((item) =>
        item === row ? { ...item, [field]: value } : item,
      ),
    );
    setRowErrors((current) => ({
      ...current,
      [row._originalId || row.id]: "",
    }));
  };
  const addRow = () => {
    setRows((current) => [newBudgetRow(), ...current]);
    setPage(1);
  };
  const removeDraft = (row) =>
    setRows((current) => current.filter((item) => item !== row));
  const deleteRow = async (row) => {
    if (!window.confirm(`Slet budgetposten ${row.id}?`)) return;
    try {
      await financeApi.deleteAdminBudget(row.id);
      setRows((current) => current.filter((item) => item.id !== row.id));
    } catch (requestError) {
      setError(requestError.message);
    }
  };
  const saveRow = async (row) => {
    const key = row._originalId || row.id || "new";
    if (!row.id.trim()) {
      setRowErrors((current) => ({
        ...current,
        [key]: "ID må ikke være tomt.",
      }));
      return;
    }
    if (!row.accountId) {
      setRowErrors((current) => ({
        ...current,
        [key]: "Account ID skal vælges.",
      }));
      return;
    }
    setSaving(key);
    setError("");
    try {
      const payload = {
        id: row.id.trim(),
        accountId: row.accountId,
        postingGroupId: row.postingGroupId || "",
        yearActual: Number(row.yearActual),
        forecast: Number(row.forecast),
        forecastType: row.forecastType || "",
      };
      const saved = row._new
        ? await financeApi.createAdminBudget(payload)
        : await financeApi.updateAdminBudget(
            row._originalId || row.id,
            payload,
          );
      setRows((current) =>
        current.map((item) =>
          item === row ? { ...saved, _originalId: saved.id } : item,
        ),
      );
      setRowErrors((current) => ({ ...current, [key]: "" }));
    } catch (requestError) {
      setRowErrors((current) => ({ ...current, [key]: requestError.message }));
    } finally {
      setSaving("");
    }
  };

  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Du har ikke rettigheder til at redigere finanser.
        </p>
      </AdminLayout>
    );

  return (
    <AdminLayout active="" canWrite={true}>
      <div className="menu-panel-header finance-admin-budget-header finance-admin-budgets-header">
        <div>
          <p className="menu-section-title">Budgetter</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            Administrér budgetlinjer
          </p>
        </div>
        <button
          className="menu-create-button"
          type="button"
          onClick={() => navigate("/react/admin/finance/budgets/new")}
        >
          + Ny post
        </button>
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {!data && !error && (
        <div className="finance-live-loading">Henter budgetter...</div>
      )}
      {data && (
        <>
          <SearchToolbar
            search={search}
            setSearch={setSearch}
            searchPlaceholder="Søg i budgetter"
          />
          <div className={`finance-live-panel finance-admin-budget-table finance-admin-budgets-table${isTableExpanded ? " is-expanded" : ""}`}>
            <div className="finance-admin-table-controls">
              <label className="menu-table-page-size finance-admin-expanded-page-size">
                <span>Vis</span>
                <select value={pageSize} onChange={(event) => setPageSize(Number(event.target.value))}>
                  {[10, 25, 50, 100].map((size) => <option value={size} key={size}>{size}</option>)}
                </select>
                <span>pr. side</span>
              </label>
              <div className="finance-table-control-actions">
                <TableExpansionButton
                  expanded={isTableExpanded}
                  onToggle={() => setIsTableExpanded((current) => !current)}
                />
              </div>
            </div>
            <div className="finance-live-table-scroll">
              <table className="finance-live-table">
                <thead>
                  <tr>
                    {[
                      "id",
                      "accountId",
                      "postingGroupId",
                      "yearActual",
                      "forecast",
                      "forecastType",
                    ].map((key) => (
                      <SortableHeader
                        key={key}
                        label={
                          {
                            id: "ID",
                            accountId: "Account ID",
                            postingGroupId: "Posteringsgruppe",
                            yearActual: "År",
                            forecast: "Beløb",
                            forecastType: "Budgettype",
                          }[key]
                        }
                        sortKey={key}
                        sort={sort}
                        setSort={setSort}
                      />
                    ))}
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {visibleItems.map((row) => {
                    const key = row._originalId || row.id || "new";
                    return (
                      <tr key={key}>
                        <td>
                          <input
                            value={row.id}
                            onChange={(event) =>
                              updateRow(row, "id", event.target.value)
                            }
                            aria-label="ID"
                          />
                        </td>
                        <td>
                          <SearchableSelect
                            label="Konto"
                            options={accountOptions}
                            value={row.accountId}
                            onChange={(value) => updateRow(row, "accountId", value)}
                            placeholder="Vælg konto..."
                            compact
                          />
                          <select
                            style={{ display: "none" }}
                            value={row.accountId}
                            onChange={(event) =>
                              updateRow(row, "accountId", event.target.value)
                            }
                            aria-label="Account ID"
                            required
                          >
                            <option value="">Vælg konto...</option>
                            {accountOptions.map((option) => (
                              <option value={option.id} key={option.id}>
                                {option.id}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <SearchableSelect
                            label="Posteringsgruppe"
                            options={groupOptions}
                            value={row.postingGroupId || ""}
                            onChange={(value) => updateRow(row, "postingGroupId", value)}
                            placeholder="Ingen posteringsgruppe"
                            compact
                          />
                          <select
                            style={{ display: "none" }}
                            value={row.postingGroupId || ""}
                            onChange={(event) =>
                              updateRow(
                                row,
                                "postingGroupId",
                                event.target.value,
                              )
                            }
                            aria-label="Posteringsgruppe"
                          >
                            <option value="">Ingen posteringsgruppe</option>
                            {groupOptions.map((option) => (
                              <option value={option.id} key={option.id}>
                                {option.id}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <input
                            type="number"
                            min="1"
                            step="1"
                            value={row.yearActual}
                            onChange={(event) =>
                              updateRow(row, "yearActual", event.target.value)
                            }
                            aria-label="År"
                          />
                        </td>
                        <td>
                          <input
                            type="number"
                            step="0.01"
                            value={row.forecast}
                            onChange={(event) =>
                              updateRow(row, "forecast", event.target.value)
                            }
                            aria-label="Beløb"
                          />
                        </td>
                        <td>
                          <input
                            value={row.forecastType}
                            onChange={(event) =>
                              updateRow(row, "forecastType", event.target.value)
                            }
                            aria-label="Budgettype"
                          />
                        </td>
                        <td className="finance-admin-budget-actions">
                          <button
                            className="finance-admin-save-button"
                            type="button"
                            disabled={saving === key}
                            onClick={() => saveRow(row)}
                          >
                            {saving === key ? "Gemmer..." : "Gem"}
                          </button>
                          {!row._new && (
                            <button
                              className="finance-admin-delete-button"
                              type="button"
                              onClick={() => deleteRow(row)}
                            >
                              Slet
                            </button>
                          )}
                          {row._new && (
                            <button
                              className="finance-admin-cancel-button"
                              type="button"
                              onClick={() => removeDraft(row)}
                            >
                              Fjern
                            </button>
                          )}
                          {rowErrors[key] && (
                            <small className="finance-admin-row-error">
                              {rowErrors[key]}
                            </small>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                  {visibleItems.length === 0 && (
                    <tr>
                      <td colSpan="7" className="finance-admin-empty">
                        Ingen budgetposter fundet.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            <div className="finance-admin-expanded-pagination">
              <Pagination
                page={currentPage}
                pageCount={pageCount}
                total={sorted.length}
                pageSize={pageSize}
                setPage={setPage}
              />
            </div>
          </div>
          <Pagination
            page={currentPage}
            pageCount={pageCount}
            total={sorted.length}
            pageSize={pageSize}
            setPage={setPage}
          />
        </>
      )}
    </AdminLayout>
  );
}

export function FinanceAdminBudgetDetailPage({ isAdmin, id }) {
  const isNew = id === "new";
  const [options, setOptions] = useState(null);
  const [form, setForm] = useState(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!isAdmin) return;
    Promise.all([
      financeApi.adminBudgets(),
      isNew ? Promise.resolve(newBudgetRow()) : financeApi.adminBudget(id),
    ])
      .then(([budgetData, budget]) => {
        setOptions(budgetData);
        setForm({
          id: budget.id || "",
          accountId: budget.accountId || "",
          postingGroupId: budget.postingGroupId || "",
          yearActual: budget.yearActual || new Date().getFullYear(),
          forecast: budget.forecast ?? 0,
          forecastType: budget.forecastType || "",
        });
      })
      .catch((requestError) => setError(requestError.message));
  }, [id, isAdmin, isNew]);

  const update = (field, value) =>
    setForm((current) => ({ ...current, [field]: value }));

  async function save(event) {
    event.preventDefault();
    setError("");
    setMessage("");
    if (!form.id.trim()) return setError("ID må ikke være tomt.");
    if (!form.accountId) return setError("Account ID skal vælges.");
    if (
      !Number.isInteger(Number(form.yearActual)) ||
      Number(form.yearActual) < 1
    ) {
      return setError("År skal være gyldigt.");
    }
    setSaving(true);
    try {
      const payload = {
        id: form.id.trim(),
        accountId: form.accountId,
        postingGroupId: form.postingGroupId || "",
        yearActual: Number(form.yearActual),
        forecast: Number(form.forecast),
        forecastType: form.forecastType || "",
      };
      if (isNew) await financeApi.createAdminBudget(payload);
      else await financeApi.updateAdminBudget(id, payload);
      navigate("/react/admin/finance/budgets");
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setSaving(false);
    }
  }

  async function remove() {
    if (isNew || !window.confirm(`Slet budgetposten ${id}?`)) return;
    try {
      await financeApi.deleteAdminBudget(id);
      navigate("/react/admin/finance/budgets");
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Du har ikke rettigheder til at redigere finanser.
        </p>
      </AdminLayout>
    );

  return (
    <AdminLayout
      active=""
      canWrite={true}
      contentClassName="finance-admin-detail-content"
    >
      <div className="menu-panel-header finance-admin-detail-header finance-admin-budget-detail-header">
        <div>
          <p className="menu-section-title">Budget</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            {isNew ? "Opret budgetpost" : "Redigér budgetpost"}
          </p>
        </div>
        <Link
          className="finance-live-profile-link"
          href="/react/admin/finance/budgets"
        >
          Tilbage til budgetter
        </Link>
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {!form && !error && (
        <div className="finance-live-loading">Henter budgetpost...</div>
      )}
      {form && options && (
        <form
          className="finance-admin-budget-detail-form finance-admin-budget-detail-editor"
          onSubmit={save}
        >
          <div className="finance-admin-detail-form-heading">
            <div>
              <p className="finance-live-kicker">Budgetlinje</p>
              <h2>{isNew ? "Ny budgetpost" : form.id}</h2>
            </div>
          </div>
          <div className="finance-admin-detail-fields">
            <label>
              ID
              <input
                value={form.id}
                onChange={(event) => update("id", event.target.value)}
                required
              />
            </label>
            <label>
              År
              <input
                type="number"
                min="1"
                step="1"
                value={form.yearActual}
                onChange={(event) => update("yearActual", event.target.value)}
                required
              />
            </label>
            <label>
              Account ID
              <select
                value={form.accountId}
                onChange={(event) => update("accountId", event.target.value)}
                required
              >
                <option value="">Vælg konto...</option>
                {(options.accounts || []).map((option) => (
                  <option value={option.id} key={option.id}>
                    {option.id}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Posteringsgruppe
              <select
                value={form.postingGroupId}
                onChange={(event) =>
                  update("postingGroupId", event.target.value)
                }
              >
                <option value="">Ingen posteringsgruppe</option>
                {(options.postingGroups || []).map((option) => (
                  <option value={option.id} key={option.id}>
                    {option.id}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Beløb
              <input
                type="number"
                step="0.01"
                value={form.forecast}
                onChange={(event) => update("forecast", event.target.value)}
                required
              />
            </label>
            <label>
              Budgettype
              <input
                value={form.forecastType}
                onChange={(event) => update("forecastType", event.target.value)}
              />
            </label>
          </div>
          <div className="finance-admin-detail-actions">
            <button className="profile-button" type="submit" disabled={saving}>
              {saving ? "Gemmer..." : "Gem ændringer"}
            </button>
            {!isNew && (
              <button
                className="finance-admin-delete-button"
                type="button"
                onClick={remove}
              >
                Slet budgetpost
              </button>
            )}
            {message && (
              <p className="status-message status-message-success">{message}</p>
            )}
          </div>
        </form>
      )}
    </AdminLayout>
  );
}

function newPostingGroup() {
  return { id: "", postingGroup: "", context: "", _new: true };
}

export function FinanceAdminPostingGroupsPage({ isAdmin }) {
  const [rows, setRows] = useState([]);
  const [search, setSearch] = useState("");
  const [pageSize, setPageSize] = useState(10);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState({ key: "postingGroup", direction: "asc" });
  const [error, setError] = useState("");
  const [saving, setSaving] = useState("");

  const load = () =>
    financeApi
      .adminPostingGroups()
      .then((result) =>
        setRows(result.map((row) => ({ ...row, _originalId: row.id }))),
      );
  useEffect(() => {
    if (isAdmin) load().catch((requestError) => setError(requestError.message));
  }, [isAdmin]);
  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return rows.filter(
      (row) =>
        !term ||
        [row.id, row.postingGroup, row.context]
          .join(" ")
          .toLowerCase()
          .includes(term),
    );
  }, [rows, search]);
  const sorted = useSortedMembers(filtered, sort);
  const { currentPage, pageCount, visibleItems } = usePagedItems(
    sorted,
    page,
    pageSize,
  );
  useEffect(() => setPage(1), [search, pageSize, sort.key, sort.direction]);
  const updateRow = (row, field, value) =>
    setRows((current) =>
      current.map((item) =>
        item === row ? { ...item, [field]: value } : item,
      ),
    );
  const saveRow = async (row) => {
    if (!row.id.trim()) return setError("ID må ikke være tomt.");
    const key = row._originalId || row.id;
    setSaving(key);
    setError("");
    try {
      const payload = {
        id: row.id.trim(),
        postingGroup: row.postingGroup || "",
        context: row.context || "",
      };
      const saved = row._new
        ? await financeApi.createAdminPostingGroup(payload)
        : await financeApi.updateAdminPostingGroup(
            row._originalId || row.id,
            payload,
          );
      setRows((current) =>
        current.map((item) =>
          item === row ? { ...saved, _originalId: saved.id } : item,
        ),
      );
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setSaving("");
    }
  };
  const deleteRow = async (row) => {
    if (!window.confirm(`Slet posteringsgruppen ${row.id}?`)) return;
    try {
      await financeApi.deleteAdminPostingGroup(row.id);
      setRows((current) => current.filter((item) => item !== row));
    } catch (requestError) {
      setError(requestError.message);
    }
  };
  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Du har ikke rettigheder til at redigere finanser.
        </p>
      </AdminLayout>
    );
  return (
    <AdminLayout active="" canWrite={true}>
      <div className="menu-panel-header finance-admin-budget-header finance-admin-groups-header">
        <div>
          <p className="menu-section-title">Posteringsgrupper</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            Redigér posteringsgrupper
          </p>
        </div>
        <button
          className="menu-create-button"
          type="button"
          onClick={() => navigate("/react/admin/finance/posting-groups/new")}
        >
          + Ny post
        </button>
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      <SearchToolbar
        search={search}
        setSearch={setSearch}
        pageSize={pageSize}
        setPageSize={setPageSize}
        searchPlaceholder="Søg i posteringsgrupper"
      />
      <div className="finance-live-panel finance-admin-budget-table finance-admin-groups-table">
        <div className="finance-live-table-scroll">
          <table className="finance-live-table">
            <thead>
              <tr>
                <SortableHeader
                  label="ID"
                  sortKey="id"
                  sort={sort}
                  setSort={setSort}
                />
                <SortableHeader
                  label="Posteringsgruppe"
                  sortKey="postingGroup"
                  sort={sort}
                  setSort={setSort}
                />
                <SortableHeader
                  label="Kontekst"
                  sortKey="context"
                  sort={sort}
                  setSort={setSort}
                />
                <th />
              </tr>
            </thead>
            <tbody>
              {visibleItems.map((row) => {
                const key = row._originalId || row.id || "new";
                return (
                  <tr key={key}>
                    <td>
                      <input
                        value={row.id}
                        onChange={(event) =>
                          updateRow(row, "id", event.target.value)
                        }
                        aria-label="ID"
                      />
                    </td>
                    <td>
                      <input
                        value={row.postingGroup}
                        onChange={(event) =>
                          updateRow(row, "postingGroup", event.target.value)
                        }
                        aria-label="Posteringsgruppe"
                      />
                    </td>
                    <td>
                      <input
                        value={row.context}
                        onChange={(event) =>
                          updateRow(row, "context", event.target.value)
                        }
                        aria-label="Kontekst"
                      />
                    </td>
                    <td className="finance-admin-budget-actions">
                      <button
                        className="finance-admin-save-button"
                        type="button"
                        disabled={saving === key}
                        onClick={() => saveRow(row)}
                      >
                        {saving === key ? "Gemmer..." : "Gem"}
                      </button>
                      <button
                        className="finance-admin-delete-button"
                        type="button"
                        onClick={() => deleteRow(row)}
                      >
                        Slet
                      </button>
                    </td>
                  </tr>
                );
              })}
              {visibleItems.length === 0 && (
                <tr>
                  <td colSpan="4" className="finance-admin-empty">
                    Ingen posteringsgrupper fundet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      <Pagination
        page={currentPage}
        pageCount={pageCount}
        total={sorted.length}
        pageSize={pageSize}
        setPage={setPage}
      />
    </AdminLayout>
  );
}

export function FinanceAdminPostingGroupDetailPage({ isAdmin, id }) {
  const isNew = id === "new";
  const [form, setForm] = useState(null);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  useEffect(() => {
    if (!isAdmin) return;
    (isNew
      ? Promise.resolve(newPostingGroup())
      : financeApi.adminPostingGroup(id)
    )
      .then(setForm)
      .catch((requestError) => setError(requestError.message));
  }, [id, isAdmin, isNew]);
  const update = (field, value) =>
    setForm((current) => ({ ...current, [field]: value }));
  async function save(event) {
    event.preventDefault();
    if (!form.id.trim()) return setError("ID må ikke være tomt.");
    setSaving(true);
    setError("");
    try {
      const payload = {
        id: form.id.trim(),
        postingGroup: form.postingGroup || "",
        context: form.context || "",
      };
      if (isNew) await financeApi.createAdminPostingGroup(payload);
      else await financeApi.updateAdminPostingGroup(id, payload);
      navigate("/react/admin/finance/posting-groups");
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setSaving(false);
    }
  }
  async function remove() {
    if (isNew || !window.confirm(`Slet posteringsgruppen ${id}?`)) return;
    try {
      await financeApi.deleteAdminPostingGroup(id);
      navigate("/react/admin/finance/posting-groups");
    } catch (requestError) {
      setError(requestError.message);
    }
  }
  if (!isAdmin)
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Du har ikke rettigheder til at redigere finanser.
        </p>
      </AdminLayout>
    );
  return (
    <AdminLayout
      active=""
      canWrite={true}
      contentClassName="finance-admin-detail-content"
    >
      <div className="menu-panel-header finance-admin-detail-header finance-admin-posting-group-detail-header">
        <div>
          <p className="menu-section-title">Posteringsgruppe</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            {isNew ? "Opret posteringsgruppe" : "Redigér posteringsgruppe"}
          </p>
        </div>
        <Link
          className="finance-live-profile-link"
          href="/react/admin/finance/posting-groups"
        >
          Tilbage til posteringsgrupper
        </Link>
      </div>
      {error && <p className="finance-live-error">{error}</p>}
      {!form && !error && (
        <div className="finance-live-loading">Henter posteringsgruppe...</div>
      )}
      {form && (
        <form
          className="finance-admin-budget-detail-form finance-admin-posting-group-detail-form"
          onSubmit={save}
        >
          <p className="finance-live-kicker">Redigering</p>
          <h2>{isNew ? "Ny posteringsgruppe" : form.id}</h2>
          <div className="finance-admin-detail-fields">
            <label>
              ID
              <input
                value={form.id}
                onChange={(event) => update("id", event.target.value)}
                required
              />
            </label>
            <label>
              Posteringsgruppe
              <input
                value={form.postingGroup}
                onChange={(event) => update("postingGroup", event.target.value)}
              />
            </label>
            <label className="finance-admin-detail-field-full">
              Kontekst
              <input
                value={form.context}
                onChange={(event) => update("context", event.target.value)}
              />
            </label>
          </div>
          <div className="finance-admin-detail-actions">
            <button className="profile-button" type="submit" disabled={saving}>
              {saving ? "Gemmer..." : "Gem ændringer"}
            </button>
            {!isNew && (
              <button
                className="finance-admin-delete-button"
                type="button"
                onClick={remove}
              >
                Slet posteringsgruppe
              </button>
            )}
          </div>
        </form>
      )}
    </AdminLayout>
  );
}

export function FinanceAdminPostingDetailPage({ isAdmin, canWrite = isAdmin, id }) {
  const isNew = id === "new";
  const today = new Date().toISOString().slice(0, 10);
  const [posting, setPosting] = useState(null);
  const [options, setOptions] = useState(null);
  const [form, setForm] = useState(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin) return;
    Promise.all([
      isNew
        ? Promise.resolve({
            id: "",
            date: today,
            postingDate: today,
            text: "",
            amount: 0,
            userId: "",
            accountId: "",
            postingGroupId: "",
            document: "",
            sourceType: "Manuel",
            account: "",
            postingGroup: "",
            bankReference: "",
            mobilePayReference: "",
            status: "Ukategoriseret",
          })
        : financeApi.adminPosting(id),
      financeApi.adminPostingOptions(),
      membersApi.listFinance(),
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
          id: detail.id || "",
          date: detail.date || "",
          text: detail.text || "",
          amount: detail.amount ?? 0,
          accountId: detail.accountId || "",
          postingGroupId: detail.postingGroupId || "",
          userId: detail.userId || "",
          // Aggregates use the posting date. Imported/legacy rows can have
          // no explicit value, so keep those editable rows visible by using
          // their transaction date as the frontend fallback.
          postingDate:
            effectivePostingDate(detail.postingDate, detail.date) || today,
          document: detail.document || "",
        });
      })
      .catch((requestError) => setError(requestError.message));
  }, [id, isAdmin, canWrite]);

  const update = (field, value) =>
    setForm((current) => ({ ...current, [field]: value }));
  async function save(event) {
    event.preventDefault();
    setMessage("");
    setError("");
    const postingDate = effectivePostingDate(form.postingDate, form.date);
    if (!postingDate) {
      setError("Posteringsdato skal udfyldes.");
      return;
    }
    try {
      if (isNew) {
        await financeApi.createAdminPosting({ ...form, postingDate });
        navigate("/react/admin/finance/postings");
        return;
      }
      await financeApi.updateAdminPosting(id, { ...form, postingDate });
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
          Du har ikke rettigheder til at redigere finanser.
        </p>
      </AdminLayout>
    );
  return (
    <AdminLayout
      active=""
      canWrite={canWrite}
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
          href="/react/admin/finance/postings"
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
            <fieldset disabled={!canWrite}>
            <div className="finance-admin-detail-form-heading">
            <h2>{isNew ? "Ny manuel postering" : posting.text || posting.id}</h2>
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
                <input
                  value={isNew ? form.id : posting.id}
                  readOnly={!isNew}
                  onChange={(event) => update("id", event.target.value)}
                  required={isNew}
                />
              </label>
              <label>
                Kilde
                <input value={posting.sourceType} readOnly />
              </label>
              <label>
                Dato
                <input
                  type={isNew ? "date" : "text"}
                  value={isNew ? form.date : posting.date}
                  readOnly={!isNew}
                  onChange={(event) => update("date", event.target.value)}
                />
              </label>
              <label>
                Beløb
                <input
                  type="number"
                  step="0.01"
                  value={form.amount}
                  onChange={(event) => update("amount", event.target.value)}
                />
              </label>
              <label className="finance-admin-detail-field-full">
                Tekst
                <input
                  value={form.text}
                  onChange={(event) => update("text", event.target.value)}
                />
              </label>
              {!isNew && <label>
                Bankoverførsel
                <input
                  value={
                    posting.bankReference
                      ? `Bank · BA-${posting.bankReference}`
                      : ""
                  }
                  readOnly
                />
              </label>}
              {!isNew && <label>
                MobilePay
                <input
                  value={
                    posting.mobilePayReference
                      ? `MobilePay · MP-${posting.mobilePayReference}`
                      : ""
                  }
                  readOnly
                />
              </label>}
              <label className="finance-admin-detail-field-full">
                Posteringsdato
                <input
                  type="date"
                  value={form.postingDate}
                  required
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
                disabled={!canWrite}
                placeholder="Vælg konto…"
              />
              <SearchableSelect
                label="Posteringsgruppe"
                options={options.postingGroups}
                value={form.postingGroupId}
                onChange={(value) => update("postingGroupId", value)}
                disabled={!canWrite}
                placeholder="Vælg posteringsgruppe…"
              />
              <SearchableSelect
                label="Bruger"
                options={options.users}
                value={form.userId}
                onChange={(value) => update("userId", value)}
                disabled={!canWrite}
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
              {canWrite && <button className="profile-button" type="submit">Gem ændringer</button>}
              {message && (
                <p className="status-message status-message-success">
                  {message}
                </p>
              )}
            </div>
            </fieldset>
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

export function FinanceAdminImportPage({ isAdmin }) {
  const bankInputRef = useRef(null);
  const mobilePayInputRef = useRef(null);
  const [bankFile, setBankFile] = useState(null);
  const [mobilePayFile, setMobilePayFile] = useState(null);
  const [syncPostings, setSyncPostings] = useState(true);
  const [history, setHistory] = useState([]);
  const [historySort, setHistorySort] = useState({ key: "importedAt", direction: "desc" });
  const [historyPageSize, setHistoryPageSize] = useState(10);
  const [historyPage, setHistoryPage] = useState(1);
  const [result, setResult] = useState(null);
  const [postingSyncResult, setPostingSyncResult] = useState(null);
  const [error, setError] = useState("");
  const [isImporting, setIsImporting] = useState(false);
  const [isGeneratingPostings, setIsGeneratingPostings] = useState(false);
  const [fileValidation, setFileValidation] = useState({
    bank: { status: "idle", error: "" },
    mobilePay: { status: "idle", error: "" },
  });
  const validationRequestRef = useRef({ bank: 0, mobilePay: 0 });

  const loadHistory = async () => {
    try {
      setHistory(await financeApi.adminImportHistory());
    } catch (historyError) {
      setError(historyError.message);
    }
  };

  useEffect(() => {
    if (isAdmin) loadHistory();
  }, [isAdmin]);

  const validateSelectedFile = async (kind, file) => {
    const requestId = validationRequestRef.current[kind] + 1;
    validationRequestRef.current[kind] = requestId;
    setFileValidation((current) => ({
      ...current,
      [kind]: { status: file ? "pending" : "idle", error: "" },
    }));
    setError("");
    if (!file) return;

    try {
      await financeApi.validateFinanceCsv({
        bankFile: kind === "bank" ? file : null,
        mobilePayFile: kind === "mobilePay" ? file : null,
      });
      if (validationRequestRef.current[kind] !== requestId) return;
      setFileValidation((current) => ({
        ...current,
        [kind]: { status: "valid", error: "" },
      }));
    } catch (validationError) {
      if (validationRequestRef.current[kind] !== requestId) return;
      setFileValidation((current) => ({
        ...current,
        [kind]: { status: "invalid", error: validationError.message },
      }));
      setError(validationError.message);
    }
  };

  const submit = async (event) => {
    event.preventDefault();
    if (!bankFile && !mobilePayFile) {
      setError("Vælg mindst én CSV-fil.");
      return;
    }
    if (Object.values(fileValidation).some((item) => item.status === "pending")) {
      setError("Vent, mens CSV-filen valideres.");
      return;
    }
    const invalidValidation = Object.values(fileValidation).find((item) => item.status === "invalid");
    if (invalidValidation) {
      setError(invalidValidation.error);
      return;
    }
    setIsImporting(true);
    setError("");
    setResult(null);
    setPostingSyncResult(null);
    try {
      setResult(
        await financeApi.importFinanceCsv({
          bankFile,
          mobilePayFile,
          syncPostings,
        }),
      );
      await loadHistory();
    } catch (importError) {
      setError(importError.message);
    } finally {
      setIsImporting(false);
    }
  };

  const generatePostings = async () => {
    setIsGeneratingPostings(true);
    setError("");
    setPostingSyncResult(null);
    try {
      setPostingSyncResult(await financeApi.generateImportPostings());
    } catch (postingError) {
      setError(postingError.message);
    } finally {
      setIsGeneratingPostings(false);
    }
  };

  const displayFileLabel = (file, emptyLabel) => {
    if (!file) return emptyLabel;
    const fileName = String(file.name || "").split(/[\\/]/).pop();
    return `${fileName} · ${(file.size / 1024).toFixed(1)} KB`;
  };
  const formatImportedAt = (value) => {
    if (!value) return "";
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? value
      : new Intl.DateTimeFormat("da-DK", {
          dateStyle: "medium",
          timeStyle: "short",
        }).format(date);
  };
  const sortedHistory = useSortedMembers(history, historySort);
  const {
    currentPage: currentHistoryPage,
    pageCount: historyPageCount,
    visibleItems: visibleHistory,
  } = usePagedItems(sortedHistory, historyPage, historyPageSize);

  useEffect(() => {
    setHistoryPage(1);
  }, [history, historyPageSize, historySort.key, historySort.direction]);

  if (!isAdmin) {
    return (
      <AdminLayout active="" canWrite={false}>
        <p className="status-message status-message-warning">
          Du har ikke rettigheder til at importere finansdata.
        </p>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout active="" canWrite={true} contentClassName="finance-admin-content">
      <div className="menu-panel-header finance-live-hero finance-import-hero">
        <div>
          <p className="menu-section-title">Importér finansdata</p>
          <p className="menu-panel-lead">
            Upload et bankkontoudtog, MobilePay-transaktioner eller begge dele.
          </p>
        </div>
      </div>

      {/* Deprecated encoded copy retained temporarily so this source remains easy to review.
      <section className="finance-import-introduction">
        <p className="finance-live-kicker">Sådan dannes posteringer</p>
        <h2>Kildedata først — posteringer bagefter</h2>
        <ol>
          <li>CSV-rækkerne valideres og upsertes i henholdsvis bank- og MobilePay-tabellen.</li>
          <li>Matcher bankens <strong>Tekst</strong> præcist en MobilePay <strong>Transfer Reference</strong>, dannes MobilePay-posteringer.</li>
          <li>Matcher den ikke, dannes én bankpostering. MobilePay-rækker uden et matchende bankkontoudtog danner ikke en postering.</li>
        </ol>
        <p>
          Ved en ny import opdateres kun kildedata på eksisterende afledte posteringer.
          Konto, posteringsgruppe, bruger og dokumentation bevares, så kategorisering ikke går tabt.
        </p>
      </section>
      */}

      <section className="finance-import-introduction">
        <p className="finance-live-kicker">S&aring;dan dannes posteringer</p>
        <h2>Tre trin</h2>
        <ol>
          <li>Upload CSV for bank.</li>
          <li>Upload CSV for MobilePay.</li>
          <li>V&aelig;lg, om posteringer skal dannes efter importen. Det kan ogs&aring; g&oslash;res senere.</li>
        </ol>
        <p>
          Et MobilePay-match erstatter den tilsvarende bankpostering. Ved en ny k&oslash;rsel
          bevares konto, posteringsgruppe, bruger og dokumentation.
        </p>
      </section>

      <form className="finance-import-form" onSubmit={submit}>
        <section className="finance-import-upload-grid">
          <article className="finance-import-file-card">
            <p className="finance-live-kicker">Bankkontoudtog</p>
            <h2>Bank CSV</h2>
            <p className="finance-import-fields-label">Påkrævede kolonner</p>
            <ul className="finance-import-field-list">
              <li>Dato</li>
              <li>Tekst</li>
              <li>Beløb</li>
              <li>Saldo</li>
            </ul>
            <input
              ref={bankInputRef}
              className="finance-import-file-input"
              type="file"
              accept=".csv,text/csv"
              onChange={(event) => {
                const file = event.target.files?.[0] ?? null;
                setBankFile(file);
                validateSelectedFile("bank", file);
              }}
            />
            <div className="finance-import-card-actions">
              <button
                className="profile-button finance-import-file-button"
                type="button"
                onClick={() => bankInputRef.current?.click()}
              >
                Vælg bank CSV
              </button>
              <a className="finance-import-example-link" href="/api/finance/admin/import/templates/bank">
                CSV-eksempel
              </a>
            </div>
            <p className="finance-import-file-name">
              {displayFileLabel(bankFile, "Ingen bankfil valgt")}
            </p>
            {fileValidation.bank.status === "pending" && (
              <p className="finance-import-file-validation" aria-live="polite">Validerer CSV-filen...</p>
            )}
            {fileValidation.bank.status === "invalid" && (
              <p className="finance-import-file-validation finance-import-file-validation-error" role="alert">
                {fileValidation.bank.error}
              </p>
            )}
          </article>

          <article className="finance-import-file-card">
            <p className="finance-live-kicker">MobilePay-transaktioner</p>
            <h2>MobilePay CSV</h2>
            <p className="finance-import-fields-label">Påkrævede kolonner</p>
            <ul className="finance-import-field-list">
              <li>Date</li>
              <li>Timestamp</li>
              <li>Amount</li>
              <li>Message (m&aring; gerne v&aelig;re tom)</li>
              <li>Transaction Type</li>
              <li>Transfer Reference</li>
              <li>Transfer Date</li>
              <li>Payment Transaction ID</li>
              <li>User Name</li>
            </ul>
            <input
              ref={mobilePayInputRef}
              className="finance-import-file-input"
              type="file"
              accept=".csv,text/csv"
              onChange={(event) => {
                const file = event.target.files?.[0] ?? null;
                setMobilePayFile(file);
                validateSelectedFile("mobilePay", file);
              }}
            />
            <div className="finance-import-card-actions">
              <button
                className="profile-button finance-import-file-button"
                type="button"
                onClick={() => mobilePayInputRef.current?.click()}
              >
                Vælg MobilePay CSV
              </button>
              <a className="finance-import-example-link" href="/api/finance/admin/import/templates/mobilepay">
                CSV-eksempel
              </a>
            </div>
            <p className="finance-import-file-name">
              {displayFileLabel(mobilePayFile, "Ingen MobilePay-fil valgt")}
            </p>
            {fileValidation.mobilePay.status === "pending" && (
              <p className="finance-import-file-validation" aria-live="polite">Validerer CSV-filen...</p>
            )}
            {fileValidation.mobilePay.status === "invalid" && (
              <p className="finance-import-file-validation finance-import-file-validation-error" role="alert">
                {fileValidation.mobilePay.error}
              </p>
            )}
          </article>

          <aside className="finance-import-submit-card">
            <p className="finance-live-kicker">Trin 3</p>
            <h2>Importér data</h2>
            <label className="finance-import-sync-option">
              <input
                type="checkbox"
                checked={syncPostings}
                onChange={(event) => setSyncPostings(event.target.checked)}
              />
              <span>
                <strong>Opdatér afledte posteringer</strong>
                <small>Et MobilePay-match erstatter den tilsvarende bankpostering.</small>
              </span>
            </label>
            <button className="profile-button" type="submit" disabled={isImporting}>
              {isImporting ? "Importerer…" : "Importér data"}
            </button>
            <button
              className="frontpage-button finance-import-generate-button"
              type="button"
              onClick={generatePostings}
              disabled={isGeneratingPostings || isImporting}
            >
              {isGeneratingPostings ? "Opretter posteringer..." : "Opret posteringer fra eksisterende data"}
            </button>
            <p className="finance-import-existing-help">
              Brug denne, hvis kildedata allerede er importeret.
            </p>
            <p className="finance-import-help">Maks. 10 MB pr. CSV-fil.</p>
          </aside>
        </section>
      </form>

      {error && <p className="status-message status-message-error">{error}</p>}
      {postingSyncResult && (
        <p className="status-message status-message-success">
          {postingSyncResult.created} posteringer oprettet.
        </p>
      )}

      {result && (
        <section className="finance-import-result" aria-live="polite">
          <p className="finance-live-kicker">Import gennemført</p>
          <h2>Resultat</h2>
          <div className="finance-import-result-grid">
            <div>
              <strong>Bankkontoudtog</strong>
              <span>{result.bank.rowsProcessed} behandlet · {result.bank.rowsInserted} nye · {result.bank.rowsUpdated} opdaterede</span>
            </div>
            <div>
              <strong>MobilePay-transaktioner</strong>
              <span>{result.mobilePay.rowsProcessed} behandlet · {result.mobilePay.rowsInserted} nye · {result.mobilePay.rowsUpdated} opdaterede</span>
            </div>
            {syncPostings && (
              <div>
                <strong>Afledte posteringer</strong>
                <span>{result.postings.created} posteringer oprettet.</span>
              </div>
            )}
          </div>
        </section>
      )}

      <section id="importhistorik" className="finance-import-history">
        <div className="finance-import-history-heading">
          <div>
            <p className="finance-live-kicker">Importhistorik</p>
            <h2>Seneste importer</h2>
          </div>
          <button className="finance-admin-row-more" type="button" onClick={loadHistory}>
            Opdatér
          </button>
        </div>
        <div className="finance-admin-table-controls">
          <label className="menu-table-page-size">
            <span>Vis</span>
            <select
              value={historyPageSize}
              onChange={(event) => setHistoryPageSize(Number(event.target.value))}
            >
              {[10, 25, 50, 100].map((size) => (
                <option value={size} key={size}>{size}</option>
              ))}
            </select>
            <span>pr. side</span>
          </label>
        </div>
        <div className="finance-live-table-scroll">
          <table className="finance-live-table finance-admin-source-table">
            <thead>
              <tr>
                <SortableHeader label="Kilde" sortKey="importType" sort={historySort} setSort={setHistorySort} />
                <SortableHeader label="Fil" sortKey="fileName" sort={historySort} setSort={setHistorySort} />
                <SortableHeader label="Importeret" sortKey="importedAt" sort={historySort} setSort={setHistorySort} />
                <SortableHeader label="Behandlet" sortKey="rowsProcessed" sort={historySort} setSort={setHistorySort} />
                <SortableHeader label="Nye" sortKey="rowsInserted" sort={historySort} setSort={setHistorySort} />
                <SortableHeader label="Opdaterede" sortKey="rowsUpdated" sort={historySort} setSort={setHistorySort} />
                <SortableHeader label="Status" sortKey="status" sort={historySort} setSort={setHistorySort} />
              </tr>
            </thead>
            <tbody>
              {visibleHistory.map((item) => (
                <tr key={item.id ?? `${item.importType}-${item.importedAt}-${item.fileName}`}>
                  <td>{item.importType === "bank_csv" ? "Bank" : "MobilePay"}</td>
                  <td>{item.fileName || "—"}</td>
                  <td>{formatImportedAt(item.importedAt)}</td>
                  <td>{item.rowsProcessed}</td>
                  <td>{item.rowsInserted}</td>
                  <td>{item.rowsUpdated}</td>
                  <td><span className="finance-import-status">{item.status}</span></td>
                </tr>
              ))}
              {visibleHistory.length === 0 && (
                <tr><td colSpan="7" className="finance-admin-empty">Ingen importer endnu.</td></tr>
              )}
            </tbody>
          </table>
        </div>
        <Pagination
          page={currentHistoryPage}
          pageCount={historyPageCount}
          total={history.length}
          pageSize={historyPageSize}
          setPage={setHistoryPage}
        />
      </section>
    </AdminLayout>
  );
}
