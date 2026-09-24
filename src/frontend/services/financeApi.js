export const financeApi = {
  overview: async (year) => {
    const response = await fetch(
      `/api/finance/overview?year=${encodeURIComponent(year)}`,
      {
        credentials: "same-origin",
        cache: "no-store",
      },
    );

    if (!response.ok) {
      const message =
        response.status === 401
          ? "Du skal være logget ind for at se finansoversigten."
          : "Finansoversigten kunne ikke hentes.";
      throw new Error(message);
    }

    return response.json();
  },
  postings: async () => {
    const response = await fetch("/api/finance/postings", {
      credentials: "same-origin",
      cache: "no-store",
    });

    if (!response.ok) {
      const message =
        response.status === 401
          ? "Du skal være logget ind for at se dine posteringer."
          : "Dine posteringer kunne ikke hentes.";
      throw new Error(message);
    }

    return response.json();
  },
  adminOverview: async (year) => {
    const response = await fetch(
      `/api/finance/admin/overview?year=${encodeURIComponent(year)}`,
      {
        credentials: "same-origin",
        cache: "no-store",
      },
    );

    if (!response.ok) {
      throw new Error(
        response.status === 403
          ? "Du har ikke adgang til Finans admin."
          : "Finansoversigten kunne ikke hentes.",
      );
    }

    return response.json();
  },
  adminPostings: async (
    year,
    accountId = "",
    bankKey = "",
    mobilePayKey = "",
  ) => {
    const params = new URLSearchParams({ year: String(year) });
    if (accountId) params.set("accountId", accountId);
    if (bankKey) params.set("bankKey", bankKey);
    if (mobilePayKey) params.set("mobilePayKey", mobilePayKey);
    const response = await fetch(
      `/api/finance/admin/postings?${params.toString()}`,
      {
        credentials: "same-origin",
        cache: "no-store",
      },
    );

    if (!response.ok) {
      throw new Error(
        response.status === 403
          ? "Du har ikke adgang til Finans admin."
          : "Posteringerne kunne ikke hentes.",
      );
    }

    return response.json();
  },
  adminPosting: async (id) => {
    const response = await fetch(
      `/api/finance/admin/postings/${encodeURIComponent(id)}`,
      { credentials: "same-origin", cache: "no-store" },
    );
    if (!response.ok)
      throw new Error(
        response.status === 404
          ? "Posteringen blev ikke fundet."
          : "Posteringen kunne ikke hentes.",
      );
    return response.json();
  },
  adminPostingOptions: async () => {
    const response = await fetch("/api/finance/admin/postings/options", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) throw new Error("Valgmulighederne kunne ikke hentes.");
    return response.json();
  },
  updateAdminPosting: async (id, values) => {
    const response = await fetch(
      `/api/finance/admin/postings/${encodeURIComponent(id)}`,
      {
        method: "PUT",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(values),
      },
    );
    if (!response.ok) throw new Error("Posteringen kunne ikke gemmes.");
  },
};
