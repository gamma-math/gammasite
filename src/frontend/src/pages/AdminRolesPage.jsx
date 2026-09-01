import { useEffect, useState } from "react";
import { Plus, Save, Trash2 } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { rolesApi } from "../services/api.js";

export function AdminRolesPage({ isAdmin }) {
  const [roles, setRoles] = useState([]);
  const [name, setName] = useState("");
  const [selected, setSelected] = useState(null);
  const [membership, setMembership] = useState(null);

  useEffect(() => {
    if (isAdmin) {
      load();
    }
  }, [isAdmin]);

  async function load() {
    setRoles(await rolesApi.list());
  }

  async function create(event) {
    event.preventDefault();
    await rolesApi.create(name);
    setName("");
    await load();
  }

  async function remove(role) {
    await rolesApi.delete(role.id);
    if (selected?.id === role.id) {
      setSelected(null);
      setMembership(null);
    }
    await load();
  }

  async function selectRole(role) {
    setSelected(role);
    setMembership(await rolesApi.members(role.id));
  }

  async function updateMembership(event) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setMembership(await rolesApi.updateMembers(selected.id, {
      addIds: form.getAll("addIds"),
      deleteIds: form.getAll("deleteIds")
    }));
  }

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/roles" canWrite={false}><p className="status-message">Kun ADMIN kan administrere roller.</p></AdminLayout>;
  }

  return (
    <AdminLayout active="/react/admin/roles" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Roller</p>
          <h1>Rolleoversigt</h1>
        </div>
      </div>
      <div className="admin-split">
        <section>
          <form className="admin-inline-form" onSubmit={create}>
            <label className="admin-field"><span>Ny rolle</span><input value={name} onChange={(event) => setName(event.target.value)} required /></label>
            <button className="menu-create-button" type="submit"><Plus size={16} /> Opret rolle</button>
          </form>
          <div className="menu-table-wrap">
            <table className="menu-member-table">
              <thead><tr><th>ID</th><th>Navn</th><th></th></tr></thead>
              <tbody>
                {roles.map((role) => (
                  <tr key={role.id}>
                    <td>{role.id}</td>
                    <td>{role.name}</td>
                    <td className="table-actions">
                      <button className="admin-table-button" type="button" onClick={() => selectRole(role)}>Opdatér</button>
                      <button className="admin-table-button admin-table-button-danger" type="button" onClick={() => remove(role)}><Trash2 size={14} /> Slet</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar"><strong>{selected ? `Medlemmer i ${selected.name}` : "Vælg en rolle"}</strong></div>
          {membership && (
            <form className="role-membership-form" onSubmit={updateMembership}>
              <RoleCheckboxes title="Tilføj" name="addIds" users={membership.nonMembers} />
              <RoleCheckboxes title="Fjern" name="deleteIds" users={membership.members} />
              <button className="profile-button" type="submit"><Save size={16} /> Gem</button>
            </form>
          )}
        </section>
      </div>
    </AdminLayout>
  );
}

function RoleCheckboxes({ title, name, users }) {
  return (
    <fieldset className="role-fieldset">
      <legend>{title}</legend>
      {(users ?? []).length === 0 && <p className="muted">Ingen brugere i denne gruppe.</p>}
      {(users ?? []).map((user) => (
        <label className="profile-checkbox" key={user.id}>
          <input type="checkbox" name={name} value={user.id} />
          {user.name || user.email}
        </label>
      ))}
    </fieldset>
  );
}
