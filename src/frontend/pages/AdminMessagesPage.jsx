import { useEffect, useRef, useState } from "react";
import { Eye, Mail, Search, Send, X } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { contentApi, emailTemplatesApi, membersApi, messagesApi, registrationsApi, rolesApi } from "../services/api.js";
import { formatDate } from "../utils/format.js";

const channels = [
  { value: "Email", label: "Email" },
  { value: "SMS", label: "SMS" },
  { value: "EmailSMS", label: "Email & SMS" }
];

/**
 * Admin message composer for recipient previews, template generation, and sending.
 */
export function AdminMessagesPage({ isAdmin }) {
  const [categories, setCategories] = useState({ statuses: [], roles: [] });
  const [templates, setTemplates] = useState([]);
  const [events, setEvents] = useState([]);
  const [news, setNews] = useState([]);
  const [templateId, setTemplateId] = useState("");
  const [recipientGroups, setRecipientGroups] = useState([]);
  const [recipientRoles, setRecipientRoles] = useState([]);
  const [recipientEventIds, setRecipientEventIds] = useState([]);
  const [channel, setChannel] = useState("Email");
  const [selectedEventIds, setSelectedEventIds] = useState([]);
  const [selectedNewsIds, setSelectedNewsIds] = useState([]);
  const [subject, setSubject] = useState("");
  const [html, setHtml] = useState("");
  const [smsBody, setSmsBody] = useState("");
  const [showPreview, setShowPreview] = useState(false);
  const [showRecipients, setShowRecipients] = useState(false);
  const [showSendConfirm, setShowSendConfirm] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [sendError, setSendError] = useState("");
  const [recipientPreview, setRecipientPreview] = useState(null);
  const [recipientDetails, setRecipientDetails] = useState([]);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [lastAction, setLastAction] = useState("");

  useEffect(() => {
    if (!isAdmin) return;

    Promise.all([
      messagesApi.categories(),
      emailTemplatesApi.list(),
      contentApi.listAdmin("EVENT"),
      contentApi.listAdmin("NEWS")
    ])
      .then(([nextCategories, nextTemplates, nextEvents, nextNews]) => {
        setCategories(nextCategories);
        setTemplates(nextTemplates);
        setEvents(nextEvents);
        setNews(nextNews);
      })
      .catch((reason) => setError(reason.message));
  }, [isAdmin]);

  useEffect(() => {
    if (!isAdmin) return;
    updateRecipientPreview();
  }, [isAdmin, recipientGroups, recipientRoles, recipientEventIds, categories.statuses.length]);

  async function updateRecipientPreview() {
    try {
      const recipients = await resolveRecipientDetails({ recipientGroups, recipientRoles, recipientEventIds });
      setRecipientDetails(recipients);
      setRecipientPreview({
        recipientCount: recipients.length,
        emailCount: recipients.filter((recipient) => recipient.email).length,
        smsCount: 0,
        recipients
      });
    } catch {
      setRecipientPreview(null);
      setRecipientDetails([]);
    }
  }

  async function renderEmail() {
    setError("");
    setMessage("");
    setLastAction("generate");
    setIsGenerating(true);

    try {
      const rendered = await messagesApi.render({
        templateId: Number(templateId),
        subject,
        selectedEventIds,
        selectedNewsIds
      });
      const nextSubject = rendered.subject ?? rendered.Subject ?? "";
      const nextHtml = rendered.html ?? rendered.Html ?? "";
      if (nextHtml || nextSubject) {
        setSubject(nextSubject);
        setHtml(nextHtml);
        setMessage(nextHtml ? "Emailen er genereret og kan redigeres nedenfor." : "Emailen blev genereret, men indholdet var tomt.");
      } else {
        renderEmailLocally("Emailen blev genereret lokalt, fordi API'et returnerede tomt indhold.");
      }
    } catch (reason) {
      renderEmailLocally(`Emailen blev genereret lokalt, fordi API'et svarede: ${reason.message}`);
    } finally {
      setIsGenerating(false);
    }
  }

  function renderEmailLocally(nextMessage) {
    const template = templates.find((item) => String(item.id) === String(templateId));
    if (!template) {
      setError("Vælg en template før du genererer emailen.");
      return;
    }

    const subjectTemplate = subject || template.subject || template.Subject || "";
    const htmlTemplate = template.htmlBody || template.HtmlBody || "";
    const values = buildLocalTemplateValues(events, news, selectedEventIds, selectedNewsIds, htmlTemplate);
    setSubject(renderPlaceholders(subjectTemplate, values));
    setHtml(renderPlaceholders(removeBlockDesignMetadata(htmlTemplate), values));
    setMessage(nextMessage);
  }

  function requestSend(event) {
    event.preventDefault();
    setError("");
    setMessage("");
    setSendError("");
    setLastAction("send");

    if (!subject.trim()) {
      setError("Emne er obligatorisk før afsendelse.");
      return;
    }

    if (!html.trim()) {
      setError("Indhold er obligatorisk før afsendelse. Tryk Generer email eller skriv indhold først.");
      return;
    }

    if ((recipientPreview?.recipientCount ?? 0) <= 0) {
      setError("Vælg modtagere");
      return;
    }

    setShowSendConfirm(true);
  }

  async function sendMessage() {
    setError("");
    setSendError("");
    setMessage("Sender besked...");
    setLastAction("send");
    setIsSending(true);

    try {
      const result = await messagesApi.send({
        ...recipientPayload(),
        channel,
        subject,
        html,
        smsBody
      });
      setMessage(`Beskeden er sendt til ${result.recipientCount} modtagere.`);
      setRecipientPreview(result);
      setShowSendConfirm(false);
    } catch (reason) {
      const message = reason.message === "Method Not Allowed"
        ? "Backend tager ikke imod POST på /api/messages/send endnu. Genstart den lokale ASP.NET app, så de nye API-endpoints bliver aktive."
        : reason.message;
      setSendError(message);
      setError(message);
      setMessage("");
    } finally {
      setIsSending(false);
    }
  }

  async function openRecipientModal() {
    setError("");
    setLastAction("recipients");
    try {
      const recipients = await resolveRecipientDetails({ recipientGroups, recipientRoles, recipientEventIds });
      setRecipientDetails(recipients);
      setRecipientPreview((current) => ({
        ...(current ?? {}),
        recipientCount: recipients.length,
        recipients
      }));
      setShowRecipients(true);
    } catch (reason) {
      const recipients = recipientPreview?.recipients ?? recipientPreview?.Recipients ?? [];
      setRecipientDetails(recipients);
      setShowRecipients(true);
      if (recipients.length === 0) {
        setError(reason.message);
      }
    }
  }

  function recipientPayload() {
    const statuses = recipientGroups.includes("ALL")
      ? categories.statuses
      : recipientGroups;
    const roles = recipientRoles;
    return { statuses, roles, recipientEventIds };
  }

  const groupOptions = [
    { id: "ALL", title: "Alle medlemmer" },
    ...categories.statuses.map((status) => ({ id: status, title: status }))
  ];
  const roleOptions = categories.roles.map((role) => ({ id: role, title: role }));
  const previewRecipients = recipientDetails.length > 0
    ? recipientDetails
    : recipientPreview?.recipients ?? recipientPreview?.Recipients ?? [];

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/messages" canWrite={false}><p className="status-message status-message-warning">Kun ADMIN kan sende beskeder.</p></AdminLayout>;
  }

  return (
    <AdminLayout active="/react/admin/messages" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Beskeder</p>
        </div>
      </div>

      {!lastAction && error && <p className="status-message status-message-error">{error}</p>}

      <form className="admin-message-composer" onSubmit={requestSend}>
        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar"><strong>Opsætning</strong></div>
          <div className="admin-message-fields admin-message-fields-top">
            <label className="admin-field">
              <span>Template</span>
              <select value={templateId} onChange={(event) => setTemplateId(event.target.value)} required>
                <option value="">Vælg template</option>
                {templates.map((template) => <option value={template.id} key={template.id}>{template.name}</option>)}
              </select>
            </label>
            <label className="admin-field">
              <span>Kanal</span>
              <select value={channel} onChange={(event) => setChannel(event.target.value)}>
                {channels.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
              </select>
            </label>
          </div>
          <div className="admin-message-recipient-filters">
            <MultiSelectDropdown
              label="Modtagergruppe"
              placeholder="Vælg grupper"
              items={groupOptions}
              selectedIds={recipientGroups}
              onChange={setRecipientGroups}
              showMeta={false}
            />
            <MultiSelectDropdown
              label="Rolle"
              placeholder="Alle roller"
              items={roleOptions}
              selectedIds={recipientRoles}
              onChange={setRecipientRoles}
              showMeta={false}
            />
            <MultiSelectDropdown
              label="Deltagere"
              placeholder="Vælg event-deltagere"
              items={events}
              selectedIds={recipientEventIds}
              onChange={setRecipientEventIds}
              searchable
            />
          </div>
          {recipientPreview && (
            <div className="admin-message-recipient-preview">
              <span>{recipientPreview.recipientCount} modtagere</span>
              <button className="admin-recipient-search-button" type="button" onClick={openRecipientModal} aria-label="Se modtagere">
                <Search size={17} />
              </button>
            </div>
          )}
          {lastAction === "recipients" && error && <p className="status-message status-message-error">{error}</p>}
        </section>

        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar"><strong>Indhold</strong></div>
          <div className="admin-message-content-picker">
            <MultiSelectDropdown
              label="Events"
              placeholder="Vælg events"
              items={events}
              selectedIds={selectedEventIds}
              onChange={setSelectedEventIds}
              searchable
            />
            <MultiSelectDropdown
              label="Nyheder"
              placeholder="Vælg nyheder"
              items={news}
              selectedIds={selectedNewsIds}
              onChange={setSelectedNewsIds}
              searchable
            />
          </div>
          <div className="admin-message-generate-row">
            <button className="menu-create-button" type="button" onClick={renderEmail} disabled={!templateId || isGenerating}>
              <Mail size={16} />
              {isGenerating ? "Genererer..." : "Generer email"}
            </button>
          </div>
          {lastAction === "generate" && message && <p className={messageStatusClass(message)}>{message}</p>}
          {lastAction === "generate" && error && <p className="status-message status-message-error">{error}</p>}
        </section>

        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar"><strong>Rediger færdig mail</strong></div>
          <div className="admin-message-editor">
            <label className="admin-field">
              <span>Emne</span>
              <input value={subject} onChange={(event) => setSubject(event.target.value)} required />
            </label>
            <MessageRichTextEditor value={html} onChange={setHtml} />
            {(channel === "SMS" || channel === "EmailSMS") && (
              <label className="admin-field">
                <span>SMS tekst</span>
                <textarea value={smsBody} onChange={(event) => setSmsBody(event.target.value)} placeholder="Hvis tom, sendes emnet som SMS." />
              </label>
            )}
          </div>
        </section>

        <div className="menu-editor-actions">
          <button className="admin-action-button" type="button" onClick={() => setShowPreview(true)}>
            <Eye size={16} />
            Preview
          </button>
          <button className="profile-button" type="submit">
            <Send size={16} />
            {isSending ? "Sender..." : "Send besked"}
          </button>
        </div>
        {lastAction === "send" && message && <p className={messageStatusClass(message)}>{message}</p>}
        {lastAction === "send" && error && <p className="status-message status-message-error">{error}</p>}
      </form>

      {showPreview && (
        <div className="admin-preview-modal-backdrop" role="presentation" onClick={() => setShowPreview(false)}>
          <section className="admin-preview-modal" role="dialog" aria-modal="true" aria-label="Email preview" onClick={(event) => event.stopPropagation()}>
            <div className="admin-preview-modal-header">
              <div>
                <p className="menu-section-title">Preview</p>
                <h2>{subject || "Uden emne"}</h2>
              </div>
              <button className="admin-preview-close" type="button" aria-label="Luk preview" onClick={() => setShowPreview(false)}>
                <X size={20} />
              </button>
            </div>
            <iframe className="admin-template-preview-frame" title="Email preview" srcDoc={html || "<p>Generer mailen for at se preview.</p>"} />
          </section>
        </div>
      )}

      {showRecipients && (
        <div className="admin-preview-modal-backdrop" role="presentation" onClick={() => setShowRecipients(false)}>
          <section className="admin-recipient-modal" role="dialog" aria-modal="true" aria-label="Modtagere" onClick={(event) => event.stopPropagation()}>
            <div className="admin-preview-modal-header">
              <div>
                <p className="menu-section-title">Modtagere</p>
                <h2>{previewRecipients.length || recipientPreview?.recipientCount || 0} modtagere</h2>
              </div>
              <button className="admin-preview-close" type="button" aria-label="Luk modtagere" onClick={() => setShowRecipients(false)}>
                <X size={20} />
              </button>
            </div>
            <div className="admin-recipient-list">
              {previewRecipients.map((recipient) => {
                const name = recipient.name ?? recipient.Name;
                const email = recipient.email ?? recipient.Email;
                return (
                <div className="admin-recipient-row" key={`${name}-${email}`}>
                  <strong>{name || "Ukendt navn"}</strong>
                  <span>{email || "Ingen email"}</span>
                </div>
                );
              })}
              {previewRecipients.length === 0 && <p className="muted">Ingen modtagere fundet.</p>}
            </div>
          </section>
        </div>
      )}

      {showSendConfirm && (
        <div className="admin-preview-modal-backdrop" role="presentation" onClick={() => setShowSendConfirm(false)}>
          <section className="admin-recipient-modal" role="dialog" aria-modal="true" aria-label="Bekræft afsendelse" onClick={(event) => event.stopPropagation()}>
            <div className="admin-preview-modal-header">
              <div>
                <p className="menu-section-title">Bekræft afsendelse</p>
                <h2>Send til {recipientPreview?.recipientCount ?? 0} modtagere?</h2>
              </div>
              <button className="admin-preview-close" type="button" aria-label="Luk bekræftelse" onClick={() => setShowSendConfirm(false)}>
                <X size={20} />
              </button>
            </div>
            <div className="admin-send-confirm-body">
              <p><strong>Emne:</strong> {subject}</p>
              <p className="muted">Mailen sendes via den eksisterende Mailgun-opsætning til de valgte modtagere.</p>
              {isSending && <p className="status-message">Sender besked...</p>}
              {sendError && <p className="status-message status-message-error">{sendError}</p>}
              <div className="menu-editor-actions">
                <button className="frontpage-button frontpage-button-secondary" type="button" onClick={() => setShowSendConfirm(false)}>Annuller</button>
                <button className="profile-button" type="button" onClick={(event) => {
                  event.preventDefault();
                  event.stopPropagation();
                  sendMessage();
                }} disabled={isSending}>
                  <Send size={16} />
                  {isSending ? "Sender..." : "Ja, send besked"}
                </button>
              </div>
            </div>
          </section>
        </div>
      )}
    </AdminLayout>
  );
}

/**
 * Reusable dropdown for multi-select filters in message setup.
 */
function MultiSelectDropdown({ label, placeholder, items, selectedIds, onChange, showMeta = true, searchable = false }) {
  const [search, setSearch] = useState("");
  const selectedItems = (items ?? []).filter((item) => selectedIds.includes(item.id));
  const filteredItems = (items ?? []).filter((item) => {
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
    </details>
  );
}

/**
 * Loads recipient names/emails for the current local preview selections.
 */
async function resolveRecipientDetails({ recipientGroups, recipientRoles, recipientEventIds }) {
  const [allMembers, roles] = await Promise.all([membersApi.listAdmin(), rolesApi.list()]);
  const recipients = new Map();

  function add(user) {
    const email = user?.email ?? user?.Email ?? "";
    const name = user?.name ?? user?.userName ?? user?.Name ?? user?.UserName ?? email;
    const id = user?.id ?? user?.userId ?? user?.Id ?? user?.UserId ?? "";
    const key = email ? `email:${email.toLowerCase()}` : `id:${id}`;
    if (!email && !id) return;
    recipients.set(key, { name, email });
  }

  if (recipientGroups.includes("ALL")) {
    allMembers.forEach(add);
  } else {
    allMembers
      .filter((member) => recipientGroups.includes(member.status ?? member.Status))
      .forEach(add);
  }

  for (const roleName of recipientRoles) {
    const role = roles.find((entry) => entry.name === roleName || entry.Name === roleName);
    if (role) {
      const membership = await rolesApi.members(role.id ?? role.Id);
      (membership.members ?? membership.Members ?? []).forEach(add);
    }
  }

  for (const eventId of recipientEventIds) {
    const registrations = await registrationsApi.list(eventId);
    registrations
      .filter((registration) => registration.registered ?? registration.Registered)
      .forEach(add);
  }

  return [...recipients.values()].sort((left, right) => String(left.name ?? "").localeCompare(String(right.name ?? ""), "da-DK"));
}

function buildLocalTemplateValues(events, news, selectedEventIds, selectedNewsIds, templateHtml) {
  const selectedEvents = events.filter((item) => selectedEventIds.includes(item.id));
  const selectedNews = news.filter((item) => selectedNewsIds.includes(item.id));
  const design = parseBlockDesign(templateHtml);
  const eventBlocks = selectedEvents.map((item) => renderLocalContentBlock(item, design)).join("");
  const newsBlocks = selectedNews.map((item) => renderLocalContentBlock(item, design)).join("");
  const firstEvent = selectedEvents[0];

  return {
    ContentBlocks: `${eventBlocks}${newsBlocks}`,
    EventBlocks: eventBlocks,
    NewsBlocks: newsBlocks,
    EventTitle: firstEvent?.title ?? "",
    EventStartDate: firstEvent ? formatDate(firstEvent.startDate) : "",
    EventRegisterUrl: firstEvent ? `${window.location.origin}/react/events/${firstEvent.slug}` : "",
    ProfileUrl: `${window.location.origin}/Identity/Account/Manage`
  };
}

function renderLocalContentBlock(item, design) {
  const isEvent = item.type === "EVENT";
  const accentColor = isEvent ? design.eventColor : design.newsColor;
  const pathType = isEvent ? "events" : "news";
  const url = `${window.location.origin}/react/${pathType}/${item.slug}`;
  const links = item.links ?? item.Links ?? [];
  const ctaText = isEvent ? "Tilmeld dig" : "Læs mere";
  const linkItems = links
    .filter((link) => link.url || link.Url)
    .map(renderLocalRelatedLinkButton)
    .join("");
  const eventMeta = isEvent
    ? `
  ${item.location ? `<p style="margin:0 0 6px;"><strong>Sted:</strong> ${escapeHtml(item.location)}</p>` : ""}
  <p style="margin:0 0 6px;"><strong>Start:</strong> ${escapeHtml(formatDate(item.startDate))}</p>
  ${item.endDate ? `<p style="margin:0 0 12px;"><strong>Slut:</strong> ${escapeHtml(formatDate(item.endDate))}</p>` : ""}`
    : "";

  return `
<div style="width:90%;margin:0 auto 18px auto;padding:16px;background-color:#f7fbff;border:1px solid #d9e7f5;border-left:6px solid ${escapeHtml(accentColor)};border-radius:8px;color:#132238;">
  <p style="margin:0 0 8px;font-weight:bold;font-size:1.08rem;">${escapeHtml(item.title)}</p>
  ${eventMeta}
  ${item.body ? `<div style="margin:0 0 12px;color:#4f5f73;line-height:1.55;">${item.body}</div>` : ""}
  ${linkItems ? `<div style="margin:0 0 12px;">${linkItems}</div>` : ""}
  <a href="${escapeHtml(url)}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:${escapeHtml(accentColor)};color:#ffffff;text-decoration:none;border-radius:8px;padding:12px 16px;font-size:0.95rem;font-weight:bold;">${escapeHtml(ctaText)}</a>
</div>`;
}

function renderLocalRelatedLinkButton(link) {
  const linkUrl = link.url ?? link.Url;
  const label = link.label ?? link.Label ?? linkUrl;
  const isPayment = String(link.type ?? link.Type ?? "").toUpperCase() === "PAYMENT";
  const backgroundColor = isPayment ? "#4353f4" : "#ffffff";
  const textColor = isPayment ? "#ffffff" : "#132238";
  const borderColor = isPayment ? "#4353f4" : "#c9d8ea";

  return `<a href="${escapeHtml(linkUrl)}" target="_blank" style="display:block;margin:0 0 10px;padding:12px 16px;background-color:${backgroundColor};color:${textColor};border:1px solid ${borderColor};text-align:center;text-decoration:none;border-radius:8px;font-size:0.95rem;font-weight:bold;line-height:1.2;">${escapeHtml(label)}</a>`;
}

function messageStatusClass(message) {
  const text = String(message ?? "").toLowerCase();
  if (text.includes("lokalt") || text.includes("tomt")) {
    return "status-message status-message-warning";
  }

  if (text.includes("sender")) {
    return "status-message";
  }

  return "status-message status-message-success";
}

const blockDesignRegex = /<!--\s*GammaEmailBlockDesign\s+eventColor="([^"]*)"\s+newsColor="([^"]*)"\s*-->/i;

function parseBlockDesign(html) {
  const match = String(html ?? "").match(blockDesignRegex);
  return {
    eventColor: normalizeColor(match?.[1], "#1f78c1"),
    newsColor: normalizeColor(match?.[2], "#1f78c1")
  };
}

function removeBlockDesignMetadata(html) {
  return String(html ?? "").replace(blockDesignRegex, "");
}

function normalizeColor(value, fallback) {
  return /^#[0-9a-f]{6}$/i.test(value ?? "") ? value : fallback;
}

function renderPlaceholders(template, values) {
  return String(template ?? "").replace(/\{\{([A-Za-z0-9_]+)\}\}/g, (match, key) => values[key] ?? match);
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

/**
 * Rich text editor wrapper used by the final editable message body.
 */
function MessageRichTextEditor({ value, onChange }) {
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

  function toggleSource() {
    if (!sourceMode) {
      updateValueFromEditor();
    } else {
      lastEditorValue.current = null;
    }
    setSourceMode((current) => !current);
  }

  return (
    <div className="admin-rich-editor admin-message-rich-editor">
      <span className="admin-field-label">Indhold</span>
      <div className="admin-rich-toolbar" role="toolbar" aria-label="Mail formatering">
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
        <textarea
          ref={sourceRef}
          className="admin-rich-source admin-message-source"
          value={value ?? ""}
          onChange={(event) => onChange(event.target.value)}
          aria-label="HTML-kilde"
        />
      ) : (
        <div
          ref={editorRef}
          className="admin-rich-input admin-message-rich-input"
          contentEditable
          suppressContentEditableWarning
          onInput={updateValueFromEditor}
        />
      )}
    </div>
  );
}
