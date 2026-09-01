import { useEffect, useMemo, useState } from "react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { membersApi } from "../services/api.js";
import { SearchToolbar } from "./MembersPage.jsx";

const statuses = ["OPRETTET", "BETALT", "SKYLDER", "INAKTIV", "STUDERENDE"];

export function AdminMembersPage({ isAdmin }) {
  const [members, setMembers] = useState([]);
  const [search, setSearch] = useState("");
  const [mass, setMass] = useState({ from: today(), to: today(1), status: "SKYLDER" });
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (isAdmin) {
      load();
    }
  }, [isAdmin]);

  async function load() {
    setMembers(await membersApi.listAdmin());
  }

  async function updateStatus(member, status) {
    const updated = await membersApi.updateStatus(member.id, status);
    setMembers((current) => current.map((item) => item.id === member.id ? updated : item));
  }

  async function massUpdate(event) {
    event.preventDefault();
    const result = await membersApi.massUpdateStatus(mass);
    setMessage(`${result.updated} medlemmer blev opdateret.`);
    await load();
  }

  const filtered = useMemo(() => {
    const term = search.toLowerCase();
    return members.filter((member) => [member.name, member.email, member.status].some((value) => (value ?? "").toLowerCase().includes(term)));
  }, [members, search]);

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/users" canWrite={false}><p className="status-message">Kun ADMIN kan administrere medlemmer.</p></AdminLayout>;
  }

  return (
    <AdminLayout active="/react/admin/users" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Medlemmer</p>
          <h1>Medlemsadministration</h1>
        </div>
      </div>
      <form className="admin-mass-card" onSubmit={massUpdate}>
        {message && <p className="status-message">{message}</p>}
        <label className="admin-field"><span>Kontingentdato fra</span><input type="date" value={mass.from} onChange={(event) => setMass({ ...mass, from: event.target.value })} /></label>
        <label className="admin-field"><span>Kontingentdato til</span><input type="date" value={mass.to} onChange={(event) => setMass({ ...mass, to: event.target.value })} /></label>
        <label className="admin-field"><span>Status</span><select value={mass.status} onChange={(event) => setMass({ ...mass, status: event.target.value })}>{statuses.map((status) => <option key={status}>{status}</option>)}</select></label>
        <button className="profile-button" type="submit">Skift status</button>
      </form>
      <SearchToolbar search={search} setSearch={setSearch} />
      <div className="menu-table-wrap">
        <table className="menu-member-table">
          <thead>
            <tr>
              <th>Navn</th>
              <th>Årgang</th>
              <th>Email</th>
              <th>Telefon</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((member) => (
              <tr key={member.id}>
                <td>{member.name}</td>
                <td>{member.graduationYear || ""}</td>
                <td>{member.email}</td>
                <td>{member.phoneNumber}</td>
                <td>
                  <select className="menu-member-status-select" value={member.status} onChange={(event) => updateStatus(member, event.target.value)}>
                    {statuses.map((status) => <option key={status}>{status}</option>)}
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
}

function today(offset = 0) {
  const date = new Date();
  date.setDate(date.getDate() + offset);
  return date.toISOString().slice(0, 10);
}
