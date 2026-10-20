import { Fragment, useEffect, useMemo, useState } from "react";
import { CircleMinus, CirclePlus } from "lucide-react";
import { MultiSelectDropdown } from "../components/MultiSelectDropdown.jsx";
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
  const [userFilters, setUserFilters] = useState([]);
  const [expandedIds, setExpandedIds] = useState(() => new Set());
  const [isLoading, setIsLoading] = useState(Boolean(isAdmin));
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin) return;

    setIsLoading(true);
    paymentSubscriptionsApi.listAdmin()
      .then((result) => setSubscriptions(Array.isArray(result) ? result : []))
      .catch((loadError) => setError(loadError.message || "Betalingsaftalerne kunne ikke hentes."))
      .finally(() => setIsLoading(false));
  }, [isAdmin]);

  const userOptions = useMemo(() => {
    const options = new Map();
    for (const subscription of subscriptions) {
      const value = subscription.userId || subscription.userEmail || "";
      if (!value) continue;
      options.set(value, {
        id: value,
        title: subscription.userName || subscription.userEmail || subscription.userId,
        summary: subscription.userEmail || subscription.userId,
      });
    }
    return [...options.entries()]
      .sort((left, right) => left[1].title.localeCompare(right[1].title, "da"))
      .map(([, item]) => item);
  }, [subscriptions]);

  const filteredSubscriptions = useMemo(() => subscriptions.filter((subscription) => {
    const userValue = subscription.userId || subscription.userEmail || "";
    return userFilters.length === 0 || userFilters.includes(userValue);
  }), [subscriptions, userFilters]);

  function toggleExpanded(id) {
    setExpandedIds((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
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

      {!isLoading && !error && (
        <>
          <div className="menu-table-toolbar admin-products-filters admin-subscriptions-filters">
            <MultiSelectDropdown
              label="Bruger"
              placeholder="Alle brugere"
              items={userOptions}
              selectedIds={userFilters}
              onChange={setUserFilters}
              showMeta={false}
              searchable
            />
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
                </tr>
              </thead>
              <tbody>
                {filteredSubscriptions.map((subscription) => {
                  const isExpanded = expandedIds.has(subscription.id);

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
                      </tr>
                      {isExpanded && (
                        <tr className="admin-subscription-detail-row">
                          <td></td>
                          <td colSpan="7">
                            <dl className="admin-subscription-details">
                              <div><dt>Produkt</dt><dd>{subscription.productName || "—"}</dd></div>
                              <div><dt>Pris</dt><dd>{formatAmount(subscription.amountMinor, subscription.currency)}</dd></div>
                              <div><dt>Status</dt><dd>{subscription.status || "—"}</dd></div>
                              <div><dt>Senest betalt</dt><dd>{formatDate(subscription.lastPaidAt)}</dd></div>
                              <div><dt>Næste opkrævning</dt><dd>{formatDate(subscription.nextChargeAt)}</dd></div>
                              <div><dt>Scope</dt><dd>{scopeLabel(subscription.scope)}</dd></div>
                            </dl>
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}
                {filteredSubscriptions.length === 0 && (
                  <tr>
                    <td colSpan="8" className="muted">Ingen betalingsaftaler matcher filtrene.</td>
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
