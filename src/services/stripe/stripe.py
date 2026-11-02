import argparse
import json
from typing import Any

import requests

from config import (
    STRIPE_API_BASE_URL,
    STRIPE_API_VERSION,
    STRIPE_PAYOUT_ID,
    STRIPE_SECRET_API_KEY,
)


REQUEST_TIMEOUT = 20


def validate_api_config() -> None:
    if not STRIPE_SECRET_API_KEY:
        raise SystemExit("Missing STRIPE_SECRET_API_KEY in src/services/stripe/.env")


def api_headers() -> dict[str, str]:
    validate_api_config()
    headers = {
        "Authorization": f"Bearer {STRIPE_SECRET_API_KEY}",
        "Accept": "application/json",
    }
    if STRIPE_API_VERSION:
        headers["Stripe-Version"] = STRIPE_API_VERSION
    return headers


def api_get(path: str, params: dict[str, Any] | None = None) -> dict[str, Any]:
    response = requests.get(
        f"{STRIPE_API_BASE_URL}{path}",
        headers=api_headers(),
        params=params,
        timeout=REQUEST_TIMEOUT,
    )
    if not response.ok:
        raise RuntimeError(
            f"Stripe API request failed ({response.status_code}): {response.text}"
        )
    return response.json()


def get_balance() -> dict[str, Any]:
    return api_get("/v1/balance")


def get_payout(payout_id: str) -> dict[str, Any]:
    return api_get(f"/v1/payouts/{payout_id}")


def list_payouts(
    *,
    limit: int = 25,
    created_gte: int | None = None,
    created_lte: int | None = None,
) -> list[dict[str, Any]]:
    if limit < 1:
        raise ValueError("limit must be at least 1")

    payouts: list[dict[str, Any]] = []
    starting_after: str | None = None

    while len(payouts) < limit:
        page_limit = min(100, limit - len(payouts))
        params: dict[str, Any] = {"limit": page_limit}
        if created_gte is not None:
            params["created[gte]"] = created_gte
        if created_lte is not None:
            params["created[lte]"] = created_lte
        if starting_after:
            params["starting_after"] = starting_after

        page = api_get("/v1/payouts", params)
        page_items = page.get("data", [])
        payouts.extend(page_items)

        if not page.get("has_more") or not page_items:
            break
        starting_after = page_items[-1].get("id")
        if not starting_after:
            break

    return payouts[:limit]


def list_payout_transactions(
    payout_id: str,
    *,
    limit: int = 25,
) -> list[dict[str, Any]]:
    if limit < 1:
        raise ValueError("limit must be at least 1")

    transactions: list[dict[str, Any]] = []
    starting_after: str | None = None

    while len(transactions) < limit:
        page_limit = min(100, limit - len(transactions))
        params: dict[str, Any] = {"payout": payout_id, "limit": page_limit}
        if starting_after:
            params["starting_after"] = starting_after

        page = api_get("/v1/balance_transactions", params)
        page_items = page.get("data", [])
        transactions.extend(page_items)

        if not page.get("has_more") or not page_items:
            break
        starting_after = page_items[-1].get("id")
        if not starting_after:
            break

    return transactions[:limit]


def list_balance_transactions(
    *,
    limit: int = 25,
    created_gte: int | None = None,
    created_lte: int | None = None,
) -> list[dict[str, Any]]:
    if limit < 1:
        raise ValueError("limit must be at least 1")

    transactions: list[dict[str, Any]] = []
    starting_after: str | None = None

    while len(transactions) < limit:
        page_limit = min(100, limit - len(transactions))
        params: dict[str, Any] = {"limit": page_limit}
        if created_gte is not None:
            params["created[gte]"] = created_gte
        if created_lte is not None:
            params["created[lte]"] = created_lte
        if starting_after:
            params["starting_after"] = starting_after

        page = api_get("/v1/balance_transactions", params)
        page_items = page.get("data", [])
        transactions.extend(page_items)

        if not page.get("has_more") or not page_items:
            break
        starting_after = page_items[-1].get("id")
        if not starting_after:
            break

    return transactions[:limit]


def get_balance_transaction(transaction_id: str) -> dict[str, Any]:
    return api_get(f"/v1/balance_transactions/{transaction_id}")


def get_payment_intent(payment_intent_id: str) -> dict[str, Any]:
    return api_get(f"/v1/payment_intents/{payment_intent_id}")


def print_json(value: Any) -> None:
    print(json.dumps(value, indent=2, ensure_ascii=False, sort_keys=True))


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Read Stripe payout and payment data.")
    actions = parser.add_mutually_exclusive_group()
    actions.add_argument("--check", action="store_true", help="Check the Stripe API connection.")
    actions.add_argument("--list-payouts", action="store_true", help="List Stripe payouts.")
    actions.add_argument("--payout", metavar="PAYOUT_ID", help="Retrieve one payout.")
    actions.add_argument(
        "--payout-transactions",
        nargs="?",
        const="",
        metavar="PAYOUT_ID",
        help="List balance transactions included in one payout.",
    )
    actions.add_argument(
        "--list-balance-transactions",
        action="store_true",
        help="List account balance transactions.",
    )
    actions.add_argument(
        "--balance-transaction",
        metavar="TRANSACTION_ID",
        help="Retrieve one balance transaction.",
    )
    actions.add_argument(
        "--payment-intent",
        metavar="PAYMENT_INTENT_ID",
        help="Retrieve one PaymentIntent, including its metadata.",
    )
    parser.add_argument("--limit", type=int, default=25, help="Maximum number of rows to fetch.")
    parser.add_argument("--created-gte", type=int, help="Only include Unix timestamps after this value.")
    parser.add_argument("--created-lte", type=int, help="Only include Unix timestamps before this value.")
    return parser


def run_cli(argv: list[str]) -> None:
    args = build_parser().parse_args(argv)

    if not any(
        (
            args.check,
            args.list_payouts,
            args.payout,
            args.payout_transactions is not None,
            args.list_balance_transactions,
            args.balance_transaction,
            args.payment_intent,
        )
    ):
        args.check = True

    if args.check:
        balance = get_balance()
        available = balance.get("available", [])
        currencies = ", ".join(
            f"{item.get('amount', 0)} {str(item.get('currency', '')).upper()}"
            for item in available
        )
        print(f"Stripe API connection OK{': ' + currencies if currencies else ''}")
    elif args.list_payouts:
        print_json(
            list_payouts(
                limit=args.limit,
                created_gte=args.created_gte,
                created_lte=args.created_lte,
            )
        )
    elif args.payout:
        print_json(get_payout(args.payout))
    elif args.payout_transactions is not None:
        payout_id = args.payout_transactions or STRIPE_PAYOUT_ID
        if not payout_id:
            raise SystemExit(
                "Missing payout ID. Pass it to --payout-transactions or set STRIPE_PAYOUT_ID."
            )
        print_json(list_payout_transactions(payout_id, limit=args.limit))
    elif args.list_balance_transactions:
        print_json(
            list_balance_transactions(
                limit=args.limit,
                created_gte=args.created_gte,
                created_lte=args.created_lte,
            )
        )
    elif args.balance_transaction:
        print_json(get_balance_transaction(args.balance_transaction))
    elif args.payment_intent:
        print_json(get_payment_intent(args.payment_intent))


if __name__ == "__main__":
    import sys

    run_cli(sys.argv[1:])

