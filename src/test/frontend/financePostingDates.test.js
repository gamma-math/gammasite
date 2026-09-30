import test from "node:test";
import assert from "node:assert/strict";
import { effectivePostingDate } from "../../frontend/utils/financePostingDates.js";

test("keeps an explicit posting date", () => {
  assert.equal(effectivePostingDate("2026-09-30", "2026-09-29"), "2026-09-30");
});

test("falls back to the transaction date", () => {
  assert.equal(effectivePostingDate("", "2026-09-29"), "2026-09-29");
});

test("returns an empty value when both dates are missing", () => {
  assert.equal(effectivePostingDate("", ""), "");
});
