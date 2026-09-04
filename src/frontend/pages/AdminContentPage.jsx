import { useEffect, useMemo, useRef, useState } from "react";
import { Eye, Plus, Save, Trash2 } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { Link, navigate } from "../routes/navigation.jsx";
import { contentApi } from "../services/api.js";
import { detailPath, emptyContent, formatDate } from "../utils/format.js";
import { sanitizeHtml } from "../utils/richText.js";
import { SortableHeader } from "./MembersPage.jsx";

/**
 * Admin overview for event and news content.
 */
export function AdminContentPage({ type, isAdmin }) {
  const [items, setItems] = useState([]);
  const [sort, setSort] = useState({ key: "date", direction: "desc" });

  useEffect(() => {
    load();
  }, [type]);

  async function load() {
    const result = await contentApi.listAdmin(type);
    setItems(result.map((item) => normalizeContentItem(item, type)));
  }

  const sortedItems = useMemo(() => {
    return [...items].sort((left, right) => {
      const result = compareContentValues(contentSortValue(left, sort.key), contentSortValue(right, sort.key));
      return sort.direction === "asc" ? result : -result;
    });
  }, [items, sort]);

  return (
    <AdminLayout active={`/react/admin/${type === "EVENT" ? "events" : "news"}`} canWrite={isAdmin}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">{type === "EVENT" ? "Begivenheder" : "Nyheder"}</p>
        </div>
        {isAdmin && (
          <Link className="menu-create-button" href={`/react/admin/${type === "EVENT" ? "events" : "news"}/new`}>
            <Plus size={16} />
            Opret ny
          </Link>
        )}
      </div>

      <div className="menu-table-wrap admin-content-list">
          {!isAdmin && <p className="status-message status-message-warning">Du har kun læseadgang til admin-overblikket.</p>}
          <table className="menu-member-table">
            <thead>
              <tr>
                <SortableHeader label="Titel" sortKey="title" sort={sort} setSort={setSort} />
                <SortableHeader label="Status" sortKey="status" sort={sort} setSort={setSort} />
                <th>Forside</th>
                <SortableHeader label="Dato" sortKey="date" sort={sort} setSort={setSort} />
                <th></th>
              </tr>
            </thead>
            <tbody>
              {sortedItems.map((item) => (
                <tr key={item.id}>
                  <td>{item.title}</td>
                  <td><span className="tag">{item.status}</span></td>
                  <td>{isShownOnFrontPage(item) ? "Ja" : "Nej"}</td>
                  <td>{formatDate(item.startDate ?? item.publishedAt)}</td>
                  <td className="table-actions">
                    {isAdmin && <Link className="admin-table-button" href={`/react/admin/${type === "EVENT" ? "events" : "news"}/${item.id}/edit`}>Rediger</Link>}
                    <Link className="admin-table-button" href={detailPath(item)}><Eye size={14} /> Se</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
      </div>
    </AdminLayout>
  );

}

/**
 * Admin editor for creating and updating event or news content.
 */
export function AdminContentEditorPage({ type, isAdmin, itemId }) {
  const [selected, setSelected] = useState(emptyContent(type));
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const basePath = `/react/admin/${type === "EVENT" ? "events" : "news"}`;
  const label = type === "EVENT" ? "Begivenheder" : "Nyheder";

  useEffect(() => {
    if (!itemId) {
      setSelected(emptyContent(type));
      return;
    }
    contentApi.listAdmin(type).then((items) => {
      const match = items.find((item) => item.id === itemId);
      if (match) setSelected(normalizeContentItem(match, type));
    }).catch((reason) => setError(reason.message));
  }, [type, itemId]);

  async function save(event) {
    event.preventDefault();
    setError("");
    try {
      const payload = buildSavePayload(selected, type);
      const result = selected.id ? await contentApi.update(selected.id, payload) : await contentApi.create(payload);
      setSelected(normalizeContentItem(result, type));
      setMessage("Gemt");
    } catch (reason) {
      setError(reason.message);
    }
  }

  async function remove() {
    if (!selected.id) {
      navigate(basePath);
      return;
    }
    try {
      await contentApi.delete(selected.id);
      navigate(basePath);
    } catch (reason) {
      setError(reason.message);
    }
  }

  if (!isAdmin) {
    return <AdminLayout active={basePath} canWrite={false}><p className="status-message status-message-warning">Kun ADMIN kan redigere indhold.</p></AdminLayout>;
  }

  return (
    <AdminLayout active={basePath} canWrite={isAdmin}>
      <div className="menu-panel-header">
        <div><p className="menu-section-title">{label}</p><h1>{selected.id ? "Rediger" : "Opret ny"} {type === "EVENT" ? "begivenhed" : "nyhed"}</h1></div>
        <Link className="frontpage-button frontpage-button-secondary" href={basePath}>Tilbage til oversigt</Link>
      </div>
      <form className="menu-editor-form admin-content-editor" onSubmit={save}>
        <label className="admin-field"><span>Titel</span><input value={selected.title ?? ""} onChange={(event) => update("title", event.target.value)} required /></label>
        <div className="menu-editor-grid">
          <label className="admin-field">
            <span className="admin-field-heading">
              Slug
              <span className="admin-info-tooltip" tabIndex="0" aria-label="Forklaring af slug">
                ?
                <span className="admin-info-tooltip-text" role="tooltip">
                  Slug er enden på sidens URL. Brug små bogstaver, bindestreger og ingen mellemrum, fx faglig-aften.
                </span>
              </span>
            </span>
            <input value={selected.slug ?? ""} onChange={(event) => update("slug", event.target.value)} required />
          </label>
          <label className="admin-field"><span>Status</span><select value={selected.status ?? "DRAFT"} onChange={(event) => update("status", event.target.value)}><option value="DRAFT">DRAFT</option><option value="PUBLISHED">PUBLISHED</option><option value="ARCHIVED">ARCHIVED</option></select></label>
        </div>
        <label className="account-checkbox">
          <input type="checkbox" checked={isShownOnFrontPage(selected)} onChange={(event) => update("showOnFrontPage", event.target.checked)} />
          <span>Vis på forsiden</span>
        </label>
        {type === "NEWS" && <label className="admin-field"><span>Dato</span><input type="datetime-local" value={toLocalInput(selected.publishedAt)} onChange={(event) => update("publishedAt", event.target.value)} /></label>}
        <label className="admin-field"><span>Summary</span><input value={selected.summary ?? ""} onChange={(event) => update("summary", event.target.value)} /></label>
        <RichTextEditor value={selected.body ?? ""} onChange={(value) => update("body", value)} />
        <div className="menu-editor-grid">
          <label className="admin-field"><span>Billede URL</span><input value={selected.pictureUrl ?? ""} onChange={(event) => update("pictureUrl", event.target.value)} /></label>
          <label className="admin-field"><span>Tags</span><input value={selected.tags ?? ""} onChange={(event) => update("tags", event.target.value)} placeholder="event,karriere" /></label>
        </div>
        {type === "EVENT" && <div className="menu-editor-grid"><label className="admin-field"><span>Sted</span><input value={selected.location ?? ""} onChange={(event) => update("location", event.target.value)} /></label><label className="admin-field"><span>Start</span><input type="datetime-local" value={toLocalInput(selected.startDate)} onChange={(event) => update("startDate", event.target.value)} /></label><label className="admin-field"><span>Slut</span><input type="datetime-local" value={toLocalInput(selected.endDate)} onChange={(event) => update("endDate", event.target.value)} /></label></div>}
        <LinkEditor links={selected.links ?? []} onChange={(links) => update("links", links)} />
        <div className="menu-editor-actions"><button className="profile-button" type="submit"><Save size={16} /> Gem</button><button className="profile-button profile-button-danger" type="button" onClick={remove}><Trash2 size={16} /> Slet</button></div>
        {message && <p className="status-message status-message-success">{message}</p>}
        {error && <p className="status-message status-message-error">{error}</p>}
      </form>
    </AdminLayout>
  );

  function update(field, value) { setSelected((current) => ({ ...current, [field]: value })); }
}

/**
 * Minimal rich text editor wrapper used by content body fields.
 */
function RichTextEditor({ value, onChange }) {
  const editorRef = useRef(null);
  const lastEditorValue = useRef(null);
  const [sourceMode, setSourceMode] = useState(false);

  useEffect(() => {
    if (!sourceMode && editorRef.current && lastEditorValue.current !== value) {
      editorRef.current.innerHTML = sanitizeHtml(value);
      lastEditorValue.current = value;
    }
  }, [value, sourceMode]);

  function exec(command, commandValue = null) {
    editorRef.current?.focus();
    document.execCommand(command, false, commandValue);
    updateValueFromEditor();
  }

  function updateValueFromEditor() {
    const nextValue = editorRef.current?.innerHTML ?? "";
    lastEditorValue.current = nextValue;
    onChange(nextValue);
  }

  function addLink() {
    const url = window.prompt("Link URL");
    if (url) exec("createLink", url);
  }

  function toggleSource() {
    if (!sourceMode) {
      updateValueFromEditor();
    } else {
      // The visual editor is remounted after source mode and must be initialized again.
      lastEditorValue.current = null;
    }
    setSourceMode((current) => !current);
  }

  return (
    <div className="admin-rich-editor">
      <span className="admin-field-label">Tekst</span>
      <div className="admin-rich-toolbar" role="toolbar" aria-label="Tekstformatering">
        <button type="button" onClick={() => exec("bold")} aria-label="Fed">B</button>
        <button type="button" onClick={() => exec("italic")} aria-label="Kursiv"><em>I</em></button>
        <button type="button" onClick={() => exec("underline")} aria-label="Understregning"><u>U</u></button>
        <button type="button" onClick={() => exec("formatBlock", "H2")} aria-label="Overskrift">H</button>
        <button type="button" onClick={() => exec("insertUnorderedList")} aria-label="Punktliste">&#8226;&#8226;&#8226;</button>
        <button type="button" onClick={() => exec("insertOrderedList")} aria-label="Nummereret liste">1.2.3.</button>
        <button type="button" onClick={addLink} aria-label="Indsæt link">Link</button>
        <button type="button" className={sourceMode ? "is-active" : ""} onClick={toggleSource} aria-label="Vis HTML">&lt;/&gt;</button>
      </div>
      {sourceMode ? (
        <textarea className="admin-rich-source" value={value} onChange={(event) => onChange(event.target.value)} aria-label="HTML-kilde" />
      ) : (
        <div ref={editorRef} className="admin-rich-input" contentEditable suppressContentEditableWarning onInput={updateValueFromEditor} />
      )}
    </div>
  );
}

/**
 * Editor for related content links such as payment, calendar, or external URLs.
 */
function LinkEditor({ links, onChange }) {
  function update(index, field, value) {
    onChange(links.map((link, currentIndex) => currentIndex === index ? { ...link, [field]: value } : link));
  }

  return (
    <div className="menu-editor-link-row">
      {links.map((link, index) => (
        <div className="menu-editor-grid" key={index}>
          <label className="admin-field">
            <span>Link label</span>
            <input value={link.label ?? ""} onChange={(event) => update(index, "label", event.target.value)} />
          </label>
          <label className="admin-field">
            <span>URL</span>
            <input value={link.url ?? ""} onChange={(event) => update(index, "url", event.target.value)} />
          </label>
          <label className="admin-field">
            <span>Type</span>
            <select value={link.type ?? "OTHER"} onChange={(event) => update(index, "type", event.target.value)}>
              <option value="FACEBOOK">FACEBOOK</option>
              <option value="INSTAGRAM">INSTAGRAM</option>
              <option value="LINKEDIN">LINKEDIN</option>
              <option value="PAYMENT">PAYMENT</option>
              <option value="OTHER">OTHER</option>
            </select>
          </label>
        </div>
      ))}
      <button className="menu-inline-add-button" type="button" onClick={() => onChange([...links, { label: "", url: "", type: "OTHER", sortOrder: links.length }])}>
        <Plus size={16} />
        Tilføj link
      </button>
    </div>
  );
}

function normalizeLinks(links) {
  return (links ?? [])
    .filter((link) => link.label && link.url)
    .map((link, index) => ({ ...link, sortOrder: index }));
}

function normalizeContentItem(item, type) {
  return {
    ...item,
    type: item.type ?? item.Type ?? type,
    showOnFrontPage: item.showOnFrontPage ?? item.ShowOnFrontPage ?? true
  };
}

function isShownOnFrontPage(item) {
  return item.showOnFrontPage ?? item.ShowOnFrontPage ?? true;
}

function buildSavePayload(item, type) {
  return {
    title: item.title ?? "",
    slug: item.slug ?? "",
    summary: item.summary ?? "",
    body: item.body ?? "",
    pictureUrl: item.pictureUrl ?? "",
    tags: item.tags ?? "",
    type,
    status: item.status ?? "DRAFT",
    showOnFrontPage: isShownOnFrontPage(item),
    startDate: item.startDate || null,
    endDate: item.endDate || null,
    location: item.location ?? "",
    publishedAt: item.publishedAt || null,
    links: normalizeLinks(item.links)
  };
}

function toLocalInput(value) {
  if (!value) {
    return "";
  }

  return value.slice(0, 16);
}

function contentSortValue(item, key) {
  if (key === "date") {
    return item.startDate ?? item.publishedAt;
  }

  return item[key];
}

function compareContentValues(left, right) {
  const leftDate = Date.parse(left);
  const rightDate = Date.parse(right);

  if (!Number.isNaN(leftDate) || !Number.isNaN(rightDate)) {
    return (Number.isNaN(leftDate) ? 0 : leftDate) - (Number.isNaN(rightDate) ? 0 : rightDate);
  }

  return String(left ?? "").localeCompare(String(right ?? ""), "da-DK", { sensitivity: "base" });
}
