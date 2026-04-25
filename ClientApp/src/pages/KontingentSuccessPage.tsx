import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { apiFetch } from '../api/client';

export default function KontingentSuccessPage() {
  const [searchParams] = useSearchParams();
  const session = searchParams.get('session');

  const [status, setStatus] = useState<'loading' | 'success' | 'failed'>('loading');

  useEffect(() => {
    if (!session) {
      setStatus('failed');
      return;
    }

    apiFetch<{ success: boolean }>(`/api/pay/kontingent-success?session=${encodeURIComponent(session)}`)
      .then(data => setStatus(data.success ? 'success' : 'failed'))
      .catch(() => setStatus('failed'));
  }, [session]);

  if (status === 'loading') {
    return <p>Bekræfter betaling...</p>;
  }

  if (status === 'success') {
    return (
      <>
        <h1>Kontingent betalt</h1>
        <p>Dit kontingent er nu registreret. Velkommen!</p>
        <Link className="btn btn-secondary" to="/app/pay">Til oversigten</Link>
      </>
    );
  }

  return (
    <>
      <h1>Betaling kunne ikke bekræftes</h1>
      <p>Vi kunne ikke bekræfte din betaling. Kontakt venligst administrator.</p>
      <Link className="btn btn-secondary" to="/app/pay">Til oversigten</Link>
    </>
  );
}
