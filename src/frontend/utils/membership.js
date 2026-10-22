const activeSubscriptionStatuses = new Set(["active", "trialing", "past_due", "incomplete"]);

export function hasActiveSubscription(subscriptions = []) {
  return subscriptions.some((subscription) => activeSubscriptionStatuses.has(String(subscription.status || "").toLowerCase()));
}

export function hasPaidMembership(user, subscriptions = []) {
  const status = String(user?.status || "").toUpperCase();
  return status === "BETALT" || status === "STUDERENDE" || hasActiveSubscription(subscriptions);
}
