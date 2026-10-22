import test from "node:test";
import assert from "node:assert/strict";
import { currentReturnUrl, loginHref } from "../../frontend/utils/authNavigation.js";

test("builds a login link that returns to the current route", () => {
  const location = { pathname: "/react/events/faglig-aften", search: "?from=menu", hash: "#tilmeld" };

  assert.equal(currentReturnUrl(location), "/react/events/faglig-aften?from=menu#tilmeld");
  assert.equal(
    loginHref(undefined, location),
    "/react/account/login?returnUrl=%2Freact%2Fevents%2Ffaglig-aften%3Ffrom%3Dmenu%23tilmeld"
  );
});

test("keeps an explicitly supplied return route", () => {
  const location = { pathname: "/react/account/login", search: "", hash: "" };

  assert.equal(loginHref("/react/pay", location), "/react/account/login?returnUrl=%2Freact%2Fpay");
});
