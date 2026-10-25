import { useEffect, useState } from "react";
import { Link } from "../routes/navigation.jsx";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { paymentSubscriptionsApi } from "../services/api.js";

const dateFormatter = new Intl.DateTimeFormat("da-DK", {
  dateStyle: "medium",
  timeStyle: "short",
});

function formatDate(value) {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : dateFormatter.format(date);
}

function formatAmount(amountMinor, currency) {
  if (amountMinor == null) return "—";
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

function statusClassName(status) {
  const normalized = String(status || "").toLowerCase();
  if (["canceled", "cancelled", "expired"].includes(normalized)) {
    return "admin-subscription-status admin-subscription-status-terminal";
  }
  if (["pending", "past_due", "incomplete"].includes(normalized)) {
    return "admin-subscription-status admin-subscription-status-pending";
  }
  return "admin-subscription-status";
}

function DetailItem({ label, value, className = "" }) {
  return (
    <div className={`admin-subscription-overview-item ${className}`.trim()}>
      <dt>{label}</dt>
      <dd>{value || "—"}</dd>
    </div>
  );
}

export function AdminSubscriptionDetailPage({ isAdmin, subscriptionId }) {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin || !subscriptionId) return;

    setData(null);
    setError("");
    paymentSubscriptionsApi.getAdmin(subscriptionId)
      .then(setData)
      .catch((loadError) => setError(loadError.message || "Betalingsaftalen kunne ikke hentes."));
  }, [isAdmin, subscriptionId]);

  if (!isAdmin) {
    return (
      <AdminLayout active="/admin/subscriptions" canWrite={false}>
        <p className="status-message status-message-warning">Du har ikke rettigheder til at se betalingsaftaler.</p>
      </AdminLayout>
    );
  }

  const subscription = data?.subscription;
  const payments = Array.isArray(data?.payments) ? data.payments : [];

  return (
    <AdminLayout active="/admin/subscriptions" canWrite>
      <div className="admin-subscription-detail-header">
        <div>
          <p className="menu-section-title">Betalingsaftale</p>
          <h1>{subscription?.productName || "Abonnement"}</h1>
          {subscription && (
            <p className="menu-panel-lead">
              {subscription.userName || "Ukendt bruger"}
              {subscription.userEmail ? ` · ${subscription.userEmail}` : ""}
            </p>
          )}
        </div>
        <Link className="frontpage-button frontpage-button-secondary" href="/admin/subscriptions">
          Tilbage til betalingsaftaler
        </Link>
      </div>

      {error && <p className="status-message status-message-error">{error}</p>}
      {!data && !error && <p className="muted">Henter betalingsaftale...</p>}

      {subscription && (
        <>
          <article className="admin-subscription-overview-card">
            <div className="admin-subscription-overview-heading">
              <div>
                <h2>Oversigt</h2>
                <p>{providerLabel(subscription.provider)} · {subscription.status || "—"}</p>
              </div>
              <span className={statusClassName(subscription.status)}>{subscription.status || "—"}</span>
            </div>
            <dl className="admin-subscription-overview-grid">
              <DetailItem label="Produkt" value={subscription.productName} />
              <DetailItem label="Pris" value={formatAmount(subscription.amountMinor, subscription.currency)} />
              <DetailItem label="Status" value={subscription.status} />
              <DetailItem label="Udbyder" value={providerLabel(subscription.provider)} />
              <DetailItem label="Betaler" value={subscription.userName} />
              <DetailItem label="E-mail" value={subscription.userEmail} />
              <DetailItem label="Senest betalt" value={formatDate(subscription.lastPaidAt)} />
              <DetailItem label="Næste opkrævning" value={formatDate(subscription.nextChargeAt)} />
              <DetailItem label="Nuværende periode fra" value={formatDate(subscription.currentPeriodStart)} />
              <DetailItem label="Nuværende periode til" value={formatDate(subscription.currentPeriodEnd)} />
              <DetailItem label="Annulleres ved periodens udløb" value={subscription.cancelAtPeriodEnd ? "Ja" : "Nej"} />
              <DetailItem label="Scope" value={scopeLabel(subscription.scope)} />
            </dl>

            <details className="admin-subscription-technical-details">
              <summary>Tekniske oplysninger</summary>
              <dl className="admin-subscription-overview-grid admin-subscription-technical-grid">
                <DetailItem label="Internt abonnement-id" value={subscription.id} />
                <DetailItem label="Internt tilbuds-id" value={subscription.paymentOfferId} />
                <DetailItem label="Udbyderens abonnements-id" value={subscription.providerSubscriptionId} />
                <DetailItem label="Udbyderens produkt-id" value={subscription.providerProductId} />
                <DetailItem label="Udbyderens pris-id" value={subscription.providerPriceId} />
                <DetailItem label="Oprettet" value={formatDate(subscription.created)} />
                <DetailItem label="Opdateret" value={formatDate(subscription.updated)} />
                <DetailItem label="Seneste webhook-tidspunkt" value={formatDate(subscription.lastProcessedWebhookAt)} />
                <DetailItem className="admin-subscription-overview-item-wide" label="Seneste webhook-id" value={subscription.lastProcessedWebhookId} />
              </dl>
            </details>
          </article>

          <section className="admin-subscription-payments-card">
            <div className="admin-subscription-payments-heading">
              <div>
                <p className="menu-section-title">Providerdata</p>
                <h2>Betalinger</h2>
              </div>
              <span className="muted">{payments.length} registreret</span>
            </div>

            {data.paymentsError && <p className="status-message status-message-warning">{data.paymentsError}</p>}
            {!data.paymentsError && payments.length === 0 && <p className="muted">Der er ingen betalinger for denne aftale.</p>}
            {!data.paymentsError && payments.length > 0 && (
              <div className="menu-table-wrap admin-subscription-payments-table-wrap">
                <table className="menu-member-table admin-subscription-payments-table">
                  <thead>
                    <tr>
                      <th>ID</th>
                      <th>Type</th>
                      <th>Status</th>
                      <th>Beløb</th>
                      <th>Oprettet</th>
                      <th>Forfald / betalt</th>
                      <th>Beskrivelse</th>
                      <th>Transaktions-id</th>
                    </tr>
                  </thead>
                  <tbody>
                    {payments.map((payment) => (
                      <tr key={payment.id}>
                        <td className="admin-subscription-payment-id">{payment.id || "—"}</td>
                        <td>{payment.type || "—"}</td>
                        <td>{payment.status || "—"}</td>
                        <td>{formatAmount(payment.amountMinor, payment.currency)}</td>
                        <td>{formatDate(payment.createdAt)}</td>
                        <td>{formatDate(payment.paidAt || payment.dueAt)}</td>
                        <td>{payment.description || "—"}</td>
                        <td>{payment.transactionId || payment.failureReason || "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </AdminLayout>
  );
}
