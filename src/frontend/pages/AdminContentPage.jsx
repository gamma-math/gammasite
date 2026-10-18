import { useEffect, useMemo, useRef, useState } from "react";
import { Eye, ImagePlus, Plus, Save, Trash2, X } from "lucide-react";
import { RichTextEditor as SharedRichTextEditor } from "../components/RichTextEditor.jsx";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { Link, navigate } from "../routes/navigation.jsx";
import { contentApi, paymentOffersApi } from "../services/api.js";
import { contentMetaLabel, detailPath, emptyContent, formatDate } from "../utils/format.js";
import { isLocalContentImage, isSupportedContentImage } from "../utils/contentMedia.js";
import { sanitizeHtml } from "../utils/richText.js";
import { SortableHeader } from "./MembersPage.jsx";

/**
 * Admin overview for event and news content.
 */
export function AdminContentPage({ type, isAdmin }) {
  const [items, setItems] = useState([]);
  const [sort, setSort] = useState({ key: "date", direction: "desc" });

  useEffect(() => {
    if (isAdmin) {
      load();
    }
  }, [type, isAdmin]);

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
          {!isAdmin && <p className="status-message status-message-warning">Du har ikke rettigheder til at redigere events og nyheder.</p>}
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
  const [eventAccess, setEventAccess] = useState(null);
  const [isLoading, setIsLoading] = useState(Boolean(itemId));
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [showPreview, setShowPreview] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [paymentOffers, setPaymentOffers] = useState([]);
  const [paymentOfferDrafts, setPaymentOfferDrafts] = useState([]);
  const [linksEditorVersion, setLinksEditorVersion] = useState(0);
  const basePath = `/react/admin/${type === "EVENT" ? "events" : "news"}`;
  const label = type === "EVENT" ? "Begivenheder" : "Nyheder";

  useEffect(() => {
    if (!itemId) {
      setSelected(emptyContent(type));
      setEventAccess(null);
      setPaymentOffers([]);
      setPaymentOfferDrafts([]);
      setIsLoading(false);
      return;
    }
    setIsLoading(true);
    Promise.all([
      contentApi.getById(itemId),
      type === "EVENT" ? contentApi.access(itemId).catch(() => null) : Promise.resolve(null),
      type === "EVENT" ? paymentOffersApi.listForEvent(itemId).catch(() => []) : Promise.resolve([])
    ]).then(([item, access, offers]) => {
      if (item) setSelected(normalizeContentItem(item, type));
      setEventAccess(access);
      setPaymentOffers(offers ?? []);
      setPaymentOfferDrafts([]);
    }).catch((reason) => setError(reason.message)).finally(() => setIsLoading(false));
  }, [type, itemId]);

  async function save(event) {
    event.preventDefault();
    setError("");
    setMessage("");
    try {
      const payload = buildSavePayload(selected, type);
      const result = selected.id ? await contentApi.update(selected.id, payload) : await contentApi.create(payload);
      let nextItem = normalizeContentItem(result, type);
      if (type === "EVENT" && paymentOfferDrafts.some(hasPaymentOfferDraft)) {
        const savedOffers = [];
        for (const draft of paymentOfferDrafts.filter(hasPaymentOfferDraft)) {
          savedOffers.push(await paymentOffersApi.saveForEvent(nextItem.id, {
            linkLabel: draft.linkLabel,
            provider: draft.provider,
            amount: Number(draft.amount),
            productName: draft.productName
          }));
        }
        setPaymentOffers((current) => {
          const next = [...current];
          savedOffers.forEach((offer) => {
            const index = next.findIndex((currentOffer) => currentOffer.provider === offer.provider);
            if (index >= 0) next[index] = offer;
            else next.push(offer);
          });
          return next;
        });
        const refreshed = await contentApi.getById(nextItem.id);
        nextItem = normalizeContentItem(refreshed, type);
        setPaymentOfferDrafts([]);
      }
      setSelected(nextItem);
      setLinksEditorVersion((current) => current + 1);
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

  async function removePaymentOffer(offerId) {
    if (!window.confirm("Fjern betalingslinket og deaktiver den tilhørende betaling?")) {
      return;
    }

    setError("");
    setMessage("");
    try {
      await paymentOffersApi.remove(offerId);
      setPaymentOffers((current) => current.filter((offer) => offer.id !== offerId));
      setPaymentOfferDrafts((current) => current.filter((draft) => draft.offerId !== offerId));
      setSelected((current) => ({
        ...current,
        links: (current.links ?? []).filter((link) => !isPaymentOfferLink(link, offerId))
      }));
      setMessage("Betalingslinket er fjernet.");
    } catch (reason) {
      setError(reason.message);
    }
  }

  async function uploadImage(file) {
    if (!selected.id) return;
    setError("");
    setMessage("");
    if (!isSupportedContentImage(file)) {
      setError("Vælg en PNG- eller JPEG-fil på højst 2 MiB.");
      return;
    }

    setIsUploading(true);
    try {
      const result = await contentApi.uploadImage(selected.id, file);
      update("pictureUrl", result.url);
      setMessage("Billedet er uploadet.");
    } catch (reason) {
      setError(reason.message);
    } finally {
      setIsUploading(false);
    }
  }

  async function deleteImage() {
    if (!selected.id || !selected.pictureUrl) return;
    setError("");
    setMessage("");
    try {
      await contentApi.deleteImage(selected.id);
      update("pictureUrl", "");
      setMessage("Billedet er slettet.");
    } catch (reason) {
      setError(reason.message);
    }
  }

  const canEdit = isAdmin || Boolean(eventAccess?.canEditEvent);

  if (isLoading) {
    return <AdminLayout active={basePath} canWrite={false}><p className="muted">Henter begivenhed...</p></AdminLayout>;
  }

  if (!canEdit) {
    return <AdminLayout active={basePath} canWrite={false}><p className="status-message status-message-warning">Du har ikke rettigheder til at redigere events og nyheder.</p></AdminLayout>;
  }

  return (
    <AdminLayout active={basePath} canWrite={canEdit}>
      <div className="menu-panel-header">
        <div><p className="menu-section-title">{label}</p><h1>{selected.id ? "Rediger" : "Opret ny"} {type === "EVENT" ? "begivenhed" : "nyhed"}</h1></div>
        <div className="menu-panel-header-actions">
          {type === "EVENT" && selected.id && selected.slug && (
            <Link className="frontpage-button frontpage-button-secondary" href={detailPath(selected)}>Se begivenhed</Link>
          )}
          {isAdmin && <Link className="frontpage-button frontpage-button-secondary" href={basePath}>Tilbage til oversigt</Link>}
        </div>
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
        <SharedRichTextEditor label="Tekst" value={selected.body ?? ""} onChange={(value) => update("body", value)} />
        <ContentImageField
          value={selected.pictureUrl}
          itemId={selected.id}
          isUploading={isUploading}
          onChange={(value) => update("pictureUrl", value)}
          onUpload={uploadImage}
          onDelete={deleteImage}
        />
        <div className="menu-editor-grid">
          <label className="admin-field"><span>Tags</span><input value={selected.tags ?? ""} onChange={(event) => update("tags", event.target.value)} placeholder="event,karriere" /></label>
          {type === "EVENT" && <label className="admin-field"><span>Sted</span><input value={selected.location ?? ""} onChange={(event) => update("location", event.target.value)} /></label>}
          {type === "EVENT" && <label className="admin-field"><span>Start</span><input type="datetime-local" value={toLocalInput(selected.startDate)} onChange={(event) => update("startDate", event.target.value)} /></label>}
          {type === "EVENT" && <label className="admin-field"><span>Slut</span><input type="datetime-local" value={toLocalInput(selected.endDate)} onChange={(event) => update("endDate", event.target.value)} /></label>}
          {type === "EVENT" && <label className="admin-field"><span>Maks. antal deltagere</span><input type="number" min="0" value={selected.maxParticipants ?? ""} onChange={(event) => update("maxParticipants", event.target.value === "" ? null : Number(event.target.value))} placeholder="Ingen begrænsning" /></label>}
        </div>
        <LinkEditor key={linksEditorVersion} links={selected.links ?? []} onChange={(links) => update("links", links)} />
        {type === "EVENT" && (
          <PaymentOfferEditorMultiple
            offers={paymentOffers}
            drafts={paymentOfferDrafts}
            onChange={setPaymentOfferDrafts}
            onAdd={() => setPaymentOfferDrafts((current) => [...current, emptyPaymentOffer()])}
            onRemove={(draft, index) => {
              if (draft.offerId) {
                removePaymentOffer(draft.offerId);
              } else {
                setPaymentOfferDrafts((current) => current.filter((_, draftIndex) => draftIndex !== index));
              }
            }}
            onEdit={(offer) => setPaymentOfferDrafts((current) => {
              const draft = {
                offerId: offer.id,
                linkLabel: offer.linkLabel ?? "",
                provider: offer.provider ?? "STRIPE",
                amount: offer.amountMinor ? String(offer.amountMinor / 100) : "",
                productName: offer.productName ?? ""
              };
              const existingIndex = current.findIndex((item) => item.offerId === offer.id);
              if (existingIndex < 0) {
                return [...current, draft];
              }

              return current.map((item, index) => index === existingIndex ? draft : item);
            })}
          />
        )}
        <div className="menu-editor-actions"><button className="profile-button" type="submit"><Save size={16} /> Gem</button><button className="profile-button profile-button-secondary" type="button" onClick={() => setShowPreview(true)}><Eye size={16} /> Preview</button>{isAdmin && <button className="profile-button profile-button-danger" type="button" onClick={remove}><Trash2 size={16} /> Slet</button>}</div>
        {message && <p className="status-message status-message-success">{message}</p>}
        {error && <p className="status-message status-message-error">{error}</p>}
      </form>
      {showPreview && <ContentPreview item={selected} type={type} onClose={() => setShowPreview(false)} />}
    </AdminLayout>
  );

  function update(field, value) { setSelected((current) => ({ ...current, [field]: value })); }
}

function ContentImageField({ value, itemId, isUploading, onChange, onUpload, onDelete }) {
  const inputRef = useRef(null);
  const hasLocalImage = isLocalContentImage(value);

  return (
    <div className="admin-field admin-content-image-field">
      <span className="admin-field-heading">
        Billede URL
        <span className="admin-info-tooltip" tabIndex="0" aria-label="Information om billedformat">
          ?
          <span className="admin-info-tooltip-text" role="tooltip">
            PNG eller JPEG, højst 2 MiB.
          </span>
        </span>
      </span>
      <div className="admin-content-image-input-row">
        <input value={value ?? ""} onChange={(event) => onChange(event.target.value)} placeholder="https://... eller /media/..." />
        <div className="admin-content-image-actions">
          <input
            ref={inputRef}
            className="admin-file-input"
            type="file"
            accept=".png,.jpg,.jpeg,image/png,image/jpeg"
            onChange={(event) => {
              onUpload(event.target.files?.[0]);
              event.target.value = "";
            }}
            disabled={!itemId || isUploading}
          />
          <button className="admin-action-button" type="button" onClick={() => inputRef.current?.click()} disabled={!itemId || isUploading}>
            <ImagePlus size={16} /> {isUploading ? "Uploader..." : "Upload billede"}
          </button>
          {hasLocalImage && <button className="admin-action-button admin-action-button-danger" type="button" onClick={onDelete} disabled={isUploading}><X size={16} /> Slet upload</button>}
        </div>
      </div>
    </div>
  );
}

function ContentPreview({ item, type, onClose }) {
  const isEvent = type === "EVENT";
  const hasImage = Boolean(item.pictureUrl);

  return (
    <div className="admin-preview-modal-backdrop" role="presentation" onClick={onClose}>
      <section className="admin-preview-modal admin-content-preview-modal" role="dialog" aria-modal="true" aria-label="Indholds-preview" onClick={(event) => event.stopPropagation()}>
        <div className="admin-preview-modal-header">
          <div><p className="menu-section-title">Preview</p><h2>{item.title || (isEvent ? "Begivenhed" : "Nyhed")}</h2></div>
          <button className="admin-preview-close" type="button" aria-label="Luk preview" onClick={onClose}><X size={20} /></button>
        </div>
        <div className="admin-rendered-preview admin-content-preview-body">
          <article className="menu-detail-card">
            <img className={`menu-detail-image ${hasImage ? "" : "content-logo-fallback"}`.trim()} src={hasImage ? item.pictureUrl : "/lib/logo_blue.png"} alt="" />
            <div className="menu-detail-body">
              <div className="menu-detail-copy">
                <small>{contentMetaLabel({ ...item, type })}</small>
                <h1>{item.title}</h1>
                <p>{item.summary}</p>
                <div className="menu-detail-rich-body" dangerouslySetInnerHTML={{ __html: sanitizeHtml(item.body ?? "") }} />
                <div className="menu-detail-meta">
                  <span className="tag tag-kind">{isEvent ? "Arrangement" : "Nyhed"}</span>
                  {(item.tags ?? "").split(",").filter(Boolean).map((tag) => <span className="tag" key={tag}>{tag.trim()}</span>)}
                </div>
                {isEvent && <div className="event-detail-schedule">{item.location && <div><strong>Sted:</strong> {item.location}</div>}{item.startDate && <div><strong>Start:</strong> {formatDate(item.startDate)}</div>}{item.endDate && <div><strong>Slut:</strong> {formatDate(item.endDate)}</div>}</div>}
              </div>
            </div>
          </article>
        </div>
      </section>
    </div>
  );
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
 * Editor for manually configured related content links, including external payment links.
 */
function LinkEditor({ links, onChange }) {
  const editableLinks = links;
  const [editingLinkIds, setEditingLinkIds] = useState(() => new Set());
  const savedLinks = editableLinks.filter((link) => link.id);
  const draftLinks = editableLinks.filter((link) => !link.id);

  function updateLinks(nextEditableLinks) {
    onChange(nextEditableLinks.map((link, index) => ({ ...link, sortOrder: index })));
  }

  function update(index, field, value) {
    updateLinks(editableLinks.map((link, currentIndex) => currentIndex === index ? { ...link, [field]: value } : link));
  }

  function removeLink(index) {
    updateLinks(editableLinks.filter((_, linkIndex) => linkIndex !== index));
  }

  function renderLinkEditor(link) {
    const index = editableLinks.indexOf(link);
    return (
      <div className="admin-payment-offer-draft" key={link.id ?? `draft-${index}`}>
        <div className="menu-editor-grid">
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
        <button className="admin-action-button admin-action-button-danger" type="button" onClick={() => removeLink(index)}>Fjern</button>
      </div>
    );
  }

  return (
    <section className="menu-editor-link-row admin-payment-offer-editor">
      <div>
        <span className="admin-field-label">Links</span>
      </div>
      {savedLinks.length > 0 && (
        <div className="admin-payment-offer-list">
          {savedLinks.map((link) => {
            const index = editableLinks.indexOf(link);
            const isEditing = editingLinkIds.has(link.id);
            return (
              <div className="admin-payment-offer-entry" key={link.id}>
                <div className="admin-payment-offer-summary">
                  <span><strong>{link.label || "Link"}</strong> · {link.type || "OTHER"}</span>
                  <button className="admin-action-button" type="button" onClick={() => setEditingLinkIds((current) => new Set(current).add(link.id))}>Rediger</button>
                </div>
                {isEditing && renderLinkEditor(link, index)}
              </div>
            );
          })}
        </div>
      )}
      {draftLinks.map((link) => renderLinkEditor(link))}
      <button className="menu-inline-add-button" type="button" onClick={() => updateLinks([...editableLinks, { label: "", url: "", type: "OTHER" }])}>
        <Plus size={16} />
        Tilføj link
      </button>
    </section>
  );
}

function PaymentOfferEditorMultiple({ offers, drafts, onChange, onAdd, onEdit, onRemove }) {
  function update(index, field, value) {
    onChange(drafts.map((draft, draftIndex) => draftIndex === index ? { ...draft, [field]: value } : draft));
  }

  function renderDraft(draft, index) {
    return (
      <div className="admin-payment-offer-draft" key={draft.offerId ?? `draft-${index}`}>
        <div className="menu-editor-grid">
          <label className="admin-field">
            <span>Link label</span>
            <input value={draft.linkLabel} onChange={(event) => update(index, "linkLabel", event.target.value)} placeholder="Køb billet" />
          </label>
          <label className="admin-field">
            <span>Type</span>
            <select value={draft.provider} onChange={(event) => update(index, "provider", event.target.value)}>
              <option value="STRIPE">Stripe</option>
              <option value="MOBILEPAY">MobilePay / Vipps</option>
            </select>
          </label>
          <label className="admin-field">
            <span>Pris (DKK)</span>
            <input type="number" min="0.01" step="0.01" value={draft.amount} onChange={(event) => update(index, "amount", event.target.value)} placeholder="150" />
          </label>
          <label className="admin-field">
            <span>Produktnavn</span>
            <input value={draft.productName} onChange={(event) => update(index, "productName", event.target.value)} placeholder="Billet til arrangementet" />
          </label>
        </div>
        <button className="admin-action-button admin-action-button-danger" type="button" onClick={() => onRemove(draft, index)}>Fjern</button>
      </div>
    );
  }

  return (
    <section className="menu-editor-link-row admin-payment-offer-editor">
      <div>
        <span className="admin-field-label">Betalingslink</span>
      </div>
      {offers.length > 0 && (
        <div className="admin-payment-offer-list">
          {offers.map((offer) => {
            const draftIndex = drafts.findIndex((draft) => draft.offerId === offer.id);
            return (
              <div className="admin-payment-offer-entry" key={offer.id}>
                <div className="admin-payment-offer-summary">
                  <span><strong>{offer.linkLabel || "Betalingslink"}</strong> · {offer.provider} · {formatOfferAmount(offer.amountMinor)} DKK</span>
                  <button className="admin-action-button" type="button" onClick={() => onEdit(offer)}>Rediger</button>
                </div>
                {draftIndex >= 0 && renderDraft(drafts[draftIndex], draftIndex)}
              </div>
            );
          })}
          {drafts.map((draft, index) => !draft.offerId && renderDraft(draft, index))}
        </div>
      )}
      {offers.length === 0 && drafts.map((draft, index) => renderDraft(draft, index))}
      <button className="menu-inline-add-button" type="button" onClick={onAdd}>
        <Plus size={16} />
        Tilføj betalingslink
      </button>
    </section>
  );
}

function emptyPaymentOffer() {
  return {
    linkLabel: "",
    provider: "STRIPE",
    amount: "",
    productName: ""
  };
}

function hasPaymentOfferDraft(draft) {
  return Boolean(draft?.linkLabel || draft?.amount || draft?.productName);
}

function formatOfferAmount(amountMinor) {
  return amountMinor == null ? "-" : (Number(amountMinor) / 100).toLocaleString("da-DK", { minimumFractionDigits: 2 });
}

function isPaymentOfferLink(link, offerId) {
  return String(link.type ?? "").toUpperCase() === "PAYMENT"
    && link.url?.endsWith(`/pay/${offerId}`);
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
    maxParticipants: item.maxParticipants ?? item.MaxParticipants ?? null,
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
    maxParticipants: type === "EVENT" && item.maxParticipants !== "" && item.maxParticipants != null ? Number(item.maxParticipants) : null,
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
