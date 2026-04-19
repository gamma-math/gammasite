/**
 * Shared fetch wrapper for all API calls.
 *
 * - Adds Content-Type: application/json on mutating requests (POST/PUT/PATCH/DELETE).
 *   This is a secondary CSRF mitigation: cross-origin HTML forms cannot set this header,
 *   so any cross-origin mutating request triggers a CORS preflight that the server rejects.
 * - Redirects to the Identity login page on 401.
 * - Throws on other non-2xx responses.
 * - Cookies are sent automatically (same-origin default).
 */
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const method = (init?.method ?? 'GET').toUpperCase();
  const isWrite = method !== 'GET' && method !== 'HEAD';

  const res = await fetch(path, {
    ...init,
    headers: {
      ...(isWrite ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  });

  if (res.status === 401) {
    window.location.href =
      '/Identity/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
    // Never resolves — navigation is in progress
    return new Promise(() => {});
  }

  if (!res.ok) {
    throw new Error(`API error ${res.status}`);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  return res.json() as Promise<T>;
}
