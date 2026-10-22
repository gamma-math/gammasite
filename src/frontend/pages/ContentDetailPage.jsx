import { useEffect, useState } from "react";
import { CalendarPlus, Edit3, LogIn, Trash2, UserPlus, Users } from "lucide-react";
import { ConfirmationDialog } from "../components/ConfirmationDialog.jsx";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { contentApi, registrationsApi } from "../services/api.js";
import { attendeeInitials, attendeeName } from "../utils/avatar.js";
import { contentMetaLabel, formatDate } from "../utils/format.js";
import { htmlToText, sanitizeHtml } from "../utils/richText.js";
import { hasPermission, permissions } from "../utils/access.js";
import { isRegistrationOpen } from "../utils/registrationOpen.js";
import { confirmedRegistrationCount, countsTowardsCapacity, isWaitlisted } from "../utils/registrationCapacity.js";

/**
 * Shows a single event or news article with registration and related-link actions.
 */
export function ContentDetailPage({ slug, type, user }) {
  const [item, setItem] = useState(null);
  const [registration, setRegistration] = useState(null);
  const [registrations, setRegistrations] = useState([]);
  const [capacitySummary, setCapacitySummary] = useState(null);
  const [eventAccess, setEventAccess] = useState(null);
  const [calendarHref, setCalendarHref] = useState("");
  const [showUnregisterConfirmation, setShowUnregisterConfirmation] = useState(false);
  const [isRemovingRegistration, setIsRemovingRegistration] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    contentApi.getBySlug(slug)
      .then(setItem)
      .catch((err) => setError(err.message));
  }, [slug]);

  useEffect(() => {
    if (item?.type !== "EVENT") {
      setCapacitySummary(null);
      setEventAccess(null);
      return undefined;
    }

    const requests = [
      user.isAuthenticated ? registrationsApi.mine(item.id).catch(() => null) : Promise.resolve(null),
      user.isAuthenticated ? registrationsApi.list(item.id).catch(() => []) : Promise.resolve([]),
      user.isAuthenticated ? contentApi.access(item.id).catch(() => null) : Promise.resolve(null),
      registrationsApi.summary(item.id).catch(() => null)
    ];

    Promise.all(requests)
      .then(([mine, eventRegistrations, access, summary]) => {
        setRegistration(mine);
        setRegistrations(eventRegistrations);
        setEventAccess(access);
        setCapacitySummary(summary);
      })
      .catch(() => {
        setRegistration(null);
        setCapacitySummary(null);
      });
  }, [item?.id, item?.type, user.isAuthenticated, user.permissions]);

  useEffect(() => {
    if (item?.type !== "EVENT") {
      setCalendarHref("");
      return undefined;
    }

    const blob = new Blob([createCalendarContent(item)], { type: "text/calendar;charset=utf-8" });
    const objectUrl = URL.createObjectURL(blob);
    setCalendarHref(objectUrl);

    return () => {
      URL.revokeObjectURL(objectUrl);
    };
  }, [item]);

  if (error) {
    return <MenuLayout active={type === "EVENT" ? "/react/events" : "/react/news"} isAuthenticated={user.isAuthenticated} contentClassName="menu-content-flat"><p className="status-message status-message-error">{error}</p></MenuLayout>;
  }

  if (!item) {
    return <MenuLayout active={type === "EVENT" ? "/react/events" : "/react/news"} isAuthenticated={user.isAuthenticated} contentClassName="menu-content-flat"><p className="muted">Henter indhold...</p></MenuLayout>;
  }

  const canEditContent = hasPermission(user, permissions.contentEdit) || Boolean(eventAccess?.canEditEvent);
  const canViewRegistrations = user.isAuthenticated;
  const isEvent = item.type === "EVENT";
  const hasImage = Boolean(item.pictureUrl);
  const registrationsPath = `/react/events/${item.slug}/registrations`;
  const editPath = `/react/admin/${type === "EVENT" ? "events" : "news"}/${item.id}/edit`;
  const calendarFileName = `${item.slug || "begivenhed"}.ics`;
  const registrationOpen = isRegistrationOpen(item);
  const maxParticipants = capacitySummary?.maxParticipants ?? item.maxParticipants;
  const confirmedParticipants = capacitySummary?.confirmedParticipants ?? confirmedRegistrationCount(registrations);
  const isCapacityReached = maxParticipants != null && (capacitySummary?.isFull ?? confirmedParticipants >= maxParticipants);
  const canPayForEvent = isEvent
    && registrationOpen
    && user.isAuthenticated
    && Boolean(registration)
    && registration.registered === false
    && countsTowardsCapacity(registration);
  const paymentCompleted = isEvent
    && user.isAuthenticated
    && Boolean(registration)
    && registration.registered === true
    && countsTowardsCapacity(registration);
  const paymentOfferLinks = (item.paymentOffers ?? item.PaymentOffers ?? [])
    .filter((offer) => offer.isActive !== false && offer.paymentUrl)
    .map((offer) => ({
      id: `payment-offer-${offer.id}`,
      label: offer.linkLabel || "Betalingslink",
      url: offer.paymentUrl,
      type: "PAYMENT"
    }));
  const visibleLinks = [...(item.links ?? []), ...paymentOfferLinks].filter((link) => {
    const isPaymentLink = String(link.type ?? "").toUpperCase() === "PAYMENT";
    return !isPaymentLink || canPayForEvent || paymentCompleted;
  });

  async function unregisterCurrentUser() {
    if (!registration || isRemovingRegistration) {
      return;
    }

    setIsRemovingRegistration(true);
    setError("");
    try {
      await registrationsApi.unregister(item.id);
      setRegistration(null);
      const [nextRegistrations, nextSummary] = await Promise.all([
        registrationsApi.list(item.id).catch(() => []),
        registrationsApi.summary(item.id).catch(() => null)
      ]);
      setRegistrations(nextRegistrations);
      setCapacitySummary(nextSummary);
      setShowUnregisterConfirmation(false);
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setIsRemovingRegistration(false);
    }
  }

  async function toggleRegistration() {
    if (registration) {
      setShowUnregisterConfirmation(true);
      return;
    }

    setError("");
    try {
      const next = await registrationsApi.register(item.id, { registrationType: "ATTENDEE", responseText: "" });
      setRegistration(next);
      setRegistrations((current) => {
        const withoutCurrentUser = current.filter((eventRegistration) => eventRegistration.userId !== next.userId);
        return [...withoutCurrentUser, next];
      });
      setCapacitySummary(await registrationsApi.summary(item.id).catch(() => null));
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  return (
    <MenuLayout active={item.type === "EVENT" ? "/react/events" : "/react/news"} isAuthenticated={user.isAuthenticated} contentClassName="menu-content-flat">
      <article className="menu-detail-card">
        <div className="menu-detail-hero">
          <div>
            <p className="menu-section-title">{isEvent ? "Begivenheder" : "Nyheder"}</p>
            <p className="menu-panel-lead">{isEvent ? "Se selve eventet og læs mere om programmet." : "Læs nyheden fra foreningen."}</p>
          </div>
          {isEvent && !registrationOpen && <p className="menu-detail-status menu-detail-status-closed">Arrangementet er afsluttet</p>}
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
                {maxParticipants != null && <div><strong>Pladser:</strong> {confirmedParticipants}/{maxParticipants}</div>}
                {isCapacityReached && <div className="event-capacity-message">Der er ikke flere ledige pladser. Du kan skrive dig på venteliste.</div>}
              </div>
            )}
            {isEvent && (
              <div className="menu-detail-bottom-bar">
                <div className="menu-attendee-preview" aria-hidden="true">
                  {registrations.filter((eventRegistration) => !isWaitlisted(eventRegistration)).slice(0, 4).map((eventRegistration) => (
                    <span className="menu-registration-avatar" title={attendeeName(eventRegistration)} key={eventRegistration.id}>{attendeeInitials(eventRegistration)}</span>
                  ))}
                </div>
                <div className="menu-detail-footer-actions">
                  {registrationOpen && user.isAuthenticated ? (
                    <button className={`menu-attend-button menu-attend-button-primary ${registration ? "is-active" : ""}`} type="button" onClick={toggleRegistration}>
                      {registration ? <Trash2 size={16} /> : <UserPlus size={16} />}
                      {registration ? (isWaitlisted(registration) ? "Afmeld venteliste" : "Afmeld") : (isCapacityReached ? "Venteliste" : "Tilmeld")}
                    </button>
                  ) : registrationOpen ? (
                    <a className="menu-attend-button menu-attend-button-primary" href={`/Identity/Account/Login?ReturnUrl=${encodeURIComponent(window.location.pathname)}`}>
                      <LogIn size={16} />
                      {isCapacityReached ? "Venteliste" : "Tilmeld"}
                    </a>
                  ) : null}
                  {canViewRegistrations && (
                    <Link className="menu-attend-button" href={registrationsPath}>
                      <Users size={16} />
                      Tilmeldte
                    </Link>
                  )}
                  {canEditContent && (
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
          {visibleLinks.length > 0 && (
            <aside className="menu-detail-side" aria-label="Event links">
            {visibleLinks.map((link) => (
              String(link.type ?? "").toUpperCase() === "PAYMENT" && paymentCompleted ? (
                <div className="menu-detail-side-link menu-detail-side-link-payment menu-detail-side-link-paid" aria-label="Betaling gennemført" key={link.id}>
                  <span>{shortLinkLabel(link.label)}</span>
                  <strong>Betalt</strong>
                </div>
              ) : (
                <a
                  className={`menu-detail-side-link menu-detail-side-link-${link.type?.toLowerCase()}`}
                  href={link.url}
                  target={link.type?.toUpperCase() === "PAYMENT" && link.url?.startsWith("/react/") ? undefined : "_blank"}
                  rel={link.type?.toUpperCase() === "PAYMENT" && link.url?.startsWith("/react/") ? undefined : "noreferrer"}
                  key={link.id}
                >
                  <span>{shortLinkLabel(link.label)}</span>
                </a>
              )
            ))}
            </aside>
          )}
        </div>
      </article>
      {showUnregisterConfirmation && registration && (
        <ConfirmationDialog
          title={isWaitlisted(registration) ? "Afmeld venteliste" : "Afmeld tilmelding"}
          message={isWaitlisted(registration)
            ? "Er du sikker på, at du vil afmelde dig fra ventelisten?"
            : paymentCompleted
              ? <>Er du sikker på, at du vil afmelde din tilmelding?<br /><br /><strong>Bemærk:</strong> Du har allerede betalt for dette arrangement. Hvis du afmelder dig, bliver beløbet ikke automatisk tilbagebetalt.</>
              : "Er du sikker på, at du vil afmelde din tilmelding?"}
          confirmLabel="Ja, afmeld"
          busyLabel="Afmelder..."
          isBusy={isRemovingRegistration}
          onConfirm={unregisterCurrentUser}
          onCancel={() => !isRemovingRegistration && setShowUnregisterConfirmation(false)}
        />
      )}
    </MenuLayout>
  );
}

function shortLinkLabel(label) {
  return (label ?? "Link")
    .replace("-gruppen", "")
    .replace("Linked In", "LinkedIn")
    .trim();
}

function createCalendarContent(item) {
  const startsAt = toIcsDate(item.startDate);
  const endsAt = toIcsDate(item.endDate || item.startDate);
  const body = htmlToText(item.body ?? item.summary ?? "");
  const uid = `gamma-${item.id || item.slug}@gammasite`;
  return [
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
