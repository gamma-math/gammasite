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

export function formatShortDate(value) {
  if (!value) {
    return "Ingen dato";
  }

  return new Intl.DateTimeFormat("da-DK", {
    day: "numeric",
    month: "long"
  }).format(new Date(value));
}

export function contentMetaLabel(item) {
  const date = item.type === "EVENT" ? formatShortDate(item.startDate) : formatShortDate(item.publishedAt);
  return item.type === "EVENT" && item.location ? `${date} · ${item.location}` : date;
}

export function detailPath(item) {
  return item.type === "EVENT" ? `/react/events/${item.slug}` : `/react/news/${item.slug}`;
}

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
