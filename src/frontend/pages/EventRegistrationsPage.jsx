import { useEffect, useState } from "react";
import { LogIn, Plus } from "lucide-react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { contentApi, membersApi, registrationsApi } from "../services/api.js";
import { attendeeInitials, attendeeName } from "../utils/avatar.js";
import { formatDate } from "../utils/format.js";

const registrationTypes = [
  { value: "ATTENDEE", label: "Deltager" },
  { value: "ORGANIZER", label: "Arrangør" },
  { value: "INTERESTED", label: "Interesseret" },
  { value: "DECLINED", label: "Afmeldt" }
];

export function EventRegistrationsPage({ slug, user }) {
  const [item, setItem] = useState(null);
  const [registrations, setRegistrations] = useState([]);
  const [members, setMembers] = useState([]);
  const [memberSearch, setMemberSearch] = useState("");
  const [addForm, setAddForm] = useState({ userId: "", registrationType: "ATTENDEE", registered: true });
  const [error, setError] = useState("");
  const roles = new Set(user.roles ?? []);
  const isAdmin = roles.has("Admin") || roles.has("ADMIN");

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
        if (isAdmin) {
          setMembers(await membersApi.listAdmin());
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
  }, [slug, user.isAuthenticated, isAdmin]);

  async function updateRegistration(registration, changes) {
    const payload = {
      registrationType: registration.registrationType,
      registered: registration.registered,
      responseText: registration.responseText,
      ...changes
    };
    const updated = await registrationsApi.update(item.id, registration.id, payload);
    setRegistrations((current) => current.map((entry) => entry.id === updated.id ? updated : entry));
  }

  async function addRegistration(event) {
    event.preventDefault();
    const added = await registrationsApi.add(item.id, addForm);
    setRegistrations((current) => {
      const withoutExisting = current.filter((entry) => entry.id !== added.id && entry.userId !== added.userId);
      return [...withoutExisting, added].sort((left, right) => attendeeName(left).localeCompare(attendeeName(right), "da-DK"));
    });
    setAddForm({ userId: "", registrationType: "ATTENDEE", registered: true });
    setMemberSearch("");
  }

  if (error) {
    return <MenuLayout active="/react/events" isAuthenticated={user.isAuthenticated}><p className="status-message status-message-error">{error}</p></MenuLayout>;
  }

  if (!item) {
    return <MenuLayout active="/react/events" isAuthenticated={user.isAuthenticated}><p className="muted">Henter tilmeldte...</p></MenuLayout>;
  }

  if (!user.isAuthenticated) {
    return (
      <MenuLayout active="/react/events" isAuthenticated={user.isAuthenticated}>
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

  const availableMembers = members.filter((member) => !registrations.some((registration) => registration.userId === member.id));
  const selectedMember = members.find((member) => member.id === addForm.userId);
  const memberSearchTerm = memberSearch.toLowerCase();
  const matchingMembers = availableMembers
    .filter((member) => [member.name, member.email].some((value) => String(value ?? "").toLowerCase().includes(memberSearchTerm)))
    .slice(0, 8);

  return (
    <MenuLayout active="/react/events" isAuthenticated={user.isAuthenticated}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Begivenheder</p>
          <p className="menu-detail-kicker">{formatDate(item.startDate)}</p>
          <h1>{item.title}</h1>
          <p className="menu-panel-lead">Se tilmeldte</p>
        </div>
        <Link className="frontpage-button frontpage-button-secondary" href={`/react/events/${item.slug}`}>
          Begivenheden
        </Link>
      </div>

      {isAdmin && (
        <form className="menu-registration-add" onSubmit={addRegistration}>
          <label className="admin-field menu-member-combobox">
            <span>Tilføj deltager</span>
            <input value={selectedMember ? selectedMember.name || selectedMember.email : memberSearch} onChange={(event) => {
              setAddForm((current) => ({ ...current, userId: "" }));
              setMemberSearch(event.target.value);
            }} placeholder="Søg medlem" required={!addForm.userId} />
            {!addForm.userId && memberSearch && (
              <div className="menu-member-options">
                {matchingMembers.map((member) => (
                  <button className="menu-member-option" type="button" key={member.id} onClick={() => {
                    setAddForm((current) => ({ ...current, userId: member.id }));
                    setMemberSearch(member.name || member.email || "");
                  }}>
                    <span className="menu-registration-avatar">{initialsFromMember(member)}</span>
                    <span>
                      <strong>{member.name || "Uden navn"}</strong>
                      <small>{member.email}</small>
                    </span>
                  </button>
                ))}
                {matchingMembers.length === 0 && <p className="menu-member-option-empty">Ingen medlemmer fundet.</p>}
              </div>
            )}
          </label>
          <label className="admin-field">
            <span>Rolle</span>
            <select value={addForm.registrationType} onChange={(event) => setAddForm((current) => ({ ...current, registrationType: event.target.value }))}>
              {registrationTypes.map((type) => <option value={type.value} key={type.value}>{type.label}</option>)}
            </select>
          </label>
          <label className="menu-registration-check menu-registration-add-check">
            <input type="checkbox" checked={addForm.registered} onChange={(event) => setAddForm((current) => ({ ...current, registered: event.target.checked }))} />
            <span>Registered</span>
          </label>
          <button className="menu-attend-button menu-attend-button-primary" type="submit" disabled={!addForm.userId}>
            <Plus size={16} />
            Tilføj
          </button>
        </form>
      )}

      <div className="menu-table-wrap menu-registration-card">
        <table className="menu-member-table menu-registration-table">
          <thead>
            <tr>
              <th>Navn</th>
              <th>Rolle</th>
              <th>Registered</th>
            </tr>
          </thead>
          <tbody>
            {registrations.map((registration) => (
              <tr key={registration.id}>
                <td>
                  <div className="menu-registration-person">
                    <span className="menu-registration-avatar">{attendeeInitials(registration)}</span>
                    <span>{attendeeName(registration)}</span>
                  </div>
                </td>
                <td>
                  {isAdmin ? (
                    <select className="menu-registration-select" value={registration.registrationType} onChange={(event) => updateRegistration(registration, { registrationType: event.target.value })}>
                      {registrationTypes.map((type) => <option value={type.value} key={type.value}>{type.label}</option>)}
                    </select>
                  ) : (
                    <span className="menu-role-badge menu-role-badge-attendee">{registrationLabel(registration.registrationType)}</span>
                  )}
                </td>
                <td>
                  {isAdmin ? (
                    <label className="menu-registration-check">
                      <input type="checkbox" checked={registration.registered} onChange={(event) => updateRegistration(registration, { registered: event.target.checked })} />
                      <span>{registration.registered ? "Ja" : "Nej"}</span>
                    </label>
                  ) : (
                    <span>{registration.registered ? "Ja" : "Nej"}</span>
                  )}
                </td>
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

function registrationLabel(value) {
  return registrationTypes.find((type) => type.value === value)?.label ?? value;
}

function initialsFromMember(member) {
  const firstName = String(member.name || member.email || "?").trim().split(/\s+/)[0] ?? "?";
  return firstName.slice(0, 2).toUpperCase();
}
