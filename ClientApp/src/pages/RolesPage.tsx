import { useEffect, useState } from 'react';
import { apiFetch } from '../api/client';

interface Role {
  id: string;
  name: string;
}

interface RoleUser {
  id: string;
  navn: string;
  email: string;
}

interface RoleMembers {
  roleId: string;
  roleName: string;
  members: RoleUser[];
  nonMembers: RoleUser[];
}

// ── Membership editor (shown inline when "Opdatér" is clicked) ─────────────

function MemberEditor({ roleId, onClose }: { roleId: string; onClose: () => void }) {
  const [data, setData] = useState<RoleMembers | null>(null);
  const [addIds, setAddIds] = useState<Set<string>>(new Set());
  const [removeIds, setRemoveIds] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiFetch<RoleMembers>(`/api/roles/${roleId}/members`)
      .then(setData)
      .catch(err => setError((err as Error).message));
  }, [roleId]);

  const toggle = (set: Set<string>, id: string): Set<string> => {
    const next = new Set(set);
    if (next.has(id)) next.delete(id); else next.add(id);
    return next;
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      await apiFetch(`/api/roles/${roleId}/members`, {
        method: 'PATCH',
        body: JSON.stringify({
          addIds: [...addIds],
          removeIds: [...removeIds],
        }),
      });
      onClose();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  if (!data) return <tr><td colSpan={4}><em>Henter...</em></td></tr>;

  return (
    <tr>
      <td colSpan={4}>
        <div className="border rounded p-3 mb-2">
          <h6>Tilføj til <strong>{data.roleName}</strong></h6>
          {data.nonMembers.length === 0
            ? <p className="text-muted">Alle brugere er allerede medlemmer</p>
            : <table className="table table-sm table-bordered mb-3">
                <tbody>
                  {data.nonMembers.map(u => (
                    <tr key={u.id}>
                      <td>{u.navn}, {u.email}</td>
                      <td className="text-center">
                        <input
                          type="checkbox"
                          checked={addIds.has(u.id)}
                          onChange={() => setAddIds(toggle(addIds, u.id))}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
          }

          <h6>Fjern fra <strong>{data.roleName}</strong></h6>
          {data.members.length === 0
            ? <p className="text-muted">Ingen brugere er medlemmer</p>
            : <table className="table table-sm table-bordered mb-3">
                <tbody>
                  {data.members.map(u => (
                    <tr key={u.id}>
                      <td>{u.navn}, {u.email}</td>
                      <td className="text-center">
                        <input
                          type="checkbox"
                          checked={removeIds.has(u.id)}
                          onChange={() => setRemoveIds(toggle(removeIds, u.id))}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
          }

          {error && <p className="text-danger">{error}</p>}

          <button className="btn btn-primary btn-sm me-2" onClick={handleSave} disabled={saving}>
            Gem
          </button>
          <button className="btn btn-secondary btn-sm" onClick={onClose} disabled={saving}>
            Annullér
          </button>
        </div>
      </td>
    </tr>
  );
}

// ── Main page ──────────────────────────────────────────────────────────────

export default function RolesPage() {
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    apiFetch<Role[]>('/api/roles')
      .then(data => { setRoles(data); setLoading(false); })
      .catch(err => { setError((err as Error).message); setLoading(false); });
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) return;
    setCreating(true);
    try {
      const role = await apiFetch<Role>('/api/roles', {
        method: 'POST',
        body: JSON.stringify({ name: newName.trim() }),
      });
      setRoles(prev => [...prev, role].sort((a, b) => a.name.localeCompare(b.name)));
      setNewName('');
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = async (id: string) => {
    await apiFetch(`/api/roles/${id}`, { method: 'DELETE' });
    setRoles(prev => prev.filter(r => r.id !== id));
    if (expandedId === id) setExpandedId(null);
  };

  const toggleExpand = (id: string) =>
    setExpandedId(prev => (prev === id ? null : id));

  if (loading) return <p>Henter roller...</p>;
  if (error) return <p>Fejl: {error}</p>;

  return (
    <>
      <h1>Rolleoversigt</h1>

      <form className="row g-2 align-items-end mb-4" onSubmit={handleCreate}>
        <div className="col-auto">
          <label className="form-label">Opret rolle</label>
          <input
            className="form-control"
            placeholder="Rollenavn"
            value={newName}
            onChange={e => setNewName(e.target.value)}
            required
          />
        </div>
        <div className="col-auto">
          <button className="btn btn-secondary" type="submit" disabled={creating}>
            Opret
          </button>
        </div>
      </form>

      <table className="table table-striped table-bordered">
        <thead className="table-dark">
          <tr>
            <th>ID</th>
            <th>Navn</th>
            <th>Opdatér</th>
            <th>Slet</th>
          </tr>
        </thead>
        <tbody>
          {roles.map(role => (
            <>
              <tr key={role.id}>
                <td><small className="text-muted">{role.id}</small></td>
                <td>{role.name}</td>
                <td>
                  <button
                    className="btn btn-sm btn-primary"
                    onClick={() => toggleExpand(role.id)}
                  >
                    {expandedId === role.id ? 'Luk' : 'Opdatér'}
                  </button>
                </td>
                <td>
                  <div className="dropdown">
                    <button
                      className="btn btn-sm btn-danger dropdown-toggle"
                      data-bs-toggle="dropdown"
                    >
                      Slet
                    </button>
                    <ul className="dropdown-menu">
                      <li>
                        <button
                          className="dropdown-item"
                          onClick={() => handleDelete(role.id)}
                        >
                          Bekræft
                        </button>
                      </li>
                    </ul>
                  </div>
                </td>
              </tr>
              {expandedId === role.id && (
                <MemberEditor
                  key={`editor-${role.id}`}
                  roleId={role.id}
                  onClose={() => setExpandedId(null)}
                />
              )}
            </>
          ))}
        </tbody>
      </table>
    </>
  );
}
