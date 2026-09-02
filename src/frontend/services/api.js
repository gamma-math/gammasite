const CSRF_HEADER = "X-CSRF-TOKEN";
const SAFE_METHODS = new Set(["GET", "HEAD", "OPTIONS", "TRACE"]);

async function getCsrfToken() {
  const response = await fetch("/api/account/csrf-token", {
    credentials: "same-origin",
    cache: "no-store"
  });

  if (!response.ok) {
    throw new Error("Kunne ikke hente sikkerhedstoken. Genindlæs siden og prøv igen.");
  }

  const payload = await response.json();
  if (!payload?.token) {
    throw new Error("Sikkerhedstoken mangler. Genindlæs siden og prøv igen.");
  }

  return payload.token;
}

async function request(path, options = {}) {
  const method = (options.method ?? "GET").toUpperCase();
  const headers = {
    "Content-Type": "application/json",
    ...(options.headers ?? {})
  };

  if (!SAFE_METHODS.has(method) && !headers[CSRF_HEADER]) {
    headers[CSRF_HEADER] = await getCsrfToken();
  }

  const response = await fetch(path, {
    credentials: "same-origin",
    ...options,
    method,
    headers
  });

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get("content-type") ?? "";
  const payload = contentType.includes("application/json") ? await response.json() : await response.text();

  if (!response.ok) {
    if (response.status === 429) {
      const error = new Error("Der er forsøgt for mange gange på kort tid. Vent lidt og prøv igen.");
      error.status = response.status;
      throw error;
    }

    const message = typeof payload === "object"
      ? payload.error ?? flattenValidationErrors(payload.errors) ?? response.statusText
      : response.statusText;
    const error = new Error(message || `HTTP ${response.status}`);
    error.status = response.status;
    throw error;
  }

  return payload;
}

export const meApi = {
  get: () => request("/api/me")
};

export const accountApi = {
  login: (payload) => request("/api/account/login", { method: "POST", body: JSON.stringify(payload) }),
  logout: () => request("/api/account/logout", { method: "POST" }),
  register: (payload) => request("/api/account/register", { method: "POST", body: JSON.stringify(payload) }),
  forgotPassword: (email) => request("/api/account/forgot-password", { method: "POST", body: JSON.stringify({ email }) }),
  resendEmailConfirmation: (email) => request("/api/account/resend-email-confirmation", { method: "POST", body: JSON.stringify({ email }) }),
  profile: () => request("/api/account/profile"),
  updateProfile: (payload) => request("/api/account/profile", { method: "PUT", body: JSON.stringify(payload) }),
  changeEmail: (newEmail) => request("/api/account/change-email", { method: "POST", body: JSON.stringify({ newEmail }) }),
  sendVerificationEmail: () => request("/api/account/send-verification-email", { method: "POST" }),
  changePassword: (payload) => request("/api/account/change-password", { method: "POST", body: JSON.stringify(payload) }),
  deleteAccount: (password) => request("/api/account/delete", { method: "POST", body: JSON.stringify({ password }) })
};

export const contentApi = {
  listPublished: (type, options = {}) => {
    const params = new URLSearchParams();
    if (type) {
      params.set("type", type);
    }
    if (options.frontPage) {
      params.set("frontPage", "true");
    }
    return request(`/api/content${params.toString() ? `?${params}` : ""}`);
  },
  listAdmin: (type) => request(`/api/content/admin${type ? `?type=${encodeURIComponent(type)}` : ""}`),
  getBySlug: (slug) => request(`/api/content/slug/${encodeURIComponent(slug)}`),
  create: (payload) => request("/api/content", { method: "POST", body: JSON.stringify(payload) }),
  update: (id, payload) => request(`/api/content/${id}`, { method: "PUT", body: JSON.stringify(payload) }),
  delete: (id) => request(`/api/content/${id}`, { method: "DELETE" })
};

export const registrationsApi = {
  mine: (contentId) => request(`/api/content/${contentId}/registrations/me`),
  register: (contentId, payload) => request(`/api/content/${contentId}/registrations`, { method: "POST", body: JSON.stringify(payload) }),
  add: (contentId, payload) => request(`/api/content/${contentId}/registrations/admin`, { method: "POST", body: JSON.stringify(payload) }),
  unregister: (contentId) => request(`/api/content/${contentId}/registrations/me`, { method: "DELETE" }),
  list: (contentId) => request(`/api/content/${contentId}/registrations`),
  update: (contentId, registrationId, payload) => request(`/api/content/${contentId}/registrations/${registrationId}`, { method: "PUT", body: JSON.stringify(payload) })
};

export const emailTemplatesApi = {
  list: () => request("/api/email-templates"),
  create: (payload) => request("/api/email-templates", { method: "POST", body: JSON.stringify(payload) }),
  update: (id, payload) => request(`/api/email-templates/${id}`, { method: "PUT", body: JSON.stringify(payload) }),
  delete: (id) => request(`/api/email-templates/${id}`, { method: "DELETE" }),
  preview: (id, values) => request(`/api/email-templates/${id}/preview`, { method: "POST", body: JSON.stringify({ values }) })
};

export const membersApi = {
  list: () => request("/api/members"),
  listAdmin: () => request("/api/members/admin"),
  updateStatus: (id, status) => request(`/api/members/${id}/status`, { method: "PUT", body: JSON.stringify({ status }) }),
  massUpdateStatus: (payload) => request("/api/members/admin/mass-status", { method: "POST", body: JSON.stringify(payload) })
};

export const rolesApi = {
  list: () => request("/api/roles"),
  create: (name) => request("/api/roles", { method: "POST", body: JSON.stringify({ name }) }),
  delete: (id) => request(`/api/roles/${id}`, { method: "DELETE" }),
  members: (id) => request(`/api/roles/${id}/members`),
  updateMembers: (id, payload) => request(`/api/roles/${id}/members`, { method: "PUT", body: JSON.stringify(payload) })
};

export const calendarApi = {
  upcoming: () => request("/api/calendar")
};

export const libraryApi = {
  listing: (path = "") => request(`/api/library${path ? `?path=${encodeURIComponent(path)}` : ""}`)
};

export const paymentsApi = {
  config: () => request("/api/payments/config"),
  products: () => request("/api/payments/products"),
  product: (id) => request(`/api/payments/products/${encodeURIComponent(id)}`),
  startProductCheckout: (product, user) => request(`/api/Stripe/Product?product=${encodeURIComponent(product)}&user=${encodeURIComponent(user)}`, { method: "POST" }),
  startGenericCheckout: (product, price, description, user) => request(`/api/Stripe/Generic?product=${encodeURIComponent(product)}&price=${encodeURIComponent(price)}&description=${encodeURIComponent(description)}&user=${encodeURIComponent(user)}`, { method: "POST" })
};

export const messagesApi = {
  categories: () => request("/api/messages/categories"),
  recipientPreview: (payload) => request("/api/messages/recipient-preview", { method: "POST", body: JSON.stringify(payload) }),
  render: (payload) => request("/api/messages/render", { method: "POST", body: JSON.stringify(payload) }),
  send: (payload) => request("/api/messages/send", { method: "POST", body: JSON.stringify(payload) })
};

function flattenValidationErrors(errors) {
  if (!errors || typeof errors !== "object") {
    return "";
  }

  return Object.values(errors).flat().filter(Boolean).join(" ");
}
