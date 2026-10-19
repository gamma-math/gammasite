import { useEffect, useMemo, useState } from "react";
import { Save } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { contentApi, paymentOffersApi } from "../services/api.js";

const scopeLabels = {
  EVENT: "Event",
  MEMBERSHIP: "Medlemskab",
};

const providerLabels = {
  STRIPE: "Stripe",
  MOBILEPAY: "MobilePay",
};

const offerTypeLabels = {
  ONE_TIME: "Engangsbetaling",
  RECURRING: "Gentagen betaling",
};

function formatAmount(amountMinor, currency) {
  if (amountMinor == null) {
    return "—";
  }

  return new Intl.NumberFormat("da-DK", {
    style: "currency",
    currency: (currency || "DKK").toUpperCase(),
  }).format(amountMinor / 100);
}

function labelFor(values, value) {
  return values[value] || value || "—";
}

/**
 * Overview of all configured payment products and offers.
 */
export function AdminProductsPage({ isAdmin }) {
  const [offers, setOffers] = useState([]);
  const [events, setEvents] = useState([]);
  const [eventFilter, setEventFilter] = useState("");
  const [scopeFilter, setScopeFilter] = useState("");
  const [isLoading, setIsLoading] = useState(Boolean(isAdmin));
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin) {
      return;
    }

    setIsLoading(true);
    setError("");
    Promise.all([paymentOffersApi.listAdmin(), contentApi.listAdmin("EVENT")])
      .then(([offerResult, eventResult]) => {
        setOffers(offerResult ?? []);
        setEvents(eventResult ?? []);
      })
      .catch((loadError) => {
        setError(loadError.message || "Produkterne kunne ikke hentes.");
      })
      .finally(() => setIsLoading(false));
  }, [isAdmin]);

  const eventOptions = useMemo(() => {
    const options = new Map(
      (events ?? []).map((event) => [String(event.id), event.title]),
    );

    for (const offer of offers) {
      if (offer.contentItemId != null && !options.has(String(offer.contentItemId))) {
        options.set(String(offer.contentItemId), offer.eventTitle || "Ukendt event");
      }
    }

    return [...options.entries()].sort((left, right) =>
      left[1].localeCompare(right[1], "da"),
    );
  }, [events, offers]);

  const filteredOffers = useMemo(() => {
    return offers.filter((offer) => {
      const matchesEvent = !eventFilter || String(offer.contentItemId) === eventFilter;
      const matchesScope = !scopeFilter || offer.scope === scopeFilter;
      return matchesEvent && matchesScope;
    });
  }, [eventFilter, offers, scopeFilter]);

  return (
    <AdminLayout active="/react/admin/products" canWrite={isAdmin}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Produkter</p>
        </div>
      </div>

      {!isAdmin && (
        <p className="status-message status-message-warning">
          Du har ikke rettigheder til at se produkter.
        </p>
      )}

      {isAdmin && isLoading && <p className="muted">Henter produkter...</p>}

      {isAdmin && error && (
        <p className="status-message status-message-error">{error}</p>
      )}

      {isAdmin && !isLoading && !error && (
        <>
          <div className="menu-table-toolbar admin-products-filters">
            <label className="admin-field">
              <span>Event</span>
              <select value={eventFilter} onChange={(event) => setEventFilter(event.target.value)}>
                <option value="">Alle events</option>
                {eventOptions.map(([id, title]) => (
                  <option value={id} key={id}>{title}</option>
                ))}
              </select>
            </label>
            <label className="admin-field">
              <span>Scope</span>
              <select value={scopeFilter} onChange={(event) => setScopeFilter(event.target.value)}>
                <option value="">Alle scopes</option>
                <option value="EVENT">Event</option>
                <option value="MEMBERSHIP">Medlemskab</option>
              </select>
            </label>
          </div>

          <div className="menu-table-wrap admin-products-list">
            <table className="menu-member-table">
              <thead>
                <tr>
                  <th>Produkt</th>
                  <th>Link label</th>
                  <th>Event</th>
                  <th>Scope</th>
                  <th>Type</th>
                  <th>Udbyder</th>
                  <th>Pris</th>
                  <th>Aktiv</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {filteredOffers.map((offer) => (
                  <tr key={offer.id}>
                    <td>{offer.productName || "—"}</td>
                    <td>{offer.linkLabel || "—"}</td>
                    <td>{offer.eventTitle || "—"}</td>
                    <td>{labelFor(scopeLabels, offer.scope)}</td>
                    <td>{labelFor(offerTypeLabels, offer.offerType)}</td>
                    <td>{labelFor(providerLabels, offer.provider)}</td>
                    <td>{formatAmount(offer.amountMinor, offer.currency)}</td>
                    <td>{offer.isActive ? "Ja" : "Nej"}</td>
                    <td className="table-actions">
                      <Link className="admin-table-button" href={`/react/admin/products/${offer.id}/edit`}>Rediger</Link>
                    </td>
                  </tr>
                ))}
                {filteredOffers.length === 0 && (
                  <tr>
                    <td colSpan="9" className="muted">Ingen produkter matcher filtrene.</td>
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

function toProductDraft(offer) {
  return {
    linkLabel: offer.linkLabel ?? "",
    productName: offer.productName ?? "",
    amount: offer.amountMinor == null ? "" : (offer.amountMinor / 100).toFixed(2),
    billingInterval: offer.billingInterval ?? "",
    billingIntervalCount: offer.billingIntervalCount ?? 1,
    isActive: offer.isActive !== false,
  };
}

/**
 * Editor for the editable product settings. Provider, scope and payment type
 * remain fixed because they identify the payment offer.
 */
export function AdminProductEditorPage({ isAdmin, offerId }) {
  const [offer, setOffer] = useState(null);
  const [draft, setDraft] = useState(null);
  const [isLoading, setIsLoading] = useState(Boolean(isAdmin));
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin) {
      return;
    }

    setIsLoading(true);
    paymentOffersApi.getAdmin(offerId)
      .then((result) => {
        setOffer(result);
        setDraft(toProductDraft(result));
      })
      .catch((loadError) => setError(loadError.message || "Produktet kunne ikke hentes."))
      .finally(() => setIsLoading(false));
  }, [isAdmin, offerId]);

  function update(field, value) {
    setDraft((current) => ({ ...current, [field]: value }));
    setMessage("");
  }

  async function save(event) {
    event.preventDefault();
    setIsSaving(true);
    setMessage("");
    setError("");

    try {
      const saved = await paymentOffersApi.updateAdmin(offerId, {
        linkLabel: draft.linkLabel,
        productName: draft.productName,
        amount: draft.amount === "" ? null : Number(draft.amount),
        billingInterval: draft.billingInterval || null,
        billingIntervalCount: draft.billingIntervalCount === "" ? null : Number(draft.billingIntervalCount),
        isActive: draft.isActive,
      });
      setOffer(saved);
      setDraft(toProductDraft(saved));
      setMessage("Produktet er gemt.");
    } catch (saveError) {
      setError(saveError.message || "Produktet kunne ikke gemmes.");
    } finally {
      setIsSaving(false);
    }
  }

  if (!isAdmin) {
    return (
      <AdminLayout active="/react/admin/products" canWrite={false}>
        <p className="status-message status-message-warning">Du har ikke rettigheder til at redigere produkter.</p>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout active="/react/admin/products" canWrite>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Produkter</p>
          <h1>Rediger produkt</h1>
        </div>
        <Link className="frontpage-button frontpage-button-secondary" href="/react/admin/products">Tilbage til oversigt</Link>
      </div>

      {isLoading && <p className="muted">Henter produkt...</p>}
      {error && <p className="status-message status-message-error">{error}</p>}

      {!isLoading && !error && offer && draft && (
        <form className="menu-editor-form admin-product-editor" onSubmit={save}>
          <div className="admin-form-row admin-form-row-two">
            <label className="admin-field">
              <span>Link label</span>
              <input value={draft.linkLabel} onChange={(event) => update("linkLabel", event.target.value)} required />
            </label>
            <label className="admin-field">
              <span>Produktnavn</span>
              <input value={draft.productName} onChange={(event) => update("productName", event.target.value)} required />
            </label>
          </div>

          <div className="admin-form-row admin-form-row-two">
            <label className="admin-field">
              <span>Pris (DKK)</span>
              <input type="number" min="0.01" step="0.01" value={draft.amount} onChange={(event) => update("amount", event.target.value)} required />
            </label>
            <label className="admin-field">
              <span>Udbyder</span>
              <select value={offer.provider || ""} disabled aria-label="Udbyder">
                <option value="STRIPE">Stripe</option>
                <option value="MOBILEPAY">MobilePay</option>
              </select>
            </label>
          </div>

          <div className="admin-form-row admin-form-row-two">
            <label className="admin-field">
              <span>Scope</span>
              <select value={offer.scope || ""} disabled aria-label="Scope">
                <option value="EVENT">Event</option>
                <option value="MEMBERSHIP">Medlemskab</option>
              </select>
            </label>
            <label className="admin-field">
              <span>Betalingstype</span>
              <select value={offer.offerType || ""} disabled aria-label="Betalingstype">
                <option value="ONE_TIME">Engangsbetaling</option>
                <option value="RECURRING">Gentagen betaling</option>
              </select>
            </label>
          </div>

          {offer.offerType === "RECURRING" && (
            <div className="admin-form-row admin-form-row-two">
              <label className="admin-field">
                <span>Billing interval</span>
                <input value={draft.billingInterval} onChange={(event) => update("billingInterval", event.target.value)} placeholder="YEAR" />
              </label>
              <label className="admin-field">
                <span>Antal intervaller</span>
                <input type="number" min="1" step="1" value={draft.billingIntervalCount} onChange={(event) => update("billingIntervalCount", event.target.value)} />
              </label>
            </div>
          )}

          {offer.eventTitle && <p className="muted">Event: {offer.eventTitle}</p>}

          <label className="profile-checkbox">
            <input type="checkbox" checked={draft.isActive} onChange={(event) => update("isActive", event.target.checked)} />
            Aktiv
          </label>

          <div className="menu-editor-actions">
            <button className="profile-button" type="submit" disabled={isSaving}>
              <Save size={16} />
              {isSaving ? "Gemmer..." : "Gem"}
            </button>
            {message && <p className="status-message status-message-success">{message}</p>}
          </div>
        </form>
      )}
    </AdminLayout>
  );
}
