import { useEffect, useState } from 'react';
import { apiFetch } from '../api/client';

interface CalendarEvent {
  uid: string;
  summary: string;
  startUtc: string;
  startLocal: string;
  weekday: string;
  weekOfYear: string;
  location?: string;
  locationMapsUrl?: string;
  description?: string;
  isPast: boolean;
}

export default function CalendarPage() {
  const [upcoming, setUpcoming] = useState<CalendarEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiFetch<CalendarEvent[]>('/api/calendar')
      .then(data => {
        setUpcoming(data.filter(e => !e.isPast));
        setLoading(false);
      })
      .catch(err => {
        setError((err as Error).message);
        setLoading(false);
      });
  }, []);

  if (loading) return <p>Henter kalender...</p>;
  if (error) return <p>Fejl: {error}</p>;

  return (
    <>
      <h1>Kalender</h1>
      <table className="table table-striped table-bordered">
        <thead className="table-dark">
          <tr>
            <th>Navn</th>
            <th>Tidspunkt</th>
            <th>Ugedag</th>
            <th>Sted</th>
            <th>Detaljer</th>
          </tr>
        </thead>
        <tbody>
          {upcoming.length === 0 && (
            <tr>
              <td colSpan={5}>Ingen kommende begivenheder.</td>
            </tr>
          )}
          {upcoming.map(event => (
            <tr key={event.uid}>
              <td>{event.summary}</td>
              <td>{event.startLocal}</td>
              <td>
                {event.weekday} (uge {event.weekOfYear})
              </td>
              <td>
                {event.location ? (
                  <a href={event.locationMapsUrl} target="_blank" rel="noreferrer">
                    {event.location}
                  </a>
                ) : null}
              </td>
              <td>{event.description}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
