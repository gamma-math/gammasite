import { useEffect, useState } from "react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { paymentsApi } from "../services/api.js";
import { LoginRequired } from "./MembersPage.jsx";

const mobilePayMembershipUrl = "https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amount=150&comment=Medlemskab";

export function PaymentPage({ user }) {
  const [products, setProducts] = useState([]);

  useEffect(() => {
    if (user.isAuthenticated) {
      paymentsApi.products().then(setProducts).catch(() => setProducts([]));
    }
  }, [user.isAuthenticated]);

  if (!user.isAuthenticated) {
    return <LoginRequired title="Betaling" />;
  }

  const membershipProduct = products[0];

  return (
    <MenuLayout active="/react/pay" isAuthenticated={user.isAuthenticated}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Betal medlemskab</p>
        </div>
      </div>

      <article className="menu-payment-card menu-payment-choice-card">
        <p className="menu-payment-kicker">Vælg betalingsform</p>
        <h1>Betal dit medlemskab</h1>
        <div className="payment-choice-actions">
          {membershipProduct ? (
            <Link className="menu-payment-button menu-payment-button-large" href={`/react/pay/products/${membershipProduct.id}`}>
              Betal med hævekort
            </Link>
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
      </article>
    </MenuLayout>
  );
}

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
        <p className="status-message">{error}</p>
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
        <p className="menu-payment-kicker">Produktside</p>
        <h1>{product.name}</h1>
        <p className="muted">{product.description}</p>
        {product.additional && <p>{product.additional}</p>}
        {product.conditions && <p>Ved køb accepteres de gældende <a href={product.conditions}>{product.conditionsName || "betingelser"}</a>.</p>}
        <button className="menu-payment-button" type="button" onClick={checkout}>Køb {product.name.toLowerCase()}</button>
      </article>
    </MenuLayout>
  );
}

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

export function PaymentStatusPage({ status, user }) {
  const success = status === "success" || status === "kontingent-success";
  return (
    <MenuLayout active="/react/pay" isAuthenticated={user?.isAuthenticated}>
      <article className="menu-payment-card">
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
