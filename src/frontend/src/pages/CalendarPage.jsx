import { useEffect, useState } from "react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { calendarApi } from "../services/api.js";
import { LoginRequired } from "./MembersPage.jsx";

export function CalendarPage({ user }) {
  const [events, setEvents] = useState([]);

  useEffect(() => {
    if (user.isAuthenticated) {
      calendarApi.upcoming().then(setEvents);
    }
  }, [user.isAuthenticated]);

  if (!user.isAuthenticated) {
    return <LoginRequired title="Kalender" />;
  }

  return (
    <MenuLayout active="/react/calendar">
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Kalender</p>
          <h1>Kommende kalenderpunkter</h1>
        </div>
      </div>
      <div className="menu-table-wrap">
        <table className="menu-member-table">
          <thead><tr><th>Navn</th><th>Tidspunkt</th><th>Ugedag</th><th>Sted</th><th>Detaljer</th></tr></thead>
          <tbody>
            {events.map((event) => (
              <tr key={event.id}>
                <td>{event.title}</td>
                <td>{event.startsAt}</td>
                <td>{event.weekday} uge {event.weekNumber}</td>
                <td>{event.location ? <a href={event.mapsUrl}>{event.location}</a> : ""}</td>
                <td>{event.description}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </MenuLayout>
  );
}
