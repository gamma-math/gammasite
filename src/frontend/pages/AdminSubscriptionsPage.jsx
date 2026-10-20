import { Fragment, useEffect, useMemo, useState } from "react";
import { CircleMinus, CirclePlus } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { paymentSubscriptionsApi } from "../services/api.js";

const dateFormatter = new Intl.DateTimeFormat("da-DK", {
  dateStyle: "medium",
  timeStyle: "short",
});

function formatDate(value) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : dateFormatter.format(date);
}

function formatAmount(amountMinor, currency) {
  if (amountMinor == null) {
    return "—";
  }

  return new Intl.NumberFormat("da-DK", {
    style: "currency",
    currency: (currency || "DKK").toUpperCase(),
  }).format(amountMinor / 100);
}

function providerLabel(provider) {
  if (provider === "MOBILEPAY") return "MobilePay";
  if (provider === "STRIPE") return "Stripe";
  return provider || "—";
}

function scopeLabel(scope) {
  if (scope === "MEMBERSHIP") return "Medlemskab";
  if (scope === "EVENT") return "Event";
  return scope || "—";
}

export function AdminSubscriptionsPage({ isAdmin }) {
  const [subscriptions, setSubscriptions] = useState([]);
  const [providerFilter, setProviderFilter] = useState("");
  const [userFilter, setUserFilter] = useState("");
  const [expandedIds, setExpandedIds] = useState(() => new Set());
  const [isLoading, setIsLoading] = useState(Boolean(isAdmin));
  const [chargingId, setChargingId] = useState(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin) return;

    setIsLoading(true);
    paymentSubscriptionsApi.listAdmin()
      .then((result) => setSubscriptions(Array.isArray(result) ? result : []))
      .catch((loadError) => setError(loadError.message || "Betalingsaftalerne kunne ikke hentes."))
      .finally(() => setIsLoading(false));
  }, [isAdmin]);

  const providerOptions = useMemo(() => [...new Set(
    subscriptions
      .map((subscription) => subscription.provider)
      .filter(Boolean),
  )].sort((left, right) => left.localeCompare(right, "da")), [subscriptions]);

  const userOptions = useMemo(() => {
    const options = new Map();
    for (const subscription of subscriptions) {
      const value = subscription.userId || subscription.userEmail || "";
      if (!value) continue;
      options.set(value, subscription.userName || subscription.userEmail || subscription.userId);
    }
    return [...options.entries()].sort((left, right) => left[1].localeCompare(right[1], "da"));
  }, [subscriptions]);

  const filteredSubscriptions = useMemo(() => subscriptions.filter((subscription) => {
    const userValue = subscription.userId || subscription.userEmail || "";
    return (!providerFilter || subscription.provider === providerFilter)
      && (!userFilter || userValue === userFilter);
  }), [providerFilter, subscriptions, userFilter]);

  function toggleExpanded(id) {
    setExpandedIds((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  async function charge(subscription) {
    if (!subscription?.canCharge || chargingId) return;
    if (!window.confirm(`Vil du oprette en MobilePay-opkrævning for ${subscription.userName || subscription.userEmail || "brugeren"}?`)) return;

    setChargingId(subscription.id);
    setMessage("");
    setError("");
    try {
      const result = await paymentSubscriptionsApi.chargeAdmin(subscription.id);
      setMessage(`Opkrævningen er oprettet og forfalder ${formatDate(result?.due)}.`);
    } catch (chargeError) {
      setError(chargeError.message || "Opkrævningen kunne ikke oprettes.");
    } finally {
      setChargingId(null);
    }
  }

  if (!isAdmin) {
    return (
      <AdminLayout active="/react/admin/subscriptions" canWrite={false}>
        <p className="status-message status-message-warning">Du har ikke rettigheder til at se betalingsaftaler.</p>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout active="/react/admin/subscriptions" canWrite>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Betalingsaftaler</p>
        </div>
      </div>

      {isLoading && <p className="muted">Henter betalingsaftaler...</p>}
      {error && <p className="status-message status-message-error">{error}</p>}
      {message && <p className="status-message status-message-success">{message}</p>}

      {!isLoading && !error && (
        <>
          <div className="menu-table-toolbar admin-products-filters admin-subscriptions-filters">
            <label className="admin-field">
              <span>Udbyder</span>
              <select value={providerFilter} onChange={(event) => setProviderFilter(event.target.value)}>
                <option value="">Alle udbydere</option>
                {providerOptions.map((provider) => <option value={provider} key={provider}>{providerLabel(provider)}</option>)}
              </select>
            </label>
            <label className="admin-field">
              <span>Bruger</span>
              <select value={userFilter} onChange={(event) => setUserFilter(event.target.value)}>
                <option value="">Alle brugere</option>
                {userOptions.map(([value, label]) => <option value={value} key={value}>{label}</option>)}
              </select>
            </label>
          </div>

          <div className="menu-table-wrap admin-subscriptions-list">
            <table className="menu-member-table admin-subscriptions-table">
              <thead>
                <tr>
                  <th className="admin-subscription-expander-header" aria-label="Detaljer" />
                  <th>Bruger</th>
                  <th>Udbyder</th>
                  <th className="admin-subscription-mobile-hidden">Produkt</th>
                  <th className="admin-subscription-mobile-hidden">Pris</th>
                  <th className="admin-subscription-mobile-hidden">Status</th>
                  <th className="admin-subscription-mobile-hidden">Senest betalt</th>
                  <th className="admin-subscription-mobile-hidden">Næste opkrævning</th>
                  <th className="admin-subscription-mobile-hidden">Handling</th>
                </tr>
              </thead>
              <tbody>
                {filteredSubscriptions.map((subscription) => {
                  const isExpanded = expandedIds.has(subscription.id);
                  const action = subscription.canCharge ? (
                    <button className="admin-table-button" type="button" disabled={chargingId === subscription.id} onClick={() => charge(subscription)}>
                      {chargingId === subscription.id ? "Opretter..." : "Opkræv"}
                    </button>
                  ) : subscription.provider === "STRIPE" ? (
                    <span className="muted">Automatisk hos Stripe</span>
                  ) : (
                    <span className="muted">—</span>
                  );

                  return (
                    <Fragment key={subscription.id}>
                      <tr>
                        <td className="admin-subscription-expander-cell">
                          <button className="menu-member-expander" type="button" aria-label={isExpanded ? "Skjul detaljer" : "Vis detaljer"} aria-expanded={isExpanded} onClick={() => toggleExpanded(subscription.id)}>
                            {isExpanded ? <CircleMinus size={20} /> : <CirclePlus size={20} />}
                          </button>
                        </td>
                        <td>
                          <strong>{subscription.userName || "Ukendt bruger"}</strong>
                          <br />
                          <span className="muted">{subscription.userEmail || subscription.userId || "—"}</span>
                        </td>
                        <td>{providerLabel(subscription.provider)}</td>
                        <td className="admin-subscription-mobile-hidden">
                          {subscription.productName || "—"}
                          <br />
                          <span className="muted">{scopeLabel(subscription.scope)}</span>
                        </td>
                        <td className="admin-subscription-mobile-hidden">{formatAmount(subscription.amountMinor, subscription.currency)}</td>
                        <td className="admin-subscription-mobile-hidden">{subscription.status || "—"}</td>
                        <td className="admin-subscription-mobile-hidden">{formatDate(subscription.lastPaidAt)}</td>
                        <td className="admin-subscription-mobile-hidden">{formatDate(subscription.nextChargeAt)}</td>
                        <td className="table-actions admin-subscription-mobile-hidden">{action}</td>
                      </tr>
                      {isExpanded && (
                        <tr className="admin-subscription-detail-row">
                          <td></td>
                          <td colSpan="8">
                            <dl className="admin-subscription-details">
                              <div><dt>Produkt</dt><dd>{subscription.productName || "—"}</dd></div>
                              <div><dt>Pris</dt><dd>{formatAmount(subscription.amountMinor, subscription.currency)}</dd></div>
                              <div><dt>Status</dt><dd>{subscription.status || "—"}</dd></div>
                              <div><dt>Senest betalt</dt><dd>{formatDate(subscription.lastPaidAt)}</dd></div>
                              <div><dt>Næste opkrævning</dt><dd>{formatDate(subscription.nextChargeAt)}</dd></div>
                              <div><dt>Scope</dt><dd>{scopeLabel(subscription.scope)}</dd></div>
                            </dl>
                            <div className="table-actions">{action}</div>
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}
                {filteredSubscriptions.length === 0 && (
                  <tr>
                    <td colSpan="9" className="muted">Ingen betalingsaftaler matcher filtrene.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </AdminLayout>
  );
}
