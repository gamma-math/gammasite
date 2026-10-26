import test from "node:test";
import assert from "node:assert/strict";
import { paymentOffersApi } from "../../frontend/services/api.js";

function response(payload) {
  return {
    ok: true,
    status: 200,
    headers: { get: () => "application/json" },
    json: async () => payload
  };
}

test("reuses a checkout idempotency key until the checkout flow is cleared", async () => {
  const previousFetch = globalThis.fetch;
  const previousStorage = globalThis.sessionStorage;
  const requests = [];
  const values = new Map();

  globalThis.sessionStorage = {
    get length() {
      return values.size;
    },
    key: (index) => [...values.keys()][index] ?? null,
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, value),
    removeItem: (key) => values.delete(key)
  };
  globalThis.fetch = async (path, options) => {
    requests.push({ path, options });
    return path === "/api/account/csrf-token"
      ? response({ token: "csrf-token" })
      : response({ url: "https://payment.example/checkout" });
  };

  try {
    await paymentOffersApi.checkout(7);
    await paymentOffersApi.checkout(7);

    const checkoutRequests = requests.filter((item) => item.path.endsWith("/checkout"));
    const firstKey = checkoutRequests[0].options.headers["Idempotency-Key"];
    assert.ok(firstKey);
    assert.equal(checkoutRequests[1].options.headers["Idempotency-Key"], firstKey);

    paymentOffersApi.clearCheckoutAttempt(7);
    await paymentOffersApi.checkout(7);
    assert.notEqual(
      requests.filter((item) => item.path.endsWith("/checkout"))[2].options.headers["Idempotency-Key"],
      firstKey
    );
  } finally {
    globalThis.fetch = previousFetch;
    if (previousStorage === undefined) {
      delete globalThis.sessionStorage;
    } else {
      globalThis.sessionStorage = previousStorage;
    }
  }
});

test("replaces an expired checkout key before requesting a provider redirect", async () => {
  const previousFetch = globalThis.fetch;
  const previousStorage = globalThis.sessionStorage;
  const previousNow = Date.now;
  const requests = [];
  const values = new Map();
  const now = 1_793_015_823_000;

  globalThis.sessionStorage = {
    get length() {
      return values.size;
    },
    key: (index) => [...values.keys()][index] ?? null,
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, value),
    removeItem: (key) => values.delete(key)
  };
  values.set(
    "gammasite:payment-checkout:8",
    JSON.stringify({ key: "expired-checkout-key", createdAt: now - (8 * 60 * 1000) })
  );
  Date.now = () => now;
  globalThis.fetch = async (path, options) => {
    requests.push({ path, options });
    return path === "/api/account/csrf-token"
      ? response({ token: "csrf-token" })
      : response({ url: "https://payment.example/checkout" });
  };

  try {
    await paymentOffersApi.checkout(8);

    const checkoutRequest = requests.find((item) => item.path.endsWith("/checkout"));
    assert.notEqual(checkoutRequest.options.headers["Idempotency-Key"], "expired-checkout-key");
  } finally {
    Date.now = previousNow;
    globalThis.fetch = previousFetch;
    if (previousStorage === undefined) {
      delete globalThis.sessionStorage;
    } else {
      globalThis.sessionStorage = previousStorage;
    }
  }
});
