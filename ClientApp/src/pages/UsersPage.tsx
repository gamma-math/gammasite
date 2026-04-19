import { useEffect, useState } from 'react';
import { apiFetch } from '../api/client';
import { useCurrentUser } from '../hooks/useCurrentUser';

// Member-facing shape (visibility-gated)
interface MemberUser {
  id: string;
  navn: string;
  aargang: number;
  beskaeftigelse: string | null;
  email: string | null;
  phoneNumber: string | null;
}

// Admin shape (all fields)
interface AdminUser {
  id: string;
  navn: string;
  aargang: number;
  beskaeftigelse: string;
  status: string;
  email: string;
  phoneNumber: string;
  kontingentDato: string;
  oprettetDato: string;
}

const STATUS_OPTIONS = ['OPRETTET', 'BETALT', 'SKYLDER', 'INAKTIV', 'STUDERENDE'];

function AdminRow({ user, onStatusChange }: { user: AdminUser; onStatusChange: (id: string, status: string) => void }) {
  const [saving, setSaving] = useState(false);

  const handleChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSaving(true);
    try {
      await apiFetch(`/api/users/${user.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ status: e.target.value }),
      });
      onStatusChange(user.id, e.target.value);
    } finally {
      setSaving(false);
    }
  };

  return (
    <tr>
      <td>{user.navn}</td>
      <td>{user.aargang}</td>
      <td>{user.beskaeftigelse}</td>
      <td>
        <select
          className="form-select form-select-sm"
          value={user.status}
          onChange={handleChange}
          disabled={saving}
        >
          {STATUS_OPTIONS.map(s => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </td>
      <td>{user.email}</td>
      <td>{user.phoneNumber}</td>
      <td>{new Date(user.kontingentDato).toLocaleDateString('da-DK')}</td>
      <td>{new Date(user.oprettetDato).toLocaleDateString('da-DK')}</td>
    </tr>
  );
}

function BulkUpdatePanel({ onDone }: { onDone: () => void }) {
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [status, setStatus] = useState('SKYLDER');
  const [result, setResult] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setResult(null);
    try {
      const data = await apiFetch<{ updated: number }>('/api/users/bulk-status', {
        method: 'PATCH',
        body: JSON.stringify({ status, from, to }),
      });
      setResult(`${data.updated} brugere opdateret`);
      onDone();
    } finally {
      setSaving(false);
    }
  };

  return (
    <form className="row g-2 align-items-end mb-4" onSubmit={handleSubmit}>
      <div className="col-auto">
        <label className="form-label">Fra dato</label>
        <input type="date" className="form-control" value={from} onChange={e => setFrom(e.target.value)} required />
      </div>
      <div className="col-auto">
        <label className="form-label">Til dato</label>
        <input type="date" className="form-control" value={to} onChange={e => setTo(e.target.value)} required />
      </div>
      <div className="col-auto">
        <label className="form-label">Nyt status</label>
        <select className="form-select" value={status} onChange={e => setStatus(e.target.value)}>
          {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>
      <div className="col-auto">
        <button className="btn btn-warning" type="submit" disabled={saving}>
          Opdatér masse
        </button>
      </div>
      {result && <div className="col-auto"><span className="text-success">{result}</span></div>}
    </form>
  );
}

export default function UsersPage() {
  const { loading: authLoading, isAdmin } = useCurrentUser();
  const [users, setUsers] = useState<MemberUser[] | AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadUsers = (admin: boolean) => {
    setLoading(true);
    apiFetch<MemberUser[] | AdminUser[]>(admin ? '/api/users/admin' : '/api/users')
      .then(data => { setUsers(data); setLoading(false); })
      .catch(err => { setError((err as Error).message); setLoading(false); });
  };

  useEffect(() => {
    if (!authLoading) loadUsers(isAdmin);
  }, [authLoading, isAdmin]);

  const handleStatusChange = (id: string, newStatus: string) => {
    setUsers(prev =>
      (prev as AdminUser[]).map(u => u.id === id ? { ...u, status: newStatus } : u)
    );
  };

  if (authLoading || loading) return <p>Henter medlemmer...</p>;
  if (error) return <p>Fejl: {error}</p>;

  if (isAdmin) {
    const adminUsers = users as AdminUser[];
    return (
      <>
        <h1>Medlemsoversigt</h1>
        <BulkUpdatePanel onDone={() => loadUsers(true)} />
        <table className="table table-striped table-bordered">
          <thead className="table-dark">
            <tr>
              <th>Navn</th>
              <th>Årgang</th>
              <th>Beskæftigelse</th>
              <th>Status</th>
              <th>Email</th>
              <th>Telefon</th>
              <th>Kontingentdato</th>
              <th>Oprettet</th>
            </tr>
          </thead>
          <tbody>
            {adminUsers.map(user => (
              <AdminRow key={user.id} user={user} onStatusChange={handleStatusChange} />
            ))}
          </tbody>
        </table>
      </>
    );
  }

  const memberUsers = users as MemberUser[];
  return (
    <>
      <h1>Medlemsoversigt</h1>
      <table className="table table-striped table-bordered">
        <thead className="table-dark">
          <tr>
            <th>Navn</th>
            <th>Årgang</th>
            <th>Beskæftigelse</th>
            <th>Email</th>
            <th>Telefon</th>
          </tr>
        </thead>
        <tbody>
          {memberUsers.map(user => (
            <tr key={user.id}>
              <td>{user.navn}</td>
              <td>{user.aargang}</td>
              <td>{user.beskaeftigelse}</td>
              <td>{user.email}</td>
              <td>{user.phoneNumber}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
