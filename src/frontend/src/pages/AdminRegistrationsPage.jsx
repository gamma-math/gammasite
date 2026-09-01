import { useEffect, useState } from "react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { registrationsApi } from "../services/api.js";

export function AdminRegistrationsPage({ contentId, isReadAdmin }) {
  const [registrations, setRegistrations] = useState([]);

  useEffect(() => {
    if (isReadAdmin) {
      registrationsApi.list(contentId).then(setRegistrations);
    }
  }, [contentId, isReadAdmin]);

  if (!isReadAdmin) {
    return (
      <AdminLayout active="/react/admin/events/1/registrations" canWrite={false}>
        <p className="status-message">Du har ikke adgang til tilmeldte.</p>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout active="/react/admin/events/1/registrations" canWrite={false}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Begivenheder</p>
          <h1>Tilmeldte</h1>
        </div>
      </div>
      <div className="menu-table-wrap">
        <table className="menu-member-table">
          <thead>
            <tr>
              <th>Navn</th>
              <th>Email</th>
              <th>Rolle</th>
              <th>Svar</th>
            </tr>
          </thead>
          <tbody>
            {registrations.map((registration) => (
              <tr key={registration.id}>
                <td>{registration.userName}</td>
                <td>{registration.email}</td>
                <td><span className="menu-role-badge menu-role-badge-attendee">{registration.registrationType}</span></td>
                <td>{registration.responseText}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
}
