import { getCsrfToken } from "./api";

const SAFE_METHODS = new Set(["GET", "HEAD", "OPTIONS", "TRACE"]);

async function financeFetch(path, options = {}) {
  const method = (options.method ?? "GET").toUpperCase();
  const headers = { ...(options.headers ?? {}) };
  if (!SAFE_METHODS.has(method) && !headers["X-CSRF-TOKEN"]) {
    headers["X-CSRF-TOKEN"] = await getCsrfToken();
  }
  return fetch(path, {
    credentials: "same-origin",
    ...options,
    method,
    headers,
  });
}

export const financeApi = {
  overview: async (year) => {
    const response = await financeFetch(
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
    const response = await financeFetch("/api/finance/postings", {
      credentials: "same-origin",
      cache: "no-store",
    });

    if (!response.ok) {
      const message =
        response.status === 401
          ? "Du skal være logget ind for at se dine transaktioner."
          : "Dine transaktioner kunne ikke hentes.";
      throw new Error(message);
    }

    return response.json();
  },
  adminOverview: async (year) => {
    const response = await financeFetch(
      `/api/finance/admin/overview?year=${encodeURIComponent(year)}`,
      {
        credentials: "same-origin",
        cache: "no-store",
      },
    );

    if (!response.ok) {
      throw new Error(
        response.status === 403
          ? "Du har ikke rettigheder til at se finansadministrationen."
          : "Finansoversigten kunne ikke hentes.",
      );
    }

    return response.json();
  },
  adminBudgets: async () => {
    const response = await financeFetch("/api/finance/admin/budgets", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) {
      throw new Error(
        response.status === 403
          ? "Du har ikke rettigheder til at redigere budgetter."
          : "Budgetterne kunne ikke hentes.",
      );
    }
    return response.json();
  },
  adminAccounts: async () => {
    const response = await financeFetch("/api/finance/admin/accounts", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) {
      throw new Error(
        response.status === 403
          ? "Du har ikke rettigheder til at redigere kontoplanen."
          : "Kontoplanen kunne ikke hentes.",
      );
    }
    return response.json();
  },
  adminImportHistory: async () => {
    const response = await financeFetch("/api/finance/admin/import/history", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) throw new Error("Importhistorikken kunne ikke hentes.");
    return response.json();
  },
  generateImportPostings: async () => {
    const response = await financeFetch("/api/finance/admin/import/postings", {
      method: "POST",
      credentials: "same-origin",
    });
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Posteringerne kunne ikke dannes.");
    return payload;
  },
  importFinanceCsv: async ({ bankFile, mobilePayFile, syncPostings }) => {
    const formData = new FormData();
    if (bankFile) formData.append("bankFile", bankFile);
    if (mobilePayFile) formData.append("mobilePayFile", mobilePayFile);
    formData.append("syncPostings", String(syncPostings));
    const response = await financeFetch("/api/finance/admin/import", {
      method: "POST",
      credentials: "same-origin",
      body: formData,
    });
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Filerne kunne ikke importeres.");
    return payload;
  },
  validateFinanceCsv: async ({ bankFile, mobilePayFile }) => {
    const formData = new FormData();
    if (bankFile) formData.append("bankFile", bankFile);
    if (mobilePayFile) formData.append("mobilePayFile", mobilePayFile);
    const response = await financeFetch("/api/finance/admin/import/validate", {
      method: "POST",
      credentials: "same-origin",
      body: formData,
    });
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "CSV-filen kunne ikke valideres.");
    return payload;
  },
  adminBudget: async (id) => {
    const response = await financeFetch(
      `/api/finance/admin/budgets/${encodeURIComponent(id)}`,
      {
        credentials: "same-origin",
        cache: "no-store",
      },
    );
    if (!response.ok)
      throw new Error(
        response.status === 404
          ? "Budgetposten blev ikke fundet."
          : "Budgetposten kunne ikke hentes.",
      );
    return response.json();
  },
  createAdminBudget: async (values) => {
    const response = await financeFetch("/api/finance/admin/budgets", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(values),
    });
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Budgetposten kunne ikke oprettes.");
    return payload;
  },
  updateAdminBudget: async (id, values) => {
    const response = await financeFetch(
      `/api/finance/admin/budgets/${encodeURIComponent(id)}`,
      {
        method: "PUT",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(values),
      },
    );
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Budgetposten kunne ikke gemmes.");
    return payload;
  },
  deleteAdminBudget: async (id) => {
    const response = await financeFetch(
      `/api/finance/admin/budgets/${encodeURIComponent(id)}`,
      {
        method: "DELETE",
        credentials: "same-origin",
      },
    );
    if (!response.ok) throw new Error("Budgetposten kunne ikke slettes.");
  },
  adminPostingGroups: async () => {
    const response = await financeFetch("/api/finance/admin/posteringsgrupper", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) throw new Error("Posteringsgrupperne kunne ikke hentes.");
    return response.json();
  },
  adminPostingGroup: async (id) => {
    const response = await financeFetch(
      `/api/finance/admin/posteringsgrupper/${encodeURIComponent(id)}`,
      {
        credentials: "same-origin",
        cache: "no-store",
      },
    );
    if (!response.ok)
      throw new Error(
        response.status === 404
          ? "Posteringsgruppen blev ikke fundet."
          : "Posteringsgruppen kunne ikke hentes.",
      );
    return response.json();
  },
  createAdminPostingGroup: async (values) => {
    const response = await financeFetch("/api/finance/admin/posteringsgrupper", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(values),
    });
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(
        payload?.error || "Posteringsgruppen kunne ikke oprettes.",
      );
    return payload;
  },
  updateAdminPostingGroup: async (id, values) => {
    const response = await financeFetch(
      `/api/finance/admin/posteringsgrupper/${encodeURIComponent(id)}`,
      {
        method: "PUT",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(values),
      },
    );
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Posteringsgruppen kunne ikke gemmes.");
    return payload;
  },
  deleteAdminPostingGroup: async (id) => {
    const response = await financeFetch(
      `/api/finance/admin/posteringsgrupper/${encodeURIComponent(id)}`,
      {
        method: "DELETE",
        credentials: "same-origin",
      },
    );
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(
        payload?.error || "Posteringsgruppen kunne ikke slettes.",
      );
  },
  adminPostings: async (
    year,
    accountId = "",
    bankKey = "",
    mobilePayKey = "",
  ) => {
    const params = new URLSearchParams();
    if (year === null || year === undefined || year === "") {
      params.set("allYears", "true");
    } else {
      params.set("year", String(year));
    }
    if (accountId) params.set("accountId", accountId);
    if (bankKey) params.set("bankKey", bankKey);
    if (mobilePayKey) params.set("mobilePayKey", mobilePayKey);
    const response = await financeFetch(
      `/api/finance/admin/postings?${params.toString()}`,
      {
        credentials: "same-origin",
        cache: "no-store",
      },
    );

    if (!response.ok) {
      throw new Error(
        response.status === 403
          ? "Du har ikke rettigheder til at se finansposteringer."
          : "Posteringerne kunne ikke hentes.",
      );
    }

    return response.json();
  },
  adminPostingYears: async () => {
    const response = await financeFetch("/api/finance/admin/postings/years", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) throw new Error("Posteringsårene kunne ikke hentes.");
    return response.json();
  },
  adminPosting: async (id) => {
    const response = await financeFetch(
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
    const response = await financeFetch("/api/finance/admin/postings/options", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) throw new Error("Valgmulighederne kunne ikke hentes.");
    return response.json();
  },
  createAdminPosting: async (values) => {
    const response = await financeFetch("/api/finance/admin/postings", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(values),
    });
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Posteringen kunne ikke oprettes.");
    return payload;
  },
  updateAdminPosting: async (id, values) => {
    const response = await financeFetch(
      `/api/finance/admin/postings/${encodeURIComponent(id)}`,
      {
        method: "PUT",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(values),
      },
    );
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Posteringen kunne ikke gemmes.");
  },
  duplicateAdminPosting: async (id) => {
    const response = await financeFetch(
      `/api/finance/admin/postings/${encodeURIComponent(id)}/duplicate`,
      { method: "POST", credentials: "same-origin" },
    );
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Posteringen kunne ikke duplikeres.");
    return payload;
  },
  deleteAdminPosting: async (id) => {
    const response = await financeFetch(
      `/api/finance/admin/postings/${encodeURIComponent(id)}`,
      { method: "DELETE", credentials: "same-origin" },
    );
    const payload = response.headers.get("content-type")?.includes("json")
      ? await response.json()
      : null;
    if (!response.ok)
      throw new Error(payload?.error || "Posteringen kunne ikke slettes.");
  },
};
