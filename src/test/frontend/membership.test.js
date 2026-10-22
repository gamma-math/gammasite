import test from "node:test";
import assert from "node:assert/strict";
import { hasActiveSubscription, hasPaidMembership } from "../../frontend/utils/membership.js";

test("recognizes active subscription statuses", () => {
  assert.equal(hasActiveSubscription([{ status: "active" }]), true);
  assert.equal(hasActiveSubscription([{ status: "canceled" }]), false);
});

test("recognizes paid and student membership statuses", () => {
  assert.equal(hasPaidMembership({ status: "BETALT" }), true);
  assert.equal(hasPaidMembership({ status: "STUDERENDE" }), true);
  assert.equal(hasPaidMembership({ status: "SKYLDER" }), false);
});
