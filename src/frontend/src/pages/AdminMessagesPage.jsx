import { useEffect, useState } from "react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { messagesApi } from "../services/api.js";

export function AdminMessagesPage({ isAdmin }) {
  const [categories, setCategories] = useState({ statuses: [], roles: [] });
  const [selectedStatuses, setSelectedStatuses] = useState([]);
  const [selectedRoles, setSelectedRoles] = useState([]);
  const [preview, setPreview] = useState(null);

  useEffect(() => {
    if (isAdmin) {
      messagesApi.categories().then(setCategories);
    }
  }, [isAdmin]);

  async function updatePreview(nextStatuses = selectedStatuses, nextRoles = selectedRoles) {
    setPreview(await messagesApi.recipientPreview({ statuses: nextStatuses, roles: nextRoles }));
  }

  function toggle(value, list, setter, otherList, isRole) {
    const next = list.includes(value) ? list.filter((item) => item !== value) : [...list, value];
    setter(next);
    updatePreview(isRole ? selectedStatuses : next, isRole ? next : otherList);
  }

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/messages" canWrite={false}><p className="status-message">Kun ADMIN kan se beskedværktøjet.</p></AdminLayout>;
  }

  return (
    <AdminLayout active="/react/admin/messages" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Beskeder</p>
          <h1>Målgrupper</h1>
          <p className="muted">Afsendelse er bevidst ikke migreret endnu. Denne side viser kun målgruppe-preview.</p>
        </div>
      </div>
      <div className="admin-message-form">
        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar"><strong>Statusser</strong></div>
          <div className="choice-grid">
            {categories.statuses.map((status) => (
              <label className="profile-checkbox" key={status}>
                <input type="checkbox" checked={selectedStatuses.includes(status)} onChange={() => toggle(status, selectedStatuses, setSelectedStatuses, selectedRoles, false)} />
                {status}
              </label>
            ))}
          </div>
        </section>
        <section className="admin-editor-shell">
          <div className="admin-editor-toolbar"><strong>Roller</strong></div>
          <div className="choice-grid">
            {categories.roles.map((role) => (
              <label className="profile-checkbox" key={role}>
                <input type="checkbox" checked={selectedRoles.includes(role)} onChange={() => toggle(role, selectedRoles, setSelectedRoles, selectedStatuses, true)} />
                {role}
              </label>
            ))}
          </div>
        </section>
        {preview && (
          <div className="frontpage-membership-card">
            <h2>{preview.recipientCount} modtagere</h2>
            <p className="muted">{preview.emailCount} med email, {preview.smsCount} med telefonnummer.</p>
          </div>
        )}
      </div>
    </AdminLayout>
  );
}
