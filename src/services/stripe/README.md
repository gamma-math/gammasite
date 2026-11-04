# Stripe API service

This standalone helper reads Stripe payout and payment data through the Stripe API. It is separate from the GamMaSite application and follows the same local `.env` and Docker pattern as `src/services/vipps`.

## First setup

Run from the repository root in PowerShell:

```powershell
Push-Location src/services/stripe
py -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
Copy-Item .env.example .env
Pop-Location
```

Open `src/services/stripe/.env` and set `STRIPE_SECRET_API_KEY`. Use a test-mode key for test data and a live-mode key only when you intentionally want to read production data. Never commit `.env` or copy the secret into documentation.

`STRIPE_API_VERSION` is optional. Leave it empty unless an explicit Stripe API version is required.

## Run locally

```powershell
src/services/stripe/.venv/Scripts/python.exe src/services/stripe/stripe.py --check
src/services/stripe/.venv/Scripts/python.exe src/services/stripe/stripe.py --list-payouts --limit 10
src/services/stripe/.venv/Scripts/python.exe src/services/stripe/stripe.py --payout po_...
src/services/stripe/.venv/Scripts/python.exe src/services/stripe/stripe.py --payout-transactions po_... --limit 100
src/services/stripe/.venv/Scripts/python.exe src/services/stripe/stripe.py --payment-intent pi_...
```

The payout transaction command calls Stripe's Balance Transactions API with the payout ID. Its JSON output is the source for a later finance import, including amounts, fees, net amounts and source IDs. Use `--payment-intent` to inspect the metadata on a specific PaymentIntent.

If `STRIPE_PAYOUT_ID` is set in `.env`, the payout ID can be omitted:

```powershell
src/services/stripe/.venv/Scripts/python.exe src/services/stripe/stripe.py --payout-transactions
```

## Run with Docker

```powershell
Push-Location src/services/stripe
docker compose --profile manual run --rm stripe --check
docker compose --profile manual run --rm stripe --list-payouts --limit 10
docker compose --profile manual run --rm stripe --payout-transactions po_... --limit 100
Pop-Location
```

The Docker service only reads `.env` into the container and does not persist or modify Stripe data.

