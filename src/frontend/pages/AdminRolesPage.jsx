import { useEffect, useState } from "react";
import { Plus, Save, Trash2, X } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { Link, navigate } from "../routes/navigation.jsx";
import { rolesApi } from "../services/api.js";

/**
 * Admin role overview for creating, editing, and deleting roles.
 */
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

/**
 * Admin role editor for assigning members and permissions to a role.
 */
export function AdminRolesEditorPage({ isAdmin, roleId }) {
  const [roles, setRoles] = useState([]);
  const [name, setName] = useState("");
  const [membership, setMembership] = useState(null);
  const [selectedMemberIds, setSelectedMemberIds] = useState([]);
  const [memberSearch, setMemberSearch] = useState("");
  const [permissionState, setPermissionState] = useState(null);
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
      .then((result) => {
        setMembership(result);
        setSelectedMemberIds((result.members ?? []).map((member) => member.id));
      })
      .catch((reason) => setError(reason.message));

    rolesApi.permissions(roleId)
      .then(setPermissionState)
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

  async function updateRole(event) {
    event.preventDefault();
    setError("");
    setMessage("");

    try {
      const originalMemberIds = new Set((membership.members ?? []).map((member) => member.id));
      const result = await rolesApi.updateMembers(roleId, {
        addIds: selectedMemberIds.filter((id) => !originalMemberIds.has(id)),
        deleteIds: (membership.members ?? [])
          .filter((member) => !selectedMemberIds.includes(member.id))
          .map((member) => member.id)
      });
      setMembership(result);
      setSelectedMemberIds((result.members ?? []).map((member) => member.id));
      setMemberSearch("");

      if (permissionState) {
        const permissionResult = await rolesApi.updatePermissions(
          roleId,
          permissionState.permissions
            .filter((permission) => permission.enabled || isAdminRequiredPermission(roleId, selectedRole, permission.code))
            .map((permission) => permission.code),
        );
        setPermissionState(permissionResult);
      }

      setMessage("Rollen er opdateret.");
    } catch (reason) {
      setError(reason.message);
    }
  }

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/roles" canWrite={false}><p className="status-message status-message-warning">Kun ADMIN kan administrere roller.</p></AdminLayout>;
  }

  const selectedRole = roles.find((role) => role.id === roleId);
  const allRoleUsers = [...(membership?.members ?? []), ...(membership?.nonMembers ?? [])]
    .filter((user, index, users) => users.findIndex((item) => item.id === user.id) === index);
  const selectedMembers = allRoleUsers.filter((member) => selectedMemberIds.includes(member.id));
  const availableMembers = allRoleUsers.filter((member) => !selectedMemberIds.includes(member.id));
  const filteredAvailableMembers = filterUsers(availableMembers, memberSearch);

  return (
    <AdminLayout active="/react/admin/roles" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Roller</p>
          <h1>{isNew ? "Opret ny rolle" : `Opdater ${selectedRole?.name ?? "rolle"}`}</h1>
        </div>
        <Link className="frontpage-button frontpage-button-secondary" href="/react/admin/roles">Tilbage til oversigt</Link>
      </div>

      {isNew ? (
        <form className="menu-editor-form admin-role-editor" onSubmit={create}>
          <label className="admin-field">
            <span>Navn</span>
            <input value={name} onChange={(event) => setName(event.target.value)} required />
          </label>
          <div className="menu-editor-actions">
            <button className="profile-button" type="submit"><Save size={16} /> Gem</button>
          </div>
          {message && <p className="status-message status-message-success">{message}</p>}
          {error && <p className="status-message status-message-error">{error}</p>}
        </form>
      ) : (
        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar"><strong>Rediger {selectedRole?.name ?? "rolle"}</strong></div>
          {membership ? (
            <form className="role-membership-form" onSubmit={updateRole}>
              <section className="role-members-section">
                <div className="role-section-heading">
                  <div>
                    <p className="admin-section-kicker">Medlemmer</p>
                  </div>
                  <span className="role-count">{selectedMembers.length} valgt</span>
                </div>

                <details className={`admin-multi-select ${selectedMembers.length ? "has-selection" : ""}`}>
                  <summary>
                    <span>Medlemmer</span>
                    <strong>{selectedMembers.length ? `${selectedMembers.length} medlemmer valgt` : "Vælg medlemmer"}</strong>
                  </summary>
                  <div className="admin-multi-select-menu role-member-picker">
                    <label className="admin-multi-select-search">
                      <span>Søg efter navn eller email</span>
                      <input type="search" value={memberSearch} onChange={(event) => setMemberSearch(event.target.value)} placeholder="Søg" />
                    </label>
                    {filteredAvailableMembers.length === 0 && <p className="muted">Ingen flere medlemmer fundet.</p>}
                    {filteredAvailableMembers.map((member) => (
                      <label className="profile-checkbox" key={member.id}>
                        <input
                          type="checkbox"
                          checked={selectedMemberIds.includes(member.id)}
                          onChange={() => toggleSelectedMember(member.id, setSelectedMemberIds)}
                        />
                        <span>{member.name || member.email}</span>
                        {member.email && member.name && <small>{member.email}</small>}
                      </label>
                    ))}
                  </div>
                </details>

                <div className="role-selected-members">
                  <div className="role-selected-members-header">
                    <span>Valgte medlemmer</span>
                  </div>
                  {selectedMembers.length === 0 && <p className="muted">Ingen medlemmer er valgt.</p>}
                  {selectedMembers.map((member) => (
                    <div className="role-selected-member" key={member.id}>
                      <div>
                        <strong>{member.name || member.email}</strong>
                        {member.email && <span>{member.email}</span>}
                      </div>
                      <button type="button" className="role-remove-member" onClick={() => toggleSelectedMember(member.id, setSelectedMemberIds)}>
                        <X size={16} />
                        Fjern
                      </button>
                    </div>
                  ))}
                </div>
              </section>

              {permissionState && (
                <section className="role-permissions-section">
                  <div className="role-section-heading">
                    <div>
                      <p className="admin-section-kicker">Permissions</p>
                    </div>
                    <span className="role-count">{permissionState.permissions.filter((permission) => permission.enabled || isAdminRequiredPermission(roleId, selectedRole, permission.code)).length} aktive</span>
                  </div>
                  <div className="menu-table-wrap">
                    <table className="menu-member-table">
                      <thead>
                        <tr>
                          <th>Permission</th>
                          <th>Beskrivelse</th>
                          <th>Aktiv</th>
                        </tr>
                      </thead>
                      <tbody>
                        {permissionState.permissions.map((permission) => {
                          const isLockedForAdmin = isAdminRequiredPermission(roleId, selectedRole, permission.code);
                          return (
                            <tr key={permission.code}>
                              <td>
                                <strong>{permissionLabel(permission.code)}</strong>
                                <small className="role-permission-code">{permission.code}</small>
                              </td>
                              <td>{permission.description}</td>
                              <td>
                                <input
                                  type="checkbox"
                                  checked={isLockedForAdmin || permission.enabled}
                                  disabled={isLockedForAdmin}
                                  aria-label={`Aktiver ${permissionLabel(permission.code)}`}
                                  onChange={() => setPermissionState((current) => ({
                                    ...current,
                                    permissions: current.permissions.map((entry) => entry.code === permission.code ? { ...entry, enabled: !entry.enabled } : entry)
                                  }))}
                                />
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </section>
              )}

              <button className="profile-button" type="submit"><Save size={16} /> Gem</button>
              {message && <p className="status-message status-message-success">{message}</p>}
              {error && <p className="status-message status-message-error">{error}</p>}
            </form>
          ) : (
            <>
              <p className="muted admin-role-loading">Henter medlemmer...</p>
              {error && <p className="status-message status-message-error">{error}</p>}
            </>
          )}
        </section>
      )}
    </AdminLayout>
  );
}

function toggleSelectedMember(id, setSelectedMemberIds) {
  setSelectedMemberIds((current) => current.includes(id) ? current.filter((value) => value !== id) : [...current, id]);
}

function isProtectedRole(role) {
  return role?.name?.toUpperCase() === "ADMIN";
}

function filterUsers(users = [], search = "") {
  const term = search.trim().toLowerCase();
  if (!term) return users;
  return users.filter((user) => `${user.name ?? ""} ${user.email ?? ""}`.toLowerCase().includes(term));
}

function permissionLabel(code) {
  return {
    "content.edit": "Events og nyheder",
    "registrations.edit": "Tilmeldinger",
    "finance.view.all": "Se alle finanser",
    "finance.edit.all": "Rediger alle finanser",
    "messages.edit": "Beskeder",
    "email_templates.edit": "Beskedskabeloner",
    "roles.edit": "Roller og permissions"
  }[code] ?? code;
}

const ADMIN_REQUIRED_PERMISSION_CODES = new Set([
  "content.edit",
  "registrations.edit",
  "messages.edit",
  "roles.edit"
]);

function isAdminRequiredPermission(roleId, role, code) {
  const isAdmin = roleId === "role-admin" || role?.name?.toUpperCase() === "ADMIN";
  return isAdmin && ADMIN_REQUIRED_PERMISSION_CODES.has(code);
}
