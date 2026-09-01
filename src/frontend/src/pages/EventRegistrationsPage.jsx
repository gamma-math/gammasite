import { useEffect, useState } from "react";
import { LogIn } from "lucide-react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { contentApi, registrationsApi } from "../services/api.js";
import { attendeeAvatarUrl } from "../utils/avatar.js";
import { formatDate } from "../utils/format.js";

export function EventRegistrationsPage({ slug, user }) {
  const [item, setItem] = useState(null);
  const [registrations, setRegistrations] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    contentApi.getBySlug(slug)
      .then(async (content) => {
        if (!active) {
          return;
        }

        setItem(content);
        if (user.isAuthenticated) {
          setRegistrations(await registrationsApi.list(content.id));
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message);
        }
      });

    return () => {
      active = false;
    };
  }, [slug, user.isAuthenticated]);

  if (error) {
    return <MenuLayout active="/react/events"><p className="status-message">{error}</p></MenuLayout>;
  }

  if (!item) {
    return <MenuLayout active="/react/events"><p className="muted">Henter tilmeldte...</p></MenuLayout>;
  }

  if (!user.isAuthenticated) {
    return (
      <MenuLayout active="/react/events">
        <div className="menu-panel-header">
          <div>
            <p className="menu-section-title">Begivenheder</p>
            <h1>{item.title}</h1>
            <p className="menu-panel-lead">Log ind for at se tilmeldte.</p>
          </div>
          <a className="menu-attend-button menu-attend-button-primary" href={`/Identity/Account/Login?ReturnUrl=${encodeURIComponent(window.location.pathname)}`}>
            <LogIn size={16} />
            Login
          </a>
        </div>
      </MenuLayout>
    );
  }

  return (
    <MenuLayout active="/react/events">
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Begivenheder</p>
          <p className="menu-detail-kicker">{formatDate(item.startDate)}</p>
          <h1>{item.title}</h1>
          <p className="menu-panel-lead">Se tilmeldte</p>
        </div>
      </div>

      <div className="menu-table-wrap menu-registration-card">
        <table className="menu-member-table">
          <thead>
            <tr>
              <th>Billede</th>
              <th>Navn</th>
              <th>Rolle</th>
            </tr>
          </thead>
          <tbody>
            {registrations.map((registration) => (
              <tr key={registration.id}>
                <td>
                  <img className="menu-registration-avatar" src={attendeeAvatarUrl(registration)} alt="" />
                </td>
                <td>{registration.userName || registration.email || "Tilmeldt medlem"}</td>
                <td><span className="menu-role-badge menu-role-badge-attendee">{registration.registrationType === "ATTENDEE" ? "Deltager" : registration.registrationType}</span></td>
              </tr>
            ))}
            {registrations.length === 0 && (
              <tr>
                <td colSpan="3">Der er ingen tilmeldte endnu.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </MenuLayout>
  );
}
