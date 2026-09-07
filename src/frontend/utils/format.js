/**
 * Formats a full Danish date/time value for visible UI text.
 */
export function formatDate(value) {
  if (!value) {
    return "Ingen dato";
  }

  return new Intl.DateTimeFormat("da-DK", {
    day: "numeric",
    month: "long",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(new Date(value));
}

/**
 * Formats a compact Danish date for cards and metadata.
 */
export function formatShortDate(value) {
  if (!value) {
    return "Ingen dato";
  }

  return new Intl.DateTimeFormat("da-DK", {
    day: "numeric",
    month: "long"
  }).format(new Date(value));
}

/**
 * Builds the small date/location metadata label shown on content cards.
 */
export function contentMetaLabel(item) {
  const date = item.type === "EVENT" ? formatShortDate(item.startDate) : formatShortDate(item.publishedAt);
  return item.type === "EVENT" && item.location ? `${date} · ${item.location}` : date;
}

/**
 * Returns the canonical React detail URL for an event or news item.
 */
export function detailPath(item) {
  return item.type === "EVENT" ? `/react/events/${item.slug}` : `/react/news/${item.slug}`;
}

/**
 * Creates an empty admin editor model for new events or news.
 */
export function emptyContent(type) {
  return {
    title: "",
    slug: "",
    summary: "",
    body: "",
    pictureUrl: "",
    tags: "",
    type,
    status: "DRAFT",
    showOnFrontPage: true,
    startDate: "",
    endDate: "",
    publishedAt: "",
    location: "",
    links: []
  };
}
