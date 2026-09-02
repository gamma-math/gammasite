import { useEffect, useState } from "react";
import { Plus, Save, Trash2 } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { Link, navigate } from "../routes/navigation.jsx";
import { rolesApi } from "../services/api.js";

export function AdminRolesPage({ isAdmin }) {
  const [roles, setRoles] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    if (isAdmin) {
      load();
    }
  }, [isAdmin]);

  async function load() {
    try {
      setRoles(await rolesApi.list());
    } catch (reason) {
      setError(reason.message);
    }
  }

  async function remove(role) {
    if (isProtectedRole(role)) return;

    try {
      await rolesApi.delete(role.id);
      await load();
    } catch (reason) {
      setError(reason.message);
    }
  }

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/roles" canWrite={false}><p className="status-message status-message-warning">Kun ADMIN kan administrere roller.</p></AdminLayout>;
  }

  return (
    <AdminLayout active="/react/admin/roles" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Roller</p>
          <h1>Rolleoversigt</h1>
        </div>
        <Link className="menu-create-button" href="/react/admin/roles/new">
          <Plus size={16} />
          Opret ny
        </Link>
      </div>

      {error && <p className="status-message status-message-error">{error}</p>}
      <div className="menu-table-wrap">
        <table className="menu-member-table admin-role-table">
          <thead>
            <tr>
              <th>Navn</th>
              <th>ID</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {roles.map((role) => (
              <tr key={role.id}>
                <td>{role.name}</td>
                <td>{role.id}</td>
                <td className="table-actions">
                  <Link className="admin-table-button" href={`/react/admin/roles/${role.id}/edit`}>Opdater</Link>
                  {!isProtectedRole(role) && (
                    <button className="admin-table-button admin-table-button-danger" type="button" onClick={() => remove(role)}>
                      <Trash2 size={14} />
                      Slet
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {roles.length === 0 && (
              <tr>
                <td colSpan="3">Ingen roller endnu.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
}

export function AdminRolesEditorPage({ isAdmin, roleId }) {
  const [roles, setRoles] = useState([]);
  const [name, setName] = useState("");
  const [membership, setMembership] = useState(null);
  const [addSearch, setAddSearch] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const isNew = roleId === null;

  useEffect(() => {
    if (!isAdmin) return;

    rolesApi.list()
      .then((result) => {
        setRoles(result);
        const role = result.find((item) => item.id === roleId);
        if (role) {
          setName(role.name);
        }
      })
      .catch((reason) => setError(reason.message));
  }, [isAdmin, roleId]);

  useEffect(() => {
    if (!isAdmin || isNew) return;

    rolesApi.members(roleId)
      .then(setMembership)
      .catch((reason) => setError(reason.message));
  }, [isAdmin, isNew, roleId]);

  async function create(event) {
    event.preventDefault();
    setError("");
    setMessage("");

    try {
      const role = await rolesApi.create(name);
      navigate(`/react/admin/roles/${role.id}/edit`);
    } catch (reason) {
      setError(reason.message);
    }
  }

  async function updateMembership(event) {
    event.preventDefault();
    setError("");
    setMessage("");

    try {
      const form = new FormData(event.currentTarget);
      const result = await rolesApi.updateMembers(roleId, {
        addIds: form.getAll("addIds"),
        deleteIds: form.getAll("deleteIds")
      });
      setMembership(result);
      setAddSearch("");
      setMessage("Rollen er opdateret.");
    } catch (reason) {
      setError(reason.message);
    }
  }

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/roles" canWrite={false}><p className="status-message status-message-warning">Kun ADMIN kan administrere roller.</p></AdminLayout>;
  }

  const selectedRole = roles.find((role) => role.id === roleId);
  const filteredNonMembers = filterUsers(membership?.nonMembers, addSearch);

  return (
    <AdminLayout active="/react/admin/roles" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Roller</p>
          <h1>{isNew ? "Opret ny rolle" : `Opdater ${selectedRole?.name ?? "rolle"}`}</h1>
        </div>
        <Link className="frontpage-button frontpage-button-secondary" href="/react/admin/roles">Tilbage til oversigt</Link>
      </div>

      {message && <p className="status-message status-message-success">{message}</p>}
      {error && <p className="status-message status-message-error">{error}</p>}

      {isNew ? (
        <form className="menu-editor-form admin-role-editor" onSubmit={create}>
          <label className="admin-field">
            <span>Navn</span>
            <input value={name} onChange={(event) => setName(event.target.value)} required />
          </label>
          <div className="menu-editor-actions">
            <button className="profile-button" type="submit"><Save size={16} /> Gem</button>
          </div>
        </form>
      ) : (
        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar"><strong>Medlemmer i {selectedRole?.name ?? "rollen"}</strong></div>
          {membership ? (
            <form className="role-membership-form" onSubmit={updateMembership}>
              <RoleCheckboxes title="Tilføj" name="addIds" users={filteredNonMembers} search={addSearch} onSearchChange={setAddSearch} />
              <RoleCheckboxes title="Fjern" name="deleteIds" users={membership.members} />
              <button className="profile-button" type="submit"><Save size={16} /> Gem</button>
            </form>
          ) : (
            <p className="muted admin-role-loading">Henter medlemmer...</p>
          )}
        </section>
      )}
    </AdminLayout>
  );
}

function RoleCheckboxes({ title, name, users, search, onSearchChange }) {
  return (
    <fieldset className="role-fieldset">
      <legend>{title}</legend>
      {onSearchChange && (
        <label className="role-search">
          <span>Søg</span>
          <input type="search" placeholder="Navn eller email" value={search} onChange={(event) => onSearchChange(event.target.value)} />
        </label>
      )}
      {(users ?? []).length === 0 && <p className="muted">Ingen brugere i denne gruppe.</p>}
      {(users ?? []).map((user) => (
        <label className="profile-checkbox role-user-option" key={user.id}>
          <input type="checkbox" name={name} value={user.id} />
          <span className="role-user-name">{user.name || user.email}</span>
          {user.email && user.name && <span className="role-user-email">{user.email}</span>}
        </label>
      ))}
    </fieldset>
  );
}

function isProtectedRole(role) {
  return role?.name?.toUpperCase() === "ADMIN";
}

function filterUsers(users = [], search = "") {
  const term = search.trim().toLowerCase();
  if (!term) return users;
  return users.filter((user) => `${user.name ?? ""} ${user.email ?? ""}`.toLowerCase().includes(term));
}
