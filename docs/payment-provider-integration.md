# Payment provider integration

## Purpose

Add support for event payments and optional membership subscriptions without coupling the domain model directly to Stripe or MobilePay.

The application should own the payment offer, the relationship between a user and an event, and the current subscription state. Stripe and MobilePay should be isolated behind provider-specific adapters.

The current Stripe integration creates one-time Checkout Sessions. The current MobilePay setup uses external payment links. Neither flow should be used as the long-term integration for event payments because the application needs a reliable callback that can update payment status.

## Provider-neutral model

### Payment offers

Create one `PaymentOffers` table. A row represents one payment option for one logical offer and one provider.

Recommended fields:

```text
Id
OfferKey
ContentItemId nullable
Type                    -- Event or Membership
Provider                -- Stripe or MobilePay
Name
Description
Amount
Currency
BillingInterval nullable
ProviderProductId nullable
ProviderPriceId nullable
ProviderReference nullable
CheckoutUrl nullable
Active
Created
Updated
```

Two rows can share the same `OfferKey`:

```text
OfferKey     Provider    Type          Amount
event-123    Stripe      Event         5000
event-123    MobilePay   Event         5000
membership   Stripe      Membership    15000
membership   MobilePay   Membership    15000
```

`ContentLinks` can point to an internal payment route such as `/react/payment-offers/event-123`. The payment page then loads all active providers for that `OfferKey` and lets the user choose the provider.

### Subscriptions

Subscriptions should be stored separately because they represent an ongoing agreement rather than a single payment.

```text
PaymentSubscriptions
---------------------
Id
UserId
PaymentOfferId
Provider
ProviderSubscriptionId
Status
CurrentPeriodStart nullable
CurrentPeriodEnd nullable
NextChargeAt nullable
LastPaidAt nullable
CancelAtPeriodEnd
Created
Updated
```

`ProviderSubscriptionId` contains the Stripe subscription ID or the MobilePay agreement ID. No Stripe- or MobilePay-specific columns are required on `ContentItems` or `AspNetUsers`.

No local `PaymentTransactions` table is required for the first version. The provider remains the source of payment history, while the application stores the current event and subscription state.

## Event payment flow

1. The user registers for the event.
2. The application creates a checkout/payment request for the selected provider.
3. The request includes the user, event, registration, and payment offer references.
4. The provider confirms the payment through a webhook.
5. The application sets `EventRegistration.Registered = true`.

For Stripe, the event and registration IDs are sent as Checkout metadata. For MobilePay, the payment reference identifies the registration. Webhook processing must be idempotent so duplicate notifications do not create inconsistent state.

## Stripe event payments

Use Stripe Checkout in `payment` mode with the stored one-time Price. Add a Stripe webhook endpoint and handle successful payment events.

The success page is only a user-facing confirmation. It must not be the only place where `EventRegistration.Registered` is changed.

## MobilePay event payments

Use the MobilePay ePayment API instead of a generic payment link. Create a payment with a unique reference, then listen for the captured payment webhook.

The relevant flow is:

```text
Create ePayment
    -> user authorizes in MobilePay
    -> epayments.payment.captured.v1
    -> EventRegistration.Registered = true
```

MobilePay recommends polling the payment as a fallback if a webhook is delayed. This reconciliation should be handled by the provider adapter or a background job.

## Membership subscriptions

### Stripe

Create a recurring Stripe Price and a Checkout Session in `subscription` mode. Store the resulting subscription ID in `PaymentSubscriptions`.

Handle at least:

```text
invoice.paid
invoice.payment_failed
customer.subscription.updated
customer.subscription.deleted
```

Membership access should be activated after a successful invoice payment and deactivated or marked as overdue when the subscription becomes unpaid, canceled, or expired according to the membership rules.

### MobilePay

Use the MobilePay Recurring API to create a payment agreement. The user must approve the agreement in MobilePay before it becomes active.

After the agreement is active, the application must schedule and create recurring charges. MobilePay processes the charge on the requested due date and sends the result back through webhooks.

Handle at least:

```text
recurring.agreement-activated.v1
recurring.agreement-stopped.v1
recurring.agreement-expired.v1
recurring.charge-captured.v1
recurring.charge-failed.v1
```

Unlike Stripe, MobilePay requires the application to schedule future charges. A hosted background job or scheduled worker should find active subscriptions where `NextChargeAt` is due, create the next charge with an idempotency key, and update the subscription after the webhook is received.

## Provider abstraction

The application should depend on a common interface rather than a concrete provider:

```csharp
public interface IPaymentProvider
{
    string Name { get; }

    Task<CheckoutResult> CreateOneTimePaymentAsync(PaymentRequest request);

    Task<SubscriptionResult> CreateSubscriptionAsync(PaymentRequest request);

    Task HandleWebhookAsync(string payload, string signature);
}
```

Provider implementations:

```text
StripePaymentProvider
MobilePayPaymentProvider
```

The frontend should only display payment options that are enabled and supported by the selected provider. Provider secrets, external IDs, webhook validation, retries, and API-specific status mapping must remain inside the provider implementations.

## Implementation tasks

| Task | Status | Feature branch | Description |
| --- | --- | --- | --- |
| Provider-neutral payment offers | Backlog | `feature/payment-offer-model` | Add `PaymentOffers`, connect payment offers to `ContentLinks`, and expose provider-neutral payment selection. |
| Stripe event payments | Backlog | `feature/stripe-event-payments` | Create Stripe Checkout Sessions for event offers and update `EventRegistration.Registered` from a verified webhook. |
| MobilePay event payments | Backlog | `feature/mobilepay-event-payments` | Integrate MobilePay ePayment API and captured-payment webhooks for event registrations. |
| Membership subscriptions | Backlog | `feature/membership-subscriptions` | Add Stripe subscriptions and MobilePay Recurring agreements, including MobilePay charge scheduling and webhook status updates. |

## Acceptance criteria

- Payment offers can support Stripe and MobilePay without adding provider-specific fields to `ContentItems` or `AspNetUsers`.
- A user can choose an enabled payment provider for an event.
- Event payment status is changed only after confirmed payment from the provider.
- Duplicate or delayed webhooks do not create duplicate state changes.
- Stripe subscription renewals update membership status through webhooks.
- MobilePay recurring agreements and charges update membership status through webhooks.
- MobilePay recurring charges are created automatically by a scheduled backend process.
- Failed, canceled, expired, and refunded payments do not remain incorrectly marked as paid.
- Provider credentials and webhook secrets are stored outside source control.

## References

- [Stripe subscriptions](https://docs.stripe.com/billing/subscriptions/overview)
- [Stripe subscription webhooks](https://docs.stripe.com/billing/subscriptions/webhooks)
- [Vipps MobilePay Recurring API](https://developer.vippsmobilepay.com/docs/APIs/recurring-api/)
- [Vipps MobilePay Recurring API guide](https://developer.vippsmobilepay.com/docs/APIs/recurring-api/recurring-api-guide/)
- [Vipps MobilePay ePayment webhooks](https://developer.vippsmobilepay.com/docs/APIs/epayment-api/api-guide/webhooks/)
- [Vipps MobilePay Webhooks API](https://developer.vippsmobilepay.com/docs/APIs/webhooks-api/api-guide/)
