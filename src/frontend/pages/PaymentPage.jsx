import { useEffect, useState } from "react";
import { CreditCard, Smartphone } from "lucide-react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { accountApi, paymentOffersApi, paymentsApi } from "../services/api.js";
import { hasPaidMembership } from "../utils/membership.js";
import { LoginRequired } from "./MembersPage.jsx";

/**
 * Member payment overview with a separate choice for billing type and provider.
 */
export function PaymentPage({ user }) {
  const [membershipOffers, setMembershipOffers] = useState([]);
  const [subscriptions, setSubscriptions] = useState([]);
  const [paymentType, setPaymentType] = useState("recurring");
  const [isStartingCheckout, setIsStartingCheckout] = useState(false);
  const [startingPaymentMethod, setStartingPaymentMethod] = useState("");
  const [checkoutError, setCheckoutError] = useState("");
  const [isMembershipStatusLoading, setIsMembershipStatusLoading] = useState(true);
  const [currentMembershipStatus, setCurrentMembershipStatus] = useState(user.status);

  useEffect(() => {
    if (user.isAuthenticated) {
      Promise.all([
        accountApi.subscriptions().catch(() => []),
        paymentOffersApi.listMembership().catch(() => []),
        accountApi.profile().catch(() => null)
      ]).then(([nextSubscriptions, nextMembershipOffers, profile]) => {
        setSubscriptions(nextSubscriptions);
        setMembershipOffers(nextMembershipOffers);
        setCurrentMembershipStatus(profile?.status || user.status);
      }).finally(() => setIsMembershipStatusLoading(false));
    }
  }, [user.isAuthenticated]);

  if (!user.isAuthenticated) {
    return <LoginRequired title="Betaling" />;
  }

  const stripeMembershipOffer = membershipOffers.find((offer) =>
    offer.provider === "STRIPE" &&
    offer.offerType === (paymentType === "recurring" ? "RECURRING" : "ONE_TIME") &&
    offer.isActive);
  const mobilePayMembershipOffer = membershipOffers.find((offer) =>
    offer.provider === "MOBILEPAY" &&
    offer.offerType === (paymentType === "recurring" ? "RECURRING" : "ONE_TIME") &&
    offer.isActive);
  const membershipAlreadyPaid = hasPaidMembership({ ...user, status: currentMembershipStatus }, subscriptions);

  async function startStripeCheckout() {
    if (!stripeMembershipOffer || isStartingCheckout) {
      return;
    }

    setCheckoutError("");
    setIsStartingCheckout(true);
    setStartingPaymentMethod("STRIPE");
    try {
      const config = await paymentsApi.config();
      const session = await paymentOffersApi.checkout(stripeMembershipOffer.id);
      await redirectToStripe(config.publicApiKey, session.id);
    } catch (error) {
      setCheckoutError(error.message || "Betalingen kunne ikke startes.");
      setIsStartingCheckout(false);
      setStartingPaymentMethod("");
    }
  }

  async function startMobilePayCheckout() {
    if (!mobilePayMembershipOffer || isStartingCheckout) {
      return;
    }

    setCheckoutError("");
    setIsStartingCheckout(true);
    setStartingPaymentMethod("MOBILEPAY");
    try {
      const payment = await paymentOffersApi.checkout(mobilePayMembershipOffer.id);
      window.location.assign(payment.url);
    } catch (error) {
      setCheckoutError(error.message || "Betalingen kunne ikke startes.");
      setIsStartingCheckout(false);
      setStartingPaymentMethod("");
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
              <label className={`payment-type-option${paymentType === "recurring" ? " is-selected" : ""}`}>
                <input type="radio" name="paymentType" value="recurring" checked={paymentType === "recurring"} onChange={() => setPaymentType("recurring")} />
                <span>
                  <strong>Automatisk fornyelse</strong>
                  <small>Medlemskabet fornyes automatisk hvert år.</small>
                </span>
              </label>
              <label className={`payment-type-option${paymentType === "one-time" ? " is-selected" : ""}`}>
                <input type="radio" name="paymentType" value="one-time" checked={paymentType === "one-time"} onChange={() => setPaymentType("one-time")} />
                <span>
                  <strong>Betal én gang</strong>
                  <small>Betal medlemskabet manuelt for den aktuelle periode.</small>
                </span>
              </label>
            </fieldset>

            <p className="menu-payment-kicker payment-method-kicker">Vælg betalingsmetode</p>
            <div className="payment-choice-actions">
              {stripeMembershipOffer ? (
                <button className="menu-payment-button menu-payment-button-large" type="button" onClick={startStripeCheckout} disabled={isStartingCheckout}>
                  {startingPaymentMethod === "STRIPE" ? (
                    "Åbner kortbetaling..."
                  ) : (
                    <><CreditCard className="payment-method-icon" size={21} strokeWidth={2.2} /> Kort / Apple Pay</>
                  )}
                </button>
              ) : (
                <button className="menu-payment-button menu-payment-button-large" type="button" disabled>
                  Henter hævekort
                </button>
              )}
              <span className="payment-choice-divider">Eller</span>
              {mobilePayMembershipOffer ? (
                <button className="menu-payment-button menu-payment-button-large menu-payment-button-secondary" type="button" onClick={startMobilePayCheckout} disabled={isStartingCheckout}>
                  {startingPaymentMethod === "MOBILEPAY" ? (
                    "Åbner MobilePay..."
                  ) : (
                    <><Smartphone className="payment-method-icon" size={21} strokeWidth={2.2} /> MobilePay</>
                  )}
                </button>
              ) : (
                <button className="menu-payment-button menu-payment-button-large menu-payment-button-secondary" type="button" disabled>
                  MobilePay kommer snart
                </button>
              )}
            </div>
          </>
        )}
        {checkoutError && <p className="status-message status-message-error">{checkoutError}</p>}
      </article>
      <p className="payment-terms-note">
        Betalinger gennemføres i henhold til vores <Link href="/react/betingelser#salgsbetingelser">salgsbetingelser</Link>.
      </p>
    </MenuLayout>
  );
}

export function EventPaymentPage({ offerId, user }) {
  const [offer, setOffer] = useState(null);
  const [error, setError] = useState("");
  const [isStartingCheckout, setIsStartingCheckout] = useState(false);

  useEffect(() => {
    paymentOffersApi.get(offerId).then(setOffer).catch(() => setError("Betalingstilbuddet kunne ikke hentes."));
  }, [offerId]);

  if (!user.isAuthenticated) {
    const returnUrl = window.location.pathname + window.location.search;
    return (
      <MenuLayout active="/react/events" isAuthenticated={false}>
        <section className="frontpage-section">
          <h1>Betaling</h1>
          <p className="muted">Du skal være logget ind for at betale.</p>
          <a className="frontpage-button frontpage-button-primary" href={"/react/account/login?ReturnUrl=" + encodeURIComponent(returnUrl)}>Log ind</a>
        </section>
      </MenuLayout>
    );
  }

  if (!offer && !error) {
    return (
      <MenuLayout active="/react/events" isAuthenticated={user.isAuthenticated}>
        <div className="menu-loading-card">
          <span className="menu-loading-dot" aria-hidden="true" />
          <p>Henter betaling...</p>
        </div>
      </MenuLayout>
    );
  }

  if (error) {
    return (
      <MenuLayout active="/react/events" isAuthenticated={user.isAuthenticated}>
        <p className="status-message status-message-error">{error}</p>
      </MenuLayout>
    );
  }

  async function checkout() {
    if (!["STRIPE", "MOBILEPAY"].includes(offer.provider) || isStartingCheckout) {
      return;
    }

    setIsStartingCheckout(true);
    setError("");
    try {
      const session = await paymentOffersApi.checkout(offer.id);
      if (offer.provider === "STRIPE") {
        const config = await paymentsApi.config();
        await redirectToStripe(config.publicApiKey, session.id);
      } else {
        window.location.assign(session.url);
      }
    } catch (reason) {
      setError(reason.message || "Betalingen kunne ikke startes.");
      setIsStartingCheckout(false);
    }
  }

  const amount = offer.amountMinor == null
    ? ""
    : (Number(offer.amountMinor) / 100).toLocaleString("da-DK", { minimumFractionDigits: 2 }) + " " + String(offer.currency || "DKK").toUpperCase();
  const providerReady = ["STRIPE", "MOBILEPAY"].includes(offer.provider);

  return (
    <MenuLayout active="/react/events" isAuthenticated={user.isAuthenticated} contentClassName="menu-content-flat">
      <article className="menu-payment-card menu-payment-product-card">
        <div className="menu-payment-product-hero">
          <p className="menu-section-title">Betaling for event</p>
          <h1>{offer.productName}</h1>
        </div>
        <div className="menu-payment-product-body">
          <p className="muted">{offer.eventTitle}</p>
          <p className="payment-offer-price">{amount}</p>
          {providerReady ? (
            <button className="menu-payment-button" type="button" onClick={checkout} disabled={isStartingCheckout}>
              {isStartingCheckout ? "Åbner betaling..." : offer.provider === "MOBILEPAY" ? "Betal med MobilePay" : "Betal med kort"}
            </button>
          ) : (
            <p className="status-message status-message-warning">Betaling med {offer.provider} er ikke tilgængelig endnu.</p>
          )}
          {error && <p className="status-message status-message-error">{error}</p>}
        </div>
      </article>
    </MenuLayout>
  );
}

/**
 * Displays payment success, membership success, or cancellation states.
 */
export function PaymentStatusPage({ status, eventSlug, session, reference, user }) {
  const [paymentContext, setPaymentContext] = useState(null);
  const [isLoadingPaymentContext, setIsLoadingPaymentContext] = useState(Boolean(status === "success" && session));
  const [eventPaymentState, setEventPaymentState] = useState(reference ? "loading" : "success");
  const [eventPaymentError, setEventPaymentError] = useState("");
  const success = status === "success" || status === "kontingent-success";

  useEffect(() => {
    if (status !== "success" || !session) {
      setIsLoadingPaymentContext(false);
      return undefined;
    }

    let active = true;
    paymentOffersApi.successContext(session)
      .then((context) => {
        if (active) {
          setPaymentContext(context);
        }
      })
      .catch(() => undefined)
      .finally(() => {
        if (active) {
          setIsLoadingPaymentContext(false);
        }
      });

    return () => {
      active = false;
    };
  }, [status, session]);

  useEffect(() => {
    if (!reference || (status !== "event-success" && status !== "success") || session) {
      return undefined;
    }

    let active = true;
    let attempts = 0;
    let timer;
    const checkPayment = async () => {
      try {
        const context = await paymentOffersApi.mobilePayContext(reference);
        if (!active) {
          return;
        }

        setPaymentContext(context);
        if (context.status === "SUCCESS" || context.status === "CANCELLED") {
          setEventPaymentState(context.status.toLowerCase());
          return;
        }

        attempts += 1;
        if (attempts < 20) {
          timer = window.setTimeout(checkPayment, 1500);
        } else {
          setEventPaymentState("pending");
        }
      } catch (error) {
        if (active) {
          setEventPaymentError(error.message || "Betalingen kunne ikke bekræftes endnu.");
          setEventPaymentState("error");
        }
      }
    };

    checkPayment();
    return () => {
      active = false;
      if (timer) {
        window.clearTimeout(timer);
      }
    };
  }, [status, reference]);

  const resolvedEventSlug = eventSlug || paymentContext?.eventSlug;
  if (status === "success" && session && isLoadingPaymentContext) {
    return (
      <MenuLayout active="/react/events" isAuthenticated={user?.isAuthenticated} contentClassName="menu-content-flat">
        <p className="muted">Bekræfter betaling...</p>
      </MenuLayout>
    );
  }

  if (status === "success" && reference && eventPaymentState === "loading") {
    return (
      <MenuLayout active="/react/pay" isAuthenticated={user?.isAuthenticated} contentClassName="menu-content-flat">
        <p className="muted">Bekræfter betaling...</p>
      </MenuLayout>
    );
  }

  if (status === "event-success" && resolvedEventSlug && reference && eventPaymentState === "loading") {
    return (
      <MenuLayout active="/react/events" isAuthenticated={user?.isAuthenticated} contentClassName="menu-content-flat">
        <p className="muted">Bekræfter betaling...</p>
      </MenuLayout>
    );
  }

  if ((status === "event-success" || paymentContext?.type === "EVENT") && resolvedEventSlug && eventPaymentState !== "cancelled" && eventPaymentState !== "error") {
    return (
      <MenuLayout active="/react/events" isAuthenticated={user?.isAuthenticated} contentClassName="menu-content-flat">
        <section className="menu-payment-event-success">
          <p className="menu-payment-kicker">Betaling</p>
          <h1>{eventPaymentState === "pending" ? "Betaling modtages" : "Betaling gennemført"}</h1>
          <p className="muted">{eventPaymentState === "pending" ? "Betalingen er modtaget og bliver bekræftet. Du kan genindlæse siden om lidt." : "Din betaling til arrangementet er gennemført."}</p>
          <Link className="frontpage-button frontpage-button-primary" href={`/react/events/${encodeURIComponent(resolvedEventSlug)}`}>
            Til begivenheden
          </Link>
        </section>
      </MenuLayout>
    );
  }

  if (status === "success" && paymentContext?.type === "MEMBERSHIP" && eventPaymentState !== "cancelled" && eventPaymentState !== "error") {
    return (
      <MenuLayout active="/react/pay" isAuthenticated={user?.isAuthenticated} contentClassName="menu-content-flat">
        <section className="menu-payment-event-success">
          <p className="menu-payment-kicker">Betaling</p>
          <h1>{eventPaymentState === "pending" ? "Betaling modtages" : "Betaling gennemført"}</h1>
          <p className="muted">{eventPaymentState === "pending" ? "Betalingen er modtaget og bliver bekræftet. Du kan genindlæse siden om lidt." : "Dit medlemskab er nu registreret som betalt."}</p>
          <Link className="frontpage-button frontpage-button-primary" href="/react/pay">
            Til betaling
          </Link>
        </section>
      </MenuLayout>
    );
  }

  if (status === "success" && reference && paymentContext?.type === "MEMBERSHIP") {
    return (
      <MenuLayout active="/react/pay" isAuthenticated={user?.isAuthenticated} contentClassName="menu-content-flat">
        <section className="menu-payment-event-success">
          <p className="menu-payment-kicker">Betaling</p>
          <h1>Betaling ikke gennemført</h1>
          <p className="muted">Betalingen blev ikke gennemført.</p>
          <Link className="frontpage-button frontpage-button-primary" href="/react/pay">
            Tilbage til betaling
          </Link>
        </section>
      </MenuLayout>
    );
  }

  if (status === "event-success" && resolvedEventSlug) {
    return (
      <MenuLayout active="/react/events" isAuthenticated={user?.isAuthenticated} contentClassName="menu-content-flat">
        <section className="menu-payment-event-success">
          <p className="menu-payment-kicker">Betaling</p>
          <h1>{eventPaymentState === "cancelled" ? "Betaling ikke gennemført" : "Betaling kunne ikke bekræftes"}</h1>
          <p className="muted">{eventPaymentError || "Betalingen blev ikke gennemført."}</p>
          <Link className="frontpage-button frontpage-button-primary" href={`/react/events/${encodeURIComponent(resolvedEventSlug)}${paymentContext?.offerId ? `/pay/${paymentContext.offerId}` : ""}`}>
            Tilbage til betaling
          </Link>
        </section>
      </MenuLayout>
    );
  }

  if (status === "cancel") {
    return (
      <MenuLayout active="/react/pay" isAuthenticated={user?.isAuthenticated} contentClassName="menu-content-flat">
        <section className="menu-payment-event-success">
          <p className="menu-payment-kicker">Betaling</p>
          <h1>Betaling ikke gennemført</h1>
          <p className="muted">Betalingen blev ikke gennemført.</p>
          <Link className="frontpage-button frontpage-button-primary" href="/react/pay">
            Tilbage til betaling
          </Link>
        </section>
      </MenuLayout>
    );
  }

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
