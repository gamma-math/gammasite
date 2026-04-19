import { useEffect, useState } from 'react';
import { apiFetch } from '../api/client';

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
    apiFetch<CurrentUser>('/api/auth/me')
      .then(data => {
        setUser(data);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, []);

  const isAdmin = user?.roles.includes('Admin') ?? false;

  return { user, loading, isAdmin };
}
