import { attendeeName } from "./avatar.js";

const registrationRoleOrder = {
  ORGANIZER: 0,
  ATTENDEE: 1,
  INTERESTED: 2,
  DECLINED: 3
};

export function sortRegistrations(registrations = []) {
  return [...registrations].sort((left, right) => {
    const roleOrder = (registrationRoleOrder[left.registrationType] ?? 99)
      - (registrationRoleOrder[right.registrationType] ?? 99);

    return roleOrder || attendeeName(left).localeCompare(attendeeName(right), "da-DK");
  });
}
