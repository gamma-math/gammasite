import { useEffect, useState } from 'react';

interface CurrentUser {
  id: string;
  email: string;
  name: string;
  status: string;
  roles: string[];
}

export function useCurrentUser() {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Use plain fetch rather than apiFetch: a 401 here means "not logged in",
    // not an error — unauthenticated users should see the app, just without
    // the auth-gated UI. apiFetch would redirect them to the login page instead.
    fetch('/api/auth/me')
      .then(res => (res.ok ? res.json() as Promise<CurrentUser> : null))
      .then(data => {
        setUser(data);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, []);

  return { user, loading, roles: user?.roles ?? [] };
}
