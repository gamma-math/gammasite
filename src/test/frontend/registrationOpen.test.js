import test from "node:test";
import assert from "node:assert/strict";
import { isEventEnded, isEventOngoing, isRegistrationClosedByDeadline, isRegistrationOpen } from "../../frontend/utils/registrationOpen.js";

const now = Date.parse("2026-10-29T12:00:00Z");

test("uses the explicit registration deadline instead of the event end", () => {
  const event = {
    status: "PUBLISHED",
    startDate: "2026-12-12T14:30:00Z",
    endDate: "2026-12-13T03:00:00Z",
    registrationDeadline: "2026-12-07T23:59:00Z"
  };

  assert.equal(isRegistrationOpen(event, now), true);
  assert.equal(isEventEnded(event, now), false);
});

test("reports a closed registration deadline before the event ends", () => {
  const event = {
    status: "PUBLISHED",
    startDate: "2026-10-30T14:30:00Z",
    endDate: "2026-10-30T20:00:00Z",
    registrationDeadline: "2026-10-29T11:00:00Z"
  };

  assert.equal(isRegistrationOpen(event, now), false);
  assert.equal(isRegistrationClosedByDeadline(event, now), true);
  assert.equal(isEventEnded(event, now), false);
});

test("reports an event as ongoing between its inclusive start and end times", () => {
  const event = {
    startDate: "2026-10-29T12:00:00Z",
    endDate: "2026-10-29T14:00:00Z"
  };

  assert.equal(isEventOngoing(event, now), true);
  assert.equal(isEventOngoing(event, Date.parse("2026-10-29T14:00:01Z")), false);
});

test("closes registration at the exact registration deadline", () => {
  const event = {
    status: "PUBLISHED",
    startDate: "2026-10-30T14:30:00Z",
    endDate: "2026-10-30T20:00:00Z",
    registrationDeadline: "2026-10-29T12:00:00Z"
  };

  assert.equal(isRegistrationOpen(event, now), false);
  assert.equal(isRegistrationClosedByDeadline(event, now), true);
});

test("keeps the existing end-date fallback when no registration deadline is set", () => {
  const event = {
    status: "PUBLISHED",
    startDate: "2026-10-28T14:30:00Z",
    endDate: "2026-10-29T11:00:00Z"
  };

  assert.equal(isRegistrationOpen(event, now), false);
  assert.equal(isRegistrationClosedByDeadline(event, now), false);
  assert.equal(isEventEnded(event, now), true);
});
