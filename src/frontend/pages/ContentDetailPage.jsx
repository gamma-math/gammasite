import { useEffect, useState } from "react";
import { CalendarPlus, Edit3, LogIn, Trash2, UserPlus, Users } from "lucide-react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { contentApi, registrationsApi } from "../services/api.js";
import { attendeeInitials, attendeeName } from "../utils/avatar.js";
import { contentMetaLabel, formatDate } from "../utils/format.js";
import { htmlToText, sanitizeHtml } from "../utils/richText.js";

export function ContentDetailPage({ slug, type, user }) {
  const [item, setItem] = useState(null);
  const [registration, setRegistration] = useState(null);
  const [registrations, setRegistrations] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    contentApi.getBySlug(slug)
      .then(setItem)
      .catch((err) => setError(err.message));
  }, [slug]);

  useEffect(() => {
    if (item?.type === "EVENT" && user.isAuthenticated) {
      Promise.all([
        registrationsApi.mine(item.id).catch(() => null),
        registrationsApi.list(item.id).catch(() => [])
      ])
        .then(([mine, eventRegistrations]) => {
          setRegistration(mine);
          setRegistrations(eventRegistrations);
        })
        .catch(() => setRegistration(null));
    }
  }, [item?.id, item?.type, user.isAuthenticated]);

  if (error) {
    return <MenuLayout active={type === "EVENT" ? "/react/events" : "/react/news"} isAuthenticated={user.isAuthenticated} contentClassName="menu-content-flat"><p className="status-message">{error}</p></MenuLayout>;
  }

  if (!item) {
    return <MenuLayout active={type === "EVENT" ? "/react/events" : "/react/news"} isAuthenticated={user.isAuthenticated} contentClassName="menu-content-flat"><p className="muted">Henter indhold...</p></MenuLayout>;
  }

  const roles = new Set(user.roles ?? []);
  const isAdmin = roles.has("Admin") || roles.has("ADMIN");
  const isEvent = item.type === "EVENT";
  const hasImage = Boolean(item.pictureUrl);
  const registrationsPath = `/react/events/${item.slug}/registrations`;
  const editPath = `/react/admin/${type === "EVENT" ? "events" : "news"}/${item.id}/edit`;
  const calendarFileName = `${item.slug || "begivenhed"}.ics`;
  const calendarHref = isEvent ? createCalendarHref(item) : "";

  async function toggleRegistration() {
    if (registration) {
      await registrationsApi.unregister(item.id);
      setRegistration(null);
      setRegistrations((current) => current.filter((eventRegistration) => eventRegistration.userId !== registration.userId));
      return;
    }

    const next = await registrationsApi.register(item.id, { registrationType: "ATTENDEE", responseText: "" });
    setRegistration(next);
    setRegistrations((current) => {
      const withoutCurrentUser = current.filter((eventRegistration) => eventRegistration.userId !== next.userId);
      return [...withoutCurrentUser, next];
    });
  }

  return (
    <MenuLayout active={item.type === "EVENT" ? "/react/events" : "/react/news"} isAuthenticated={user.isAuthenticated} contentClassName="menu-content-flat">
      <article className="menu-detail-card">
        <div className="menu-detail-hero">
          <div>
            <p className="menu-section-title">{isEvent ? "Begivenheder" : "Nyheder"}</p>
            <p className="menu-panel-lead">{isEvent ? "Se selve eventet og læs mere om programmet." : "Læs nyheden fra foreningen."}</p>
          </div>
        </div>
        <img className={`menu-detail-image ${hasImage ? "" : "content-logo-fallback"}`.trim()} src={hasImage ? item.pictureUrl : "/lib/logo_blue.png"} alt="" />
        <div className="menu-detail-body">
          <div className="menu-detail-copy">
            <small>{contentMetaLabel(item)}</small>
            <h1>{item.title}</h1>
            <p>{item.summary}</p>
            <div className="menu-detail-rich-body" dangerouslySetInnerHTML={{ __html: sanitizeHtml(item.body ?? "") }} />
            <div className="menu-detail-meta">
              <span className="tag tag-kind">{isEvent ? "Arrangement" : "Nyhed"}</span>
              {(item.tags ?? "").split(",").filter(Boolean).map((tag) => <span className="tag" key={tag}>{tag.trim()}</span>)}
            </div>
            {isEvent && (
              <div className="event-detail-schedule">
                {item.location && <div><strong>Sted:</strong> {item.location}</div>}
                {item.startDate && <div><strong>Start:</strong> {formatDate(item.startDate)}</div>}
                {item.endDate && <div><strong>Slut:</strong> {formatDate(item.endDate)}</div>}
              </div>
            )}
            {isEvent && (
              <div className="menu-detail-bottom-bar">
                <div className="menu-attendee-preview" aria-hidden="true">
                  {registrations.slice(0, 4).map((eventRegistration) => (
                    <span className="menu-registration-avatar" title={attendeeName(eventRegistration)} key={eventRegistration.id}>{attendeeInitials(eventRegistration)}</span>
                  ))}
                </div>
                <div className="menu-detail-footer-actions">
                  {user.isAuthenticated ? (
                    <button className={`menu-attend-button menu-attend-button-primary ${registration ? "is-active" : ""}`} type="button" onClick={toggleRegistration}>
                      {registration ? <Trash2 size={16} /> : <UserPlus size={16} />}
                      {registration ? "Afmeld" : "Tilmeld"}
                    </button>
                  ) : (
                    <a className="menu-attend-button menu-attend-button-primary" href={`/Identity/Account/Login?ReturnUrl=${encodeURIComponent(window.location.pathname)}`}>
                      <LogIn size={16} />
                      Tilmeld
                    </a>
                  )}
                  {user.isAuthenticated && (
                    <Link className="menu-attend-button" href={registrationsPath}>
                      <Users size={16} />
                      Tilmeldte
                    </Link>
                  )}
                  {isAdmin && (
                    <Link className="menu-attend-button" href={editPath}>
                      <Edit3 size={16} />
                      Rediger
                    </Link>
                  )}
                  <a className="menu-attend-button" href={calendarHref} download={calendarFileName}>
                    <CalendarPlus size={16} />
                    Kalender
                  </a>
                </div>
              </div>
            )}
          </div>
          {(item.links ?? []).length > 0 && (
            <aside className="menu-detail-side" aria-label="Event links">
            {(item.links ?? []).map((link) => (
              <a className={`menu-detail-side-link menu-detail-side-link-${link.type?.toLowerCase()}`} href={link.url} key={link.id}>
                <span>{shortLinkLabel(link.label)}</span>
                <span aria-hidden="true">&gt;</span>
              </a>
            ))}
            </aside>
          )}
        </div>
      </article>
    </MenuLayout>
  );
}

function shortLinkLabel(label) {
  return (label ?? "Link")
    .replace("-gruppen", "")
    .replace("Linked In", "LinkedIn")
    .trim();
}

function createCalendarHref(item) {
  const startsAt = toIcsDate(item.startDate);
  const endsAt = toIcsDate(item.endDate || item.startDate);
  const body = htmlToText(item.body ?? item.summary ?? "");
  const uid = `gamma-${item.id || item.slug}@gammasite`;
  const calendar = [
    "BEGIN:VCALENDAR",
    "VERSION:2.0",
    "PRODID:-//GamMa//GamMa Site//DA",
    "CALSCALE:GREGORIAN",
    "METHOD:PUBLISH",
    "BEGIN:VEVENT",
    `UID:${escapeIcsText(uid)}`,
    `DTSTAMP:${toIcsDate(new Date().toISOString())}`,
    startsAt ? `DTSTART:${startsAt}` : "",
    endsAt ? `DTEND:${endsAt}` : "",
    `SUMMARY:${escapeIcsText(item.title ?? "Begivenhed")}`,
    item.location ? `LOCATION:${escapeIcsText(item.location)}` : "",
    body ? `DESCRIPTION:${escapeIcsText(body)}` : "",
    "END:VEVENT",
    "END:VCALENDAR"
  ].filter(Boolean).join("\r\n");

  return `data:text/calendar;charset=utf-8,${encodeURIComponent(calendar)}`;
}

function toIcsDate(value) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().replace(/[-:]/g, "").replace(/\.\d{3}Z$/, "Z");
}

function escapeIcsText(value) {
  return String(value ?? "")
    .replace(/\\/g, "\\\\")
    .replace(/\n/g, "\\n")
    .replace(/\r/g, "")
    .replace(/,/g, "\\,")
    .replace(/;/g, "\\;");
}
