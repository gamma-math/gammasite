import { useEffect, useState } from "react";
import { Plus, Save, Trash2 } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { emailTemplatesApi } from "../services/api.js";

const emptyTemplate = {
  name: "",
  subject: "",
  preheader: "",
  htmlBody: "<p>{{ContentBlocks}}</p>",
  textBody: "{{ContentBlocks}}",
  templateType: "EVENT",
  isActive: true
};

export function AdminTemplatesPage({ isAdmin }) {
  const [templates, setTemplates] = useState([]);
  const [selected, setSelected] = useState(emptyTemplate);
  const [preview, setPreview] = useState(null);

  useEffect(() => {
    load();
  }, []);

  async function load() {
    setTemplates(await emailTemplatesApi.list());
  }

  async function save(event) {
    event.preventDefault();
    const result = selected.id
      ? await emailTemplatesApi.update(selected.id, selected)
      : await emailTemplatesApi.create(selected);
    setSelected(result);
    await load();
  }

  async function remove() {
    if (!selected.id) {
      setSelected(emptyTemplate);
      return;
    }

    await emailTemplatesApi.delete(selected.id);
    setSelected(emptyTemplate);
    setPreview(null);
    await load();
  }

  async function renderPreview() {
    if (!selected.id) {
      return;
    }

    setPreview(await emailTemplatesApi.preview(selected.id, {
      UserName: "Ada",
      EventTitle: "Karriere efter matematik",
      EventStartDate: "25. september kl. 17.00",
      EventRegisterUrl: "https://gam-ma.dk/react/events/karriere-efter-matematik",
      ProfileUrl: "https://gam-ma.dk/Identity/Account/Manage",
      ContentBlocks: "Et kort indholdsblok-preview fra React MVP."
    }));
  }

  if (!isAdmin) {
    return (
      <AdminLayout active="/react/admin/templates" canWrite={false}>
        <p className="status-message">Kun ADMIN kan redigere email templates.</p>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout active="/react/admin/templates" canWrite={isAdmin}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Beskeder</p>
          <h1>Email templates</h1>
        </div>
        <button className="menu-create-button" type="button" onClick={() => setSelected(emptyTemplate)}>
          <Plus size={16} />
          Opret template
        </button>
      </div>

      <div className="admin-split">
        <form className="admin-message-form" onSubmit={save}>
          <div className="admin-form-row admin-form-row-two">
            <label className="admin-field">
              <span>Navn</span>
              <input value={selected.name ?? ""} onChange={(event) => update("name", event.target.value)} required />
            </label>
            <label className="admin-field">
              <span>Type</span>
              <select value={selected.templateType ?? "EVENT"} onChange={(event) => update("templateType", event.target.value)}>
                <option value="NEWSLETTER">NEWSLETTER</option>
                <option value="EVENT">EVENT</option>
                <option value="SYSTEM">SYSTEM</option>
              </select>
            </label>
          </div>
          <label className="admin-field">
            <span>Emne</span>
            <input value={selected.subject ?? ""} onChange={(event) => update("subject", event.target.value)} required />
          </label>
          <label className="admin-field">
            <span>Preheader</span>
            <input value={selected.preheader ?? ""} onChange={(event) => update("preheader", event.target.value)} />
          </label>
          <div className="admin-editor-shell">
            <div className="admin-editor-toolbar" aria-label="Editor værktøjer">
              <span className="admin-tool-chip">B</span>
              <span className="admin-tool-chip">U</span>
              <span className="admin-tool-chip">&lt;/&gt;</span>
              <span className="admin-tool-chip">Link</span>
            </div>
            <label className="admin-field admin-field-editor">
              <span>HTML</span>
              <textarea value={selected.htmlBody ?? ""} onChange={(event) => update("htmlBody", event.target.value)} />
            </label>
          </div>
          <label className="admin-field">
            <span>Tekstversion</span>
            <textarea value={selected.textBody ?? ""} onChange={(event) => update("textBody", event.target.value)} />
          </label>
          <label className="profile-checkbox">
            <input type="checkbox" checked={selected.isActive ?? true} onChange={(event) => update("isActive", event.target.checked)} />
            Aktiv
          </label>
          <div className="menu-editor-actions">
            <button className="profile-button" type="submit"><Save size={16} /> Gem</button>
            <button className="admin-action-button" type="button" onClick={renderPreview}>Preview</button>
            <button className="profile-button profile-button-danger" type="button" onClick={remove}><Trash2 size={16} /> Slet</button>
          </div>
        </form>

        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar">
            <strong>Templates</strong>
          </div>
          <div className="template-list">
            {templates.map((template) => (
              <button className="template-row" type="button" onClick={() => setSelected(template)} key={template.id}>
                <strong>{template.name}</strong>
                <span>{template.templateType}</span>
              </button>
            ))}
          </div>
          {preview && (
            <div className="admin-rendered-preview">
              <h2>{preview.subject}</h2>
              <p>{preview.preheader}</p>
              <div dangerouslySetInnerHTML={{ __html: preview.htmlBody }} />
            </div>
          )}
        </section>
      </div>
    </AdminLayout>
  );

  function update(field, value) {
    setSelected((current) => ({ ...current, [field]: value }));
  }
}
