import test from "node:test";
import assert from "node:assert/strict";
import { confirmedRegistrationCount, isWaitlisted } from "../../frontend/utils/registrationCapacity.js";

test("counts attendees and organizers towards capacity even before payment is marked", () => {
  const registrations = [
    { registrationType: "ATTENDEE", registered: true },
    { registrationType: "ORGANIZER", registered: true },
    { registrationType: "WAITLISTED", registered: true },
    { registrationType: "INTERESTED", registered: true },
    { registrationType: "ATTENDEE", registered: false }
  ];

  assert.equal(confirmedRegistrationCount(registrations), 3);
  assert.equal(isWaitlisted(registrations[2]), true);
  assert.equal(isWaitlisted(registrations[0]), false);
});
