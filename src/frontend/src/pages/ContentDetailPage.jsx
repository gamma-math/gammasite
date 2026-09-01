import { useEffect, useState } from "react";
import { Edit3, LogIn, Trash2, UserPlus, Users } from "lucide-react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { contentApi, registrationsApi } from "../services/api.js";
import { attendeeAvatarUrl, attendeeName } from "../utils/avatar.js";
import { formatDate } from "../utils/format.js";

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
    return <MenuLayout active={type === "EVENT" ? "/react/events" : "/react/news"}><p className="status-message">{error}</p></MenuLayout>;
  }

  if (!item) {
    return <MenuLayout active={type === "EVENT" ? "/react/events" : "/react/news"}><p className="muted">Henter indhold...</p></MenuLayout>;
  }

  const roles = new Set(user.roles ?? []);
  const isAdmin = roles.has("Admin") || roles.has("ADMIN");
  const isEvent = item.type === "EVENT";
  const registrationsPath = `/react/events/${item.slug}/registrations`;
  const editPath = `/react/admin/events?edit=${item.id}`;

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
    <MenuLayout active={item.type === "EVENT" ? "/react/events" : "/react/news"}>
      <article className="menu-detail-card">
        <div className="menu-detail-hero">
          <div>
            <p className="menu-section-title">{isEvent ? "Begivenheder" : "Nyheder"}</p>
            <small>{isEvent ? formatDate(item.startDate) : formatDate(item.publishedAt)}</small>
            <h1>{item.title}</h1>
          </div>
          {isEvent && (
            user.isAuthenticated ? (
              <button className={`menu-attend-button menu-attend-button-primary ${registration ? "is-active" : ""}`} type="button" onClick={toggleRegistration}>
                {registration ? <Trash2 size={16} /> : <UserPlus size={16} />}
                {registration ? "Afmeld" : "Tilmeld"}
              </button>
            ) : (
              <a className="menu-attend-button menu-attend-button-primary" href={`/Identity/Account/Login?ReturnUrl=${encodeURIComponent(window.location.pathname)}`}>
                <LogIn size={16} />
                Tilmeld
              </a>
            )
          )}
        </div>
        <img className="menu-detail-image" src={item.pictureUrl || `https://picsum.photos/seed/gamma-detail-${item.id}/1400/640`} alt="" />
        <div className="menu-detail-body">
          <div className="menu-detail-copy">
            <p>{item.summary}</p>
            <p>{item.body}</p>
            <div className="menu-detail-meta">
              <span className="tag tag-kind">{isEvent ? "Arrangement" : "Nyhed"}</span>
              {item.location && <span className="tag">{item.location}</span>}
              {(item.tags ?? "").split(",").filter(Boolean).map((tag) => <span className="tag" key={tag}>{tag.trim()}</span>)}
            </div>
            {isEvent && (
              <div className="menu-detail-bottom-bar">
                <div className="menu-attendee-preview" aria-hidden="true">
                  {registrations.slice(0, 4).map((eventRegistration) => (
                    <img src={attendeeAvatarUrl(eventRegistration)} alt={attendeeName(eventRegistration)} key={eventRegistration.id} />
                  ))}
                </div>
                <div className="menu-detail-footer-actions">
                  {user.isAuthenticated && (
                    <Link className="menu-attend-button" href={registrationsPath}>
                      <Users size={16} />
                      Se tilmeldte
                    </Link>
                  )}
                  {isAdmin && (
                    <Link className="menu-attend-button" href={editPath}>
                      <Edit3 size={16} />
                      Rediger
                    </Link>
                  )}
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
