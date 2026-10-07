export function isRegistrationOpen(item) {
  if (item?.status !== "PUBLISHED") return false;
  const deadline = item.endDate || item.startDate;
  return Boolean(deadline && new Date(deadline).getTime() > Date.now());
}
