import { useEffect, useState } from "react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { paymentsApi } from "../services/api.js";
import { LoginRequired } from "./MembersPage.jsx";

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

  return (
    <MenuLayout active="/react/pay">
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Betal medlemskab</p>
          <h1>Produkter</h1>
        </div>
      </div>
      <div className="payment-grid">
        {products.map((product) => (
          <article className="menu-payment-card" key={product.id}>
            <p className="menu-payment-kicker">{product.currency?.toUpperCase() ?? "DKK"}</p>
            <h2>{product.name}</h2>
            <p className="muted">{product.description}</p>
            <strong>{product.unitAmount ? `${product.unitAmount / 100} kr.` : "Pris ikke sat"}</strong>
            <Link className="menu-payment-button" href={`/react/pay/products/${product.id}`}>Vælg</Link>
          </article>
        ))}
      </div>
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

  async function checkout() {
    const config = await paymentsApi.config();
    const session = await paymentsApi.startProductCheckout(product.id, user.id);
    await redirectToStripe(config.publicApiKey, session.id);
  }

  return (
    <MenuLayout active="/react/pay">
      <article className="menu-payment-card">
        <p className="menu-payment-kicker">Produktside</p>
        <h1>{product?.name ?? "Henter produkt..."}</h1>
        {error && <p className="status-message">{error}</p>}
        <p className="muted">{product?.description}</p>
        {product?.additional && <p>{product.additional}</p>}
        {product?.conditions && <p>Ved køb accepteres de gældende <a href={product.conditions}>{product.conditionsName || "betingelser"}</a>.</p>}
        <button className="menu-payment-button" type="button" disabled={!product} onClick={checkout}>Køb {product?.name?.toLowerCase() ?? ""}</button>
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
    <MenuLayout active="/react/pay">
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

export function PaymentStatusPage({ status }) {
  const success = status === "success" || status === "kontingent-success";
  return (
    <MenuLayout active="/react/pay">
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
