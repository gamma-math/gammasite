import { useState } from "react";
import { formatDate } from "../utils/format.js";

/**
 * Reusable searchable multi-select dropdown for admin filters and pickers.
 */
export function MultiSelectDropdown({
  label,
  placeholder,
  items = [],
  selectedIds = [],
  onChange,
  showMeta = true,
  searchable = false,
}) {
  const [search, setSearch] = useState("");
  const selectedItems = items.filter((item) => selectedIds.includes(item.id));
  const filteredItems = items.filter((item) => {
    const term = search.trim().toLowerCase();
    if (!term) return true;
    return [item.title, item.summary, item.location]
      .some((value) => String(value ?? "").toLowerCase().includes(term));
  });
  const buttonText = selectedItems.length === 0
    ? placeholder
    : selectedItems.length === 1
      ? selectedItems[0].title
      : selectedItems.length === 2
        ? selectedItems.map((item) => item.title).join(", ")
        : `${selectedItems.slice(0, 2).map((item) => item.title).join(", ")} +${selectedItems.length - 2}`;

  function toggle(id) {
    if (id === "ALL") {
      onChange(selectedIds.includes("ALL") ? [] : ["ALL"]);
      return;
    }

    const nextIds = selectedIds.includes(id)
      ? selectedIds.filter((item) => item !== id)
      : [...selectedIds.filter((item) => item !== "ALL"), id];
    onChange(nextIds);
  }

  return (
    <details className={`admin-multi-select ${selectedItems.length > 0 ? "has-selection" : ""}`}>
      <summary>
        <span>{label}</span>
        <strong>{buttonText}</strong>
      </summary>
      {selectedItems.length > 0 && (
        <div className="admin-multi-select-chips" aria-label={`${label} valgt`}>
          {selectedItems.map((item) => (
            <button type="button" key={item.id} onClick={() => toggle(item.id)}>
              {item.title}
              <span aria-hidden="true">x</span>
            </button>
          ))}
        </div>
      )}
      <div className="admin-multi-select-menu">
        {searchable && (
          <label className="admin-multi-select-search">
            <span>Søg</span>
            <input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder={`Søg i ${label.toLowerCase()}`} />
          </label>
        )}
        {filteredItems.length === 0 && <p className="muted">Ingen elementer fundet.</p>}
        {filteredItems.map((item) => (
          <label className="profile-checkbox admin-message-content-option" key={item.id}>
            <input type="checkbox" checked={selectedIds.includes(item.id)} onChange={() => toggle(item.id)} />
            <span>{item.title}</span>
            {showMeta && <small>{formatDate(item.startDate ?? item.publishedAt)}</small>}
          </label>
        ))}
      </div>
    </details>
  );
}
