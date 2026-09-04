/**
 * Returns the display name for a registration or its linked user.
 */
export function attendeeName(registration) {
  return registration?.userName || registration?.email || "Tilmeldt medlem";
}

/**
 * Builds initials for attendee avatars.
 */
export function attendeeInitials(registration) {
  const firstName = attendeeName(registration).trim().split(/\s+/)[0] ?? "?";
  return firstName.slice(0, 2).toUpperCase();
}
