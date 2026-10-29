export function getRegistrationDeadline(item) {
  return item?.registrationDeadline || item?.endDate || item?.startDate || null;
}

export function isEventEnded(item, now = Date.now()) {
  const eventEnd = item?.endDate || item?.startDate;
  return !eventEnd || new Date(eventEnd).getTime() <= now;
}

export function isRegistrationClosedByDeadline(item, now = Date.now()) {
  return Boolean(item?.registrationDeadline)
    && !isEventEnded(item, now)
    && new Date(item.registrationDeadline).getTime() <= now;
}

export function isRegistrationOpen(item, now = Date.now()) {
  const deadline = getRegistrationDeadline(item);
  return item?.status === "PUBLISHED"
    && Boolean(deadline && new Date(deadline).getTime() > now);
}
