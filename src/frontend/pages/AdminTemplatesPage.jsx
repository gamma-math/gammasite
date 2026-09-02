import { useEffect, useRef, useState } from "react";
import { Eye, Plus, Save, Trash2, X } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { Link, navigate } from "../routes/navigation.jsx";
import { emailTemplatesApi } from "../services/api.js";
import { formatDate } from "../utils/format.js";

const defaultBlockDesign = {
  eventColor: "#1f78c1",
  newsColor: "#1f78c1"
};

const emptyTemplate = {
  name: "",
  subject: "",
  preheader: "",
  htmlBody: `${blockDesignComment(defaultBlockDesign)}<p>{{ContentBlocks}}</p>`,
  textBody: "{{ContentBlocks}}",
  templateType: "EVENT",
  isActive: true
};

export function AdminTemplatesPage({ isAdmin }) {
  const [templates, setTemplates] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    if (isAdmin) {
      load();
    }
  }, [isAdmin]);

  async function load() {
    try {
      setTemplates(await emailTemplatesApi.list());
    } catch (reason) {
      setError(reason.message);
    }
  }

  async function remove(template) {
    try {
      await emailTemplatesApi.delete(template.id);
      await load();
    } catch (reason) {
      setError(reason.message);
    }
  }

  if (!isAdmin) {
    return (
      <AdminLayout active="/react/admin/templates" canWrite={false}>
        <p className="status-message status-message-warning">Kun ADMIN kan redigere email templates.</p>
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
        <Link className="menu-create-button" href="/react/admin/templates/new">
          <Plus size={16} />
          Opret ny
        </Link>
      </div>

      {error && <p className="status-message status-message-error">{error}</p>}
      <div className="menu-table-wrap">
        <table className="menu-member-table admin-template-table">
          <thead>
            <tr>
              <th>Navn</th>
              <th>Type</th>
              <th>Status</th>
              <th>Opdateret</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {templates.map((template) => (
              <tr key={template.id}>
                <td>{template.name}</td>
                <td>{template.templateType}</td>
                <td><span className="tag">{template.isActive ? "Aktiv" : "Inaktiv"}</span></td>
                <td>{formatDate(template.updated)}</td>
                <td className="table-actions">
                  <Link className="admin-table-button" href={`/react/admin/templates/${template.id}/edit`}>Rediger</Link>
                  <button className="admin-table-button admin-table-button-danger" type="button" onClick={() => remove(template)}>
                    <Trash2 size={14} />
                    Slet
                  </button>
                </td>
              </tr>
            ))}
            {templates.length === 0 && (
              <tr>
                <td colSpan="5">Ingen templates endnu.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
}

export function AdminTemplateEditorPage({ isAdmin, templateId }) {
  const [selected, setSelected] = useState(emptyTemplate);
  const [preview, setPreview] = useState(null);
  const [showPreview, setShowPreview] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const isNew = templateId === null;

  useEffect(() => {
    if (!isAdmin) return;

    if (isNew) {
      setSelected(emptyTemplate);
      setPreview(null);
      setShowPreview(false);
      return;
    }

    emailTemplatesApi.list()
      .then((templates) => {
        const match = templates.find((template) => template.id === templateId);
        if (match) {
          setSelected(match);
        } else {
          setError("Templaten blev ikke fundet.");
        }
      })
      .catch((reason) => setError(reason.message));
  }, [isAdmin, isNew, templateId]);

  async function save(event) {
    event.preventDefault();
    setError("");
    setMessage("");

    try {
      const result = selected.id
        ? await emailTemplatesApi.update(selected.id, selected)
        : await emailTemplatesApi.create(selected);
      setSelected(result);
      setMessage("Templaten er gemt.");
      if (!selected.id) {
        navigate(`/react/admin/templates/${result.id}/edit`);
      }
    } catch (reason) {
      setError(reason.message);
    }
  }

  async function remove() {
    if (!selected.id) {
      navigate("/react/admin/templates");
      return;
    }

    try {
      await emailTemplatesApi.delete(selected.id);
      navigate("/react/admin/templates");
    } catch (reason) {
      setError(reason.message);
    }
  }

  async function renderPreview() {
    setError("");
    setPreview({
      subject: selected.subject,
      preheader: selected.preheader,
      htmlBody: selected.htmlBody
    });
    setShowPreview(true);
  }

  function update(field, value) {
    setSelected((current) => ({ ...current, [field]: value }));
  }

  const blockDesign = parseBlockDesign(selected.htmlBody);

  function updateBlockDesign(field, value) {
    update("htmlBody", writeBlockDesign(selected.htmlBody, { ...blockDesign, [field]: value }));
  }

  if (!isAdmin) {
    return (
      <AdminLayout active="/react/admin/templates" canWrite={false}>
        <p className="status-message status-message-warning">Kun ADMIN kan redigere email templates.</p>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout active="/react/admin/templates" canWrite={isAdmin}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Beskeder</p>
          <h1>{isNew ? "Opret ny template" : "Rediger template"}</h1>
        </div>
        <Link className="frontpage-button frontpage-button-secondary" href="/react/admin/templates">Tilbage til oversigt</Link>
      </div>

      {message && <p className="status-message status-message-success">{message}</p>}
      {error && <p className="status-message status-message-error">{error}</p>}

      <form className="menu-editor-form admin-template-editor" onSubmit={save}>
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
        <div className="admin-form-row admin-form-row-two">
          <label className="admin-field admin-color-field">
            <span>Event farve</span>
            <input type="color" value={blockDesign.eventColor} onChange={(event) => updateBlockDesign("eventColor", event.target.value)} />
          </label>
          <label className="admin-field admin-color-field">
            <span>Nyhed farve</span>
            <input type="color" value={blockDesign.newsColor} onChange={(event) => updateBlockDesign("newsColor", event.target.value)} />
          </label>
        </div>
        <TemplateRichTextEditor value={selected.htmlBody ?? ""} onChange={(value) => update("htmlBody", value)} />
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
          <button className="admin-action-button" type="button" onClick={renderPreview}><Eye size={16} /> Preview</button>
          <button className="profile-button profile-button-danger" type="button" onClick={remove}><Trash2 size={16} /> Slet</button>
        </div>
      </form>

      {showPreview && preview && (
        <div className="admin-preview-modal-backdrop" role="presentation" onClick={() => setShowPreview(false)}>
          <section className="admin-preview-modal" role="dialog" aria-modal="true" aria-label="Template preview" onClick={(event) => event.stopPropagation()}>
            <div className="admin-preview-modal-header">
              <div>
                <p className="menu-section-title">Preview</p>
                <h2>{preview.subject}</h2>
                {preview.preheader && <p className="muted">{preview.preheader}</p>}
              </div>
              <button className="admin-preview-close" type="button" aria-label="Luk preview" onClick={() => setShowPreview(false)}>
                <X size={20} />
              </button>
            </div>
            <iframe className="admin-template-preview-frame" title="Template preview" srcDoc={preview.htmlBody || "<p>Ingen preview endnu.</p>"} />
          </section>
        </div>
      )}
    </AdminLayout>
  );
}

const blockDesignRegex = /<!--\s*GammaEmailBlockDesign\s+eventColor="([^"]*)"\s+newsColor="([^"]*)"\s*-->/i;

function blockDesignComment(design) {
  return `<!-- GammaEmailBlockDesign eventColor="${design.eventColor}" newsColor="${design.newsColor}" -->`;
}

function parseBlockDesign(html) {
  const match = String(html ?? "").match(blockDesignRegex);
  return {
    eventColor: normalizeColor(match?.[1], defaultBlockDesign.eventColor),
    newsColor: normalizeColor(match?.[2], defaultBlockDesign.newsColor)
  };
}

function writeBlockDesign(html, design) {
  const nextDesign = {
    eventColor: normalizeColor(design.eventColor, defaultBlockDesign.eventColor),
    newsColor: normalizeColor(design.newsColor, defaultBlockDesign.newsColor)
  };
  const comment = blockDesignComment(nextDesign);
  const currentHtml = String(html ?? "");
  return blockDesignRegex.test(currentHtml)
    ? currentHtml.replace(blockDesignRegex, comment)
    : `${comment}${currentHtml}`;
}

function normalizeColor(value, fallback) {
  return /^#[0-9a-f]{6}$/i.test(value ?? "") ? value : fallback;
}

function TemplateRichTextEditor({ value, onChange }) {
  const editorRef = useRef(null);
  const sourceRef = useRef(null);
  const lastEditorValue = useRef(null);
  const [sourceMode, setSourceMode] = useState(false);

  useEffect(() => {
    if (!sourceMode && editorRef.current && lastEditorValue.current !== value) {
      editorRef.current.innerHTML = value ?? "";
      lastEditorValue.current = value ?? "";
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

  function insertSnippet(snippet) {
    if (sourceMode) {
      const source = sourceRef.current;
      const currentValue = value ?? "";
      const start = source?.selectionStart ?? currentValue.length;
      const end = source?.selectionEnd ?? currentValue.length;
      const nextValue = `${currentValue.slice(0, start)}${snippet}${currentValue.slice(end)}`;
      onChange(nextValue);
      window.requestAnimationFrame(() => {
        source?.focus();
        source?.setSelectionRange(start + snippet.length, start + snippet.length);
      });
      return;
    }

    exec("insertText", snippet);
  }

  function toggleSource() {
    if (!sourceMode) {
      updateValueFromEditor();
    } else {
      lastEditorValue.current = null;
    }
    setSourceMode((current) => !current);
  }

  return (
    <div className="admin-rich-editor admin-template-rich-editor">
      <span className="admin-field-label">Indhold</span>
      <div className="admin-rich-toolbar" role="toolbar" aria-label="Template formatering">
        <button type="button" onClick={() => exec("bold")} aria-label="Fed">B</button>
        <button type="button" onClick={() => exec("italic")} aria-label="Kursiv"><em>I</em></button>
        <button type="button" onClick={() => exec("underline")} aria-label="Understregning"><u>U</u></button>
        <button type="button" onClick={() => exec("formatBlock", "H2")} aria-label="Overskrift">H</button>
        <button type="button" onClick={() => exec("insertUnorderedList")} aria-label="Punktliste">&#8226;&#8226;&#8226;</button>
        <button type="button" onClick={() => exec("insertOrderedList")} aria-label="Nummereret liste">1.2.3.</button>
        <button type="button" onClick={addLink} aria-label="Indsæt link">Link</button>
        <button type="button" onClick={() => insertSnippet("{{ContentBlocks}}")}>{"{{ContentBlocks}}"}</button>
        <button type="button" onClick={() => insertSnippet("{{EventBlocks}}")}>{"{{EventBlocks}}"}</button>
        <button type="button" onClick={() => insertSnippet("{{NewsBlocks}}")}>{"{{NewsBlocks}}"}</button>
        <button type="button" className={sourceMode ? "is-active" : ""} onClick={toggleSource} aria-label="Vis HTML">&lt;/&gt;</button>
      </div>
      {sourceMode ? (
        <textarea
          ref={sourceRef}
          className="admin-rich-source admin-template-source"
          value={value ?? ""}
          onChange={(event) => onChange(event.target.value)}
          aria-label="HTML-kilde"
        />
      ) : (
        <div
          ref={editorRef}
          className="admin-rich-input admin-template-rich-input"
          contentEditable
          suppressContentEditableWarning
          onInput={updateValueFromEditor}
        />
      )}
    </div>
  );
}
