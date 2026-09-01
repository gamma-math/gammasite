async function request(path, options = {}) {
  const response = await fetch(path, {
    credentials: "same-origin",
    headers: {
      "Content-Type": "application/json",
      ...(options.headers ?? {})
    },
    ...options
  });

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get("content-type") ?? "";
  const payload = contentType.includes("application/json") ? await response.json() : await response.text();

  if (!response.ok) {
    const message = typeof payload === "object" ? payload.error ?? response.statusText : response.statusText;
    throw new Error(message);
  }

  return payload;
}

export const meApi = {
  get: () => request("/api/me")
};

export const contentApi = {
  listPublished: (type) => request(`/api/content${type ? `?type=${encodeURIComponent(type)}` : ""}`),
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
  recipientPreview: (payload) => request("/api/messages/recipient-preview", { method: "POST", body: JSON.stringify(payload) })
};
