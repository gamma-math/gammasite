import { useEffect, useState } from "react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { accountApi, paymentsApi } from "../services/api.js";
import { hasPaidMembership } from "../utils/membership.js";
import { LoginRequired } from "./MembersPage.jsx";

const mobilePayMembershipUrl = "https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amount=150&comment=Medlemskab";

/**
 * Member payment overview with a separate choice for billing type and provider.
 */
export function PaymentPage({ user }) {
  const [products, setProducts] = useState([]);
  const [subscriptions, setSubscriptions] = useState([]);
  const [paymentType, setPaymentType] = useState("one-time");
  const [isStartingCheckout, setIsStartingCheckout] = useState(false);
  const [checkoutError, setCheckoutError] = useState("");
  const [isMembershipStatusLoading, setIsMembershipStatusLoading] = useState(true);

  useEffect(() => {
    if (user.isAuthenticated) {
      Promise.all([
        paymentsApi.products().catch(() => []),
        accountApi.subscriptions().catch(() => [])
      ]).then(([nextProducts, nextSubscriptions]) => {
        setProducts(nextProducts);
        setSubscriptions(nextSubscriptions);
      }).finally(() => setIsMembershipStatusLoading(false));
    }
  }, [user.isAuthenticated]);

  if (!user.isAuthenticated) {
    return <LoginRequired title="Betaling" />;
  }

  const membershipProduct = products[0];
  const membershipAlreadyPaid = hasPaidMembership(user, subscriptions);

  async function startStripeCheckout() {
    if (!membershipProduct || isStartingCheckout) {
      return;
    }

    setCheckoutError("");
    setIsStartingCheckout(true);
    try {
      const config = await paymentsApi.config();
      const session = await paymentsApi.startProductCheckout(membershipProduct.id, user.id, paymentType);
      await redirectToStripe(config.publicApiKey, session.id);
    } catch (error) {
      setCheckoutError(error.message || "Betalingen kunne ikke startes.");
      setIsStartingCheckout(false);
    }
  }

  return (
    <MenuLayout active="/react/pay" isAuthenticated={user.isAuthenticated}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Betal medlemskab</p>
        </div>
      </div>

      <article className={`menu-payment-card menu-payment-choice-card${membershipAlreadyPaid ? " membership-paid-card" : ""}`}>
        {isMembershipStatusLoading ? (
          <p className="muted">Henter medlemsstatus...</p>
        ) : membershipAlreadyPaid ? (
          <div className="membership-paid-message" role="status">
            <span className="membership-paid-icon" aria-hidden="true">✓</span>
            <span className="membership-paid-copy">
              <strong>Medlemskab er aktivt</strong>
              <small>Dit medlemskab er allerede registreret.</small>
            </span>
          </div>
        ) : (
          <>
            <p id="payment-type-heading" className="menu-payment-kicker payment-method-kicker">Vælg betalingstype</p>
            <fieldset className="payment-type-options" aria-labelledby="payment-type-heading">
              <label className={`payment-type-option${paymentType === "one-time" ? " is-selected" : ""}`}>
                <input type="radio" name="paymentType" value="one-time" checked={paymentType === "one-time"} onChange={() => setPaymentType("one-time")} />
                <span>
                  <strong>Betal én gang</strong>
                  <small>Betal medlemskabet manuelt for den aktuelle periode.</small>
                </span>
              </label>
              <label className={`payment-type-option${paymentType === "recurring" ? " is-selected" : ""}`}>
                <input type="radio" name="paymentType" value="recurring" checked={paymentType === "recurring"} onChange={() => setPaymentType("recurring")} />
                <span>
                  <strong>Automatisk fornyelse</strong>
                  <small>Medlemskabet fornyes automatisk hvert år.</small>
                </span>
              </label>
            </fieldset>

            {paymentType === "recurring" && (
              <p className="payment-type-note">Automatisk fornyelse er i første omgang kun tilgængelig med betalingskort. MobilePay-linket nedenfor gennemfører den almindelige manuelle betaling.</p>
            )}

            <p className="menu-payment-kicker payment-method-kicker">Vælg betalingsmetode</p>
            <div className="payment-choice-actions">
              {membershipProduct ? (
                <button className="menu-payment-button menu-payment-button-large" type="button" onClick={startStripeCheckout} disabled={isStartingCheckout}>
                  {isStartingCheckout ? "Åbner Stripe..." : "Betal med hævekort"}
                </button>
              ) : (
                <button className="menu-payment-button menu-payment-button-large" type="button" disabled>
                  Henter hævekort
                </button>
              )}
              <span className="payment-choice-divider">eller</span>
              <a className="menu-payment-button menu-payment-button-large menu-payment-button-secondary" href={mobilePayMembershipUrl} target="_blank" rel="noreferrer">
                Betal med MobilePay
              </a>
            </div>
          </>
        )}
        {checkoutError && <p className="status-message status-message-error">{checkoutError}</p>}
      </article>
    </MenuLayout>
  );
}

/**
 * Product payment page that creates a Stripe checkout session.
 */
export function ProductPaymentPage({ productId, user }) {
  const [product, setProduct] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    if (user.isAuthenticated) {
      paymentsApi.product(productId).then(setProduct).catch(() => setError("Produktet kunne ikke hentes."));
    }
  }, [productId, user.isAuthenticated]);

  if (!user.isAuthenticated) {
    return <LoginRequired title="Betaling" />;
  }

  if (!product && !error) {
    return (
      <MenuLayout active="/react/pay" isAuthenticated={user.isAuthenticated}>
        <div className="menu-loading-card">
          <span className="menu-loading-dot" aria-hidden="true" />
          <p>Henter produkt...</p>
        </div>
      </MenuLayout>
    );
  }

  if (error) {
    return (
      <MenuLayout active="/react/pay" isAuthenticated={user.isAuthenticated}>
        <p className="status-message status-message-error">{error}</p>
      </MenuLayout>
    );
  }

  async function checkout() {
    const config = await paymentsApi.config();
    const session = await paymentsApi.startProductCheckout(product.id, user.id);
    await redirectToStripe(config.publicApiKey, session.id);
  }

  return (
    <MenuLayout active="/react/pay" isAuthenticated={user.isAuthenticated}>
      <article className="menu-payment-card menu-payment-product-card">
        <div className="menu-payment-product-hero">
          <p className="menu-section-title">Betal medlemskab</p>
          <h1>{product.name}</h1>
        </div>
        <div className="menu-payment-product-body">
          <p className="muted">{product.description}</p>
          {product.additional && <p>{product.additional}</p>}
          <p>Ved køb accepteres de gældende <Link href="/react/betingelser">Betingelser og vedtægter</Link>.</p>
          <button className="menu-payment-button" type="button" onClick={checkout}>Køb {product.name.toLowerCase()}</button>
        </div>
      </article>
    </MenuLayout>
  );
}

/**
 * Generic payment page for custom amount Stripe checkout.
 */
export function GenericPaymentPage({ user }) {
  const [amount, setAmount] = useState("");
  const [description, setDescription] = useState("");

  if (!user.isAuthenticated) {
    return <LoginRequired title="Overfør valgfrit beløb" />;
  }

  async function checkout(event) {
    event.preventDefault();
    const config = await paymentsApi.config();
    const session = await paymentsApi.startGenericCheckout("Generisk", Number(amount) * 100, description, user.id);
    await redirectToStripe(config.publicApiKey, session.id);
  }

  return (
    <MenuLayout active="/react/pay" isAuthenticated={user.isAuthenticated}>
      <form className="menu-payment-card" onSubmit={checkout}>
        <p className="menu-payment-kicker">Valgfrit beløb</p>
        <h1>Overfør valgfrit beløb</h1>
        <label className="admin-field"><span>Beløb</span><input type="number" min="1" value={amount} onChange={(event) => setAmount(event.target.value)} required /></label>
        <label className="admin-field"><span>Beskrivelse</span><input value={description} onChange={(event) => setDescription(event.target.value)} required /></label>
        <button className="menu-payment-button" type="submit">Overfør beløb</button>
      </form>
    </MenuLayout>
  );
}

/**
 * Displays payment success, membership success, or cancellation states.
 */
export function PaymentStatusPage({ status, user }) {
  const success = status === "success" || status === "kontingent-success";
  return (
    <MenuLayout active="/react/pay" isAuthenticated={user?.isAuthenticated}>
      <article className="menu-payment-card menu-payment-status-card">
        <p className="menu-payment-kicker">Betaling</p>
        <h1>{success ? "Køb gennemført" : "Køb annulleret"}</h1>
        <p className="muted">{status === "kontingent-success" ? "Du har nu købt et kontingent." : success ? "Dit køb blev gennemført." : "Dit køb blev annulleret."}</p>
        <Link className="frontpage-button frontpage-button-secondary" href="/react/pay">Til oversigten</Link>
      </article>
    </MenuLayout>
  );
}

async function redirectToStripe(publicApiKey, sessionId) {
  await loadStripeScript();
  const stripe = window.Stripe(publicApiKey);
  await stripe.redirectToCheckout({ sessionId });
}

function loadStripeScript() {
  if (window.Stripe) {
    return Promise.resolve();
  }

  return new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = "https://js.stripe.com/v3/";
    script.onload = resolve;
    script.onerror = reject;
    document.head.appendChild(script);
  });
}
