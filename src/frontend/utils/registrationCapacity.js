const capacityTypes = new Set(["ATTENDEE", "ORGANIZER"]);

export function countsTowardsCapacity(registration) {
  return capacityTypes.has(registration?.registrationType ?? registration?.RegistrationType);
}

export function confirmedRegistrationCount(registrations = []) {
  return registrations.filter(countsTowardsCapacity).length;
}

export function isWaitlisted(registration) {
  return (registration?.registrationType ?? registration?.RegistrationType) === "WAITLISTED";
}
