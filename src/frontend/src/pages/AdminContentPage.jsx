import { useEffect, useState } from "react";
import { Eye, Plus, Save, Trash2 } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { contentApi } from "../services/api.js";
import { detailPath, emptyContent, formatDate } from "../utils/format.js";

export function AdminContentPage({ type, isAdmin, selectedId }) {
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState(emptyContent(type));
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (selectedId) {
      load(selectedId);
      return;
    }

    setSelected(emptyContent(type));
    load();
  }, [type, selectedId]);

  async function load(nextSelectedId) {
    const result = await contentApi.listAdmin(type);
    setItems(result);
    if (nextSelectedId) {
      const match = result.find((item) => item.id === nextSelectedId);
      if (match) {
        setSelected(match);
      }
    }
  }

  async function save(event) {
    event.preventDefault();
    const payload = {
      ...selected,
      type,
      startDate: selected.startDate || null,
      endDate: selected.endDate || null,
      links: normalizeLinks(selected.links)
    };

    const result = selected.id
      ? await contentApi.update(selected.id, payload)
      : await contentApi.create(payload);

    setSelected(result);
    setMessage("Gemt");
    await load();
  }

  async function remove() {
    if (!selected.id) {
      setSelected(emptyContent(type));
      return;
    }

    await contentApi.delete(selected.id);
    setSelected(emptyContent(type));
    setMessage("Slettet");
    await load();
  }

  return (
    <AdminLayout active={`/react/admin/${type === "EVENT" ? "events" : "news"}`} canWrite={isAdmin}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">{type === "EVENT" ? "Begivenheder" : "Nyheder"}</p>
          <h1>{isAdmin ? `Opret / rediger ${type === "EVENT" ? "event" : "nyhed"}` : `${type === "EVENT" ? "Event" : "Nyheds"}-overblik`}</h1>
        </div>
        {isAdmin && (
          <button className="menu-create-button" type="button" onClick={() => setSelected(emptyContent(type))}>
            <Plus size={16} />
            Opret ny
          </button>
        )}
      </div>

      <div className="admin-split">
        {isAdmin && (
          <form className="menu-editor-form" onSubmit={save}>
            {message && <p className="status-message">{message}</p>}
            <label className="admin-field">
              <span>Titel</span>
              <input value={selected.title ?? ""} onChange={(event) => update("title", event.target.value)} required />
            </label>
            <div className="menu-editor-grid">
              <label className="admin-field">
                <span>Slug</span>
                <input value={selected.slug ?? ""} onChange={(event) => update("slug", event.target.value)} required />
              </label>
              <label className="admin-field">
                <span>Status</span>
                <select value={selected.status ?? "DRAFT"} onChange={(event) => update("status", event.target.value)}>
                  <option value="DRAFT">DRAFT</option>
                  <option value="PUBLISHED">PUBLISHED</option>
                  <option value="ARCHIVED">ARCHIVED</option>
                </select>
              </label>
            </div>
            <label className="admin-field">
              <span>Summary</span>
              <input value={selected.summary ?? ""} onChange={(event) => update("summary", event.target.value)} />
            </label>
            <label className="admin-field">
              <span>Tekst</span>
              <textarea value={selected.body ?? ""} onChange={(event) => update("body", event.target.value)} />
            </label>
            <div className="menu-editor-grid">
              <label className="admin-field">
                <span>Billede URL</span>
                <input value={selected.pictureUrl ?? ""} onChange={(event) => update("pictureUrl", event.target.value)} />
              </label>
              <label className="admin-field">
                <span>Tags</span>
                <input value={selected.tags ?? ""} onChange={(event) => update("tags", event.target.value)} placeholder="event,karriere" />
              </label>
            </div>
            {type === "EVENT" && (
              <div className="menu-editor-grid">
                <label className="admin-field">
                  <span>Sted</span>
                  <input value={selected.location ?? ""} onChange={(event) => update("location", event.target.value)} />
                </label>
                <label className="admin-field">
                  <span>Start</span>
                  <input type="datetime-local" value={toLocalInput(selected.startDate)} onChange={(event) => update("startDate", event.target.value)} />
                </label>
                <label className="admin-field">
                  <span>Slut</span>
                  <input type="datetime-local" value={toLocalInput(selected.endDate)} onChange={(event) => update("endDate", event.target.value)} />
                </label>
              </div>
            )}
            <LinkEditor links={selected.links ?? []} onChange={(links) => update("links", links)} />
            <div className="menu-editor-actions">
              <button className="profile-button" type="submit"><Save size={16} /> Gem</button>
              <button className="profile-button profile-button-danger" type="button" onClick={remove}><Trash2 size={16} /> Slet</button>
            </div>
          </form>
        )}

        <div className="menu-table-wrap">
          {!isAdmin && <p className="status-message">Du har kun læseadgang til admin-overblikket.</p>}
          <table className="menu-member-table">
            <thead>
              <tr>
                <th>Titel</th>
                <th>Status</th>
                <th>Dato</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id}>
                  <td>{item.title}</td>
                  <td><span className="tag">{item.status}</span></td>
                  <td>{formatDate(item.startDate ?? item.publishedAt)}</td>
                  <td className="table-actions">
                    {isAdmin && <button className="admin-table-button" type="button" onClick={() => setSelected(item)}>Rediger</button>}
                    <Link className="admin-table-button" href={detailPath(item)}><Eye size={14} /> Se</Link>
                    {item.type === "EVENT" && <Link className="admin-table-button" href={`/react/admin/events/${item.id}/registrations`}>Tilmeldte</Link>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </AdminLayout>
  );

  function update(field, value) {
    setSelected((current) => ({ ...current, [field]: value }));
  }
}

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

function toLocalInput(value) {
  if (!value) {
    return "";
  }

  return value.slice(0, 16);
}
