import { useEffect, useMemo, useState } from "react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { membersApi } from "../services/api.js";

export function MembersPage({ user }) {
  const [members, setMembers] = useState([]);
  const [search, setSearch] = useState("");

  useEffect(() => {
    if (user.isAuthenticated) {
      membersApi.list().then(setMembers);
    }
  }, [user.isAuthenticated]);

  const filtered = useMemo(() => {
    const term = search.toLowerCase();
    return members.filter((member) => [member.name, member.occupation, member.email].some((value) => (value ?? "").toLowerCase().includes(term)));
  }, [members, search]);

  if (!user.isAuthenticated) {
    return <LoginRequired title="Medlemmer" />;
  }

  return (
    <MenuLayout active="/react/members">
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Medlemmer</p>
          <p className="menu-panel-lead menu-panel-lead-inline">Her kan du få overblik over foreningens medlemmer og deres kontaktoplysninger, når de har valgt synlighed.</p>
        </div>
      </div>
      <SearchToolbar search={search} setSearch={setSearch} />
      <div className="menu-table-wrap">
        <table className="menu-member-table">
          <thead>
            <tr>
              <th>Navn</th>
              <th>Årgang</th>
              <th>Beskæftigelse</th>
              <th>Email</th>
              <th>Telefon</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((member) => (
              <tr key={member.id}>
                <td>{member.name}</td>
                <td>{member.graduationYear || ""}</td>
                <td>{member.occupation || "Skjult"}</td>
                <td>{member.email || "Skjult"}</td>
                <td>{member.phoneNumber || "Skjult"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </MenuLayout>
  );
}

export function LoginRequired({ title }) {
  return (
    <main className="frontpage-main">
      <section className="page-shell frontpage-section">
        <h1>{title}</h1>
        <p className="muted">Du skal være logget ind for at se denne side.</p>
        <a className="frontpage-button frontpage-button-primary" href="/Identity/Account/Login">Login</a>
      </section>
    </main>
  );
}

export function SearchToolbar({ search, setSearch }) {
  return (
    <div className="menu-table-toolbar">
      <label className="menu-table-search">
        <span>Søg</span>
        <input type="search" placeholder="Navn" value={search} onChange={(event) => setSearch(event.target.value)} />
      </label>
    </div>
  );
}
