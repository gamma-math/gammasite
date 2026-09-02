export function attendeeName(registration) {
  return registration?.userName || registration?.email || "Tilmeldt medlem";
}

export function attendeeInitials(registration) {
  const firstName = attendeeName(registration).trim().split(/\s+/)[0] ?? "?";
  return firstName.slice(0, 2).toUpperCase();
}
