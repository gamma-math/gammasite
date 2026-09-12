export const financeApi = {
  overview: async (year) => {
    const response = await fetch(`/api/finance/overview?year=${encodeURIComponent(year)}`, {
      credentials: "same-origin",
      cache: "no-store"
    });

    if (!response.ok) {
      const message = response.status === 401
        ? "Du skal være logget ind for at se finansoversigten."
        : "Finansoversigten kunne ikke hentes.";
      throw new Error(message);
    }

    return response.json();
  },
  postings: async () => {
    const response = await fetch("/api/finance/postings", {
      credentials: "same-origin",
      cache: "no-store"
    });

    if (!response.ok) {
      const message = response.status === 401
        ? "Du skal være logget ind for at se dine posteringer."
        : "Dine posteringer kunne ikke hentes.";
      throw new Error(message);
    }

    return response.json();
  }
};
