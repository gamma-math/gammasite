import test from "node:test";
import assert from "node:assert/strict";
import { currentReturnUrl, loginHref } from "../../frontend/utils/authNavigation.js";

test("builds a login link that returns to the current route", () => {
  const location = { pathname: "/events/faglig-aften", search: "?from=menu", hash: "#tilmeld" };

  assert.equal(currentReturnUrl(location), "/events/faglig-aften?from=menu#tilmeld");
  assert.equal(
    loginHref(undefined, location),
    "/account/login?returnUrl=%2Fevents%2Ffaglig-aften%3Ffrom%3Dmenu%23tilmeld"
  );
});

test("keeps an explicitly supplied return route", () => {
  const location = { pathname: "/account/login", search: "", hash: "" };

  assert.equal(loginHref("/pay", location), "/account/login?returnUrl=%2Fpay");
});
