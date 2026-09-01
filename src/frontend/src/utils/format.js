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
    startDate: "",
    endDate: "",
    location: "",
    links: []
  };
}
