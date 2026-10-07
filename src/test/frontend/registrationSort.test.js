import test from "node:test";
import assert from "node:assert/strict";
import { sortRegistrations } from "../../frontend/utils/registrationSort.js";

test("sorts organizers first and then by name", () => {
  const sorted = sortRegistrations([
    { registrationType: "ATTENDEE", userName: "Zelda" },
    { registrationType: "ORGANIZER", userName: "Bo" },
    { registrationType: "ORGANIZER", userName: "Anna" }
  ]);

  assert.deepEqual(sorted.map((registration) => registration.userName), ["Anna", "Bo", "Zelda"]);
});
