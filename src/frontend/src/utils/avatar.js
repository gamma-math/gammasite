export function attendeeName(registration) {
  return registration?.userName || registration?.email || "Tilmeldt medlem";
}

export function attendeeAvatarUrl(registration) {
  const name = encodeURIComponent(attendeeName(registration));
  const seed = encodeURIComponent(registration?.email || registration?.userId || attendeeName(registration));
  return `https://api.dicebear.com/9.x/initials/svg?seed=${seed}&backgroundType=gradientLinear&fontWeight=700&initials=${name}`;
}
