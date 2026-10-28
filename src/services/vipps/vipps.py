import json
import sys
import uuid

import requests

from config import (
    VIPPS_BASE_URL,
    VIPPS_CLIENT_ID,
    VIPPS_CLIENT_SECRET,
    VIPPS_MSN,
    VIPPS_MERCHANT_AGREEMENT_URL,
    VIPPS_AGREEMENT_ID,
    VIPPS_CHARGE_ID,
    VIPPS_OLD_WEBHOOK_ID,
    VIPPS_PAYMENT_REFERENCE,
    VIPPS_PHONE_NUMBER,
    VIPPS_RETURN_URL,
    VIPPS_SUBSCRIPTION_KEY,
    VIPPS_WEBHOOK_URL,
)


WEBHOOK_EVENTS = [
    "epayments.payment.authorized.v1",
    "epayments.payment.captured.v1",
    "epayments.payment.aborted.v1",
    "epayments.payment.expired.v1",
    "recurring.agreement-activated.v1",
    "recurring.agreement-rejected.v1",
    "recurring.agreement-stopped.v1",
    "recurring.agreement-expired.v1",
    "recurring.charge-reserved.v1",
    "recurring.charge-captured.v1",
    "recurring.charge-canceled.v1",
    "recurring.charge-refunded.v1",
    "recurring.charge-failed.v1",
    "recurring.charge-creation-failed.v1",
]


def validate_api_config() -> None:
    configured = {
        "VIPPS_CLIENT_ID": VIPPS_CLIENT_ID,
        "VIPPS_CLIENT_SECRET": VIPPS_CLIENT_SECRET,
        "VIPPS_OCP_APIM_SUBSCRIPTION_KEY": VIPPS_SUBSCRIPTION_KEY,
        "VIPPS_MSN": VIPPS_MSN,
    }
    missing = [name for name, value in configured.items() if not value]
    if missing:
        raise SystemExit(f"Missing {', '.join(missing)} in src/services/vipps/.env")


def required_value(argument: str | None, configured: str | None, name: str) -> str:
    value = argument or configured
    if not value:
        raise SystemExit(f"Missing {name} in src/services/vipps/.env")
    return value


def webhook_headers() -> dict:
    return {
        "Authorization": f"Bearer {get_access_token()}",
        "Ocp-Apim-Subscription-Key": VIPPS_SUBSCRIPTION_KEY,
        "Merchant-Serial-Number": VIPPS_MSN,
        "Vipps-System-Name": "gamma",
        "Vipps-System-Version": "1.0.0",
    }


def get_access_token() -> str:
    validate_api_config()
    response = requests.post(
        f"{VIPPS_BASE_URL}/accesstoken/get",
        headers={
            "client_id": VIPPS_CLIENT_ID,
            "client_secret": VIPPS_CLIENT_SECRET,
            "Ocp-Apim-Subscription-Key": VIPPS_SUBSCRIPTION_KEY,
            "Merchant-Serial-Number": VIPPS_MSN,
            "Vipps-System-Name": "gamma",
            "Vipps-System-Version": "1.0.0",
        },
        timeout=15,
    )
    response.raise_for_status()

    token = response.json().get("access_token")
    assert token, "Vipps response did not contain an access_token"
    return token


def api_headers(*, idempotency_key: str | None = None) -> dict:
    headers = {
        "Authorization": f"Bearer {get_access_token()}",
        "Ocp-Apim-Subscription-Key": VIPPS_SUBSCRIPTION_KEY,
        "Merchant-Serial-Number": VIPPS_MSN,
        "Vipps-System-Name": "gamma",
        "Vipps-System-Version": "1.0.0",
    }
    if idempotency_key:
        headers["Idempotency-Key"] = idempotency_key
    return headers


def create_payment() -> dict:
    assert VIPPS_RETURN_URL, "Missing VIPPS_RETURN_URL in src/services/vipps/.env"

    reference = f"gamma-test-{uuid.uuid4().hex[:16]}"
    response = requests.post(
        f"{VIPPS_BASE_URL}/epayment/v1/payments",
        headers={
            "Authorization": f"Bearer {get_access_token()}",
            "Ocp-Apim-Subscription-Key": VIPPS_SUBSCRIPTION_KEY,
            "Merchant-Serial-Number": VIPPS_MSN,
            "Idempotency-Key": str(uuid.uuid4()),
            "Vipps-System-Name": "gamma",
            "Vipps-System-Version": "1.0.0",
        },
        json={
            "amount": {"currency": "DKK", "value": 100},
            "paymentMethod": {"type": "WALLET"},
            "reference": reference,
            "userFlow": "WEB_REDIRECT",
            "returnUrl": VIPPS_RETURN_URL,
            "paymentDescription": "GamMa sandbox test",
        },
        timeout=15,
    )
    response.raise_for_status()

    payment = response.json()
    assert payment.get("redirectUrl"), "Vipps response did not contain a redirectUrl"
    payment["reference"] = reference
    return payment


def get_payment(reference: str) -> dict:
    response = requests.get(
        f"{VIPPS_BASE_URL}/epayment/v1/payments/{reference}",
        headers=api_headers(),
        timeout=15,
    )
    response.raise_for_status()
    return response.json()


def capture_payment(reference: str) -> dict:
    payment = get_payment(reference)
    state = payment.get("state")
    if isinstance(state, list):
        state = state[-1] if state else None
    assert state == "AUTHORIZED", f"Payment is not authorized: {state}"

    response = requests.post(
        f"{VIPPS_BASE_URL}/epayment/v1/payments/{reference}/capture",
        headers={
            "Authorization": f"Bearer {get_access_token()}",
            "Ocp-Apim-Subscription-Key": VIPPS_SUBSCRIPTION_KEY,
            "Merchant-Serial-Number": VIPPS_MSN,
            "Idempotency-Key": str(uuid.uuid4()),
            "Vipps-System-Name": "gamma",
            "Vipps-System-Version": "1.0.0",
        },
        json={"modificationAmount": payment["amount"]},
        timeout=15,
    )
    response.raise_for_status()
    return response.json()


def register_webhook(url: str | None = None) -> dict:
    url = url or VIPPS_WEBHOOK_URL
    assert url, "Missing VIPPS_WEBHOOK_URL in src/services/vipps/.env"

    response = requests.post(
        f"{VIPPS_BASE_URL}/webhooks/v1/webhooks",
        headers=webhook_headers(),
        json={
            "url": url,
            "events": WEBHOOK_EVENTS,
        },
        timeout=15,
    )
    if not response.ok:
        raise RuntimeError(
            f"Vipps webhook registration failed ({response.status_code}): {response.text}"
        )
    response.raise_for_status()
    return response.json()


def create_agreement() -> dict:
    assert VIPPS_PHONE_NUMBER, "Missing VIPPS_PHONE_NUMBER in src/services/vipps/.env"
    assert VIPPS_RETURN_URL, "Missing VIPPS_RETURN_URL in src/services/vipps/.env"
    assert VIPPS_MERCHANT_AGREEMENT_URL, (
        "Missing VIPPS_MERCHANT_AGREEMENT_URL in src/services/vipps/.env"
    )

    external_id = f"gamma-test-{uuid.uuid4().hex[:16]}"
    response = requests.post(
        f"{VIPPS_BASE_URL}/recurring/v3/agreements",
        headers=api_headers(idempotency_key=str(uuid.uuid4())),
        json={
            "pricing": {"type": "LEGACY", "amount": 100, "currency": "DKK"},
            "interval": {"unit": "YEAR", "count": 1},
            "merchantRedirectUrl": VIPPS_RETURN_URL,
            "merchantAgreementUrl": VIPPS_MERCHANT_AGREEMENT_URL,
            "phoneNumber": VIPPS_PHONE_NUMBER,
            "productName": "GamMa test membership",
            "productDescription": "GamMa recurring sandbox test",
            "externalId": external_id,
        },
        timeout=15,
    )
    response.raise_for_status()

    agreement = response.json()
    assert agreement.get("agreementId"), "Vipps response did not contain an agreementId"
    assert agreement.get("vippsConfirmationUrl"), (
        "Vipps response did not contain a vippsConfirmationUrl"
    )
    return agreement


def get_agreement(agreement_id: str) -> dict:
    response = requests.get(
        f"{VIPPS_BASE_URL}/recurring/v3/agreements/{agreement_id}",
        headers=api_headers(),
        timeout=15,
    )
    response.raise_for_status()
    return response.json()


def list_agreements() -> dict:
    response = requests.get(
        f"{VIPPS_BASE_URL}/recurring/v3/agreements",
        headers=api_headers(),
        params={"status": "ACTIVE", "pageNumber": 1, "pageSize": 100},
        timeout=15,
    )
    response.raise_for_status()
    return response.json()


def accept_agreement(agreement_id: str) -> None:
    assert VIPPS_PHONE_NUMBER, "Missing VIPPS_PHONE_NUMBER in src/services/vipps/.env"
    response = requests.patch(
        f"{VIPPS_BASE_URL}/recurring/v3/agreements/{agreement_id}/accept",
        headers=api_headers(idempotency_key=str(uuid.uuid4())),
        json={"phoneNumber": VIPPS_PHONE_NUMBER},
        timeout=15,
    )
    response.raise_for_status()


def stop_agreement(agreement_id: str) -> None:
    response = requests.patch(
        f"{VIPPS_BASE_URL}/recurring/v3/agreements/{agreement_id}",
        headers=api_headers(idempotency_key=str(uuid.uuid4())),
        json={"status": "STOPPED"},
        timeout=15,
    )
    response.raise_for_status()


def get_charge(agreement_id: str, charge_id: str) -> dict:
    response = requests.get(
        f"{VIPPS_BASE_URL}/recurring/v3/agreements/{agreement_id}/charges/{charge_id}",
        headers=api_headers(),
        timeout=15,
    )
    response.raise_for_status()
    return response.json()


def list_charges(agreement_id: str) -> dict:
    response = requests.get(
        f"{VIPPS_BASE_URL}/recurring/v3/agreements/{agreement_id}/charges",
        headers=api_headers(),
        timeout=15,
    )
    response.raise_for_status()
    return response.json()


def list_webhooks() -> dict:
    response = requests.get(
        f"{VIPPS_BASE_URL}/webhooks/v1/webhooks",
        headers=webhook_headers(),
        timeout=15,
    )
    response.raise_for_status()
    return response.json()


def delete_webhook(webhook_id: str) -> None:
    response = requests.delete(
        f"{VIPPS_BASE_URL}/webhooks/v1/webhooks/{webhook_id}",
        headers=webhook_headers(),
        timeout=15,
    )
    response.raise_for_status()


def rotate_webhook(old_webhook_id: str, url: str | None = None) -> dict:
    """Create a replacement webhook and deliberately leave the old one active.

    The old webhook must remain active until the new secret has been deployed
    and a delivery has been verified. Deletion is therefore still explicit.
    """
    replacement = register_webhook(url)
    replacement_id = replacement.get("id")
    if not replacement_id or not replacement.get("secret"):
        raise RuntimeError("Vipps did not return both webhook id and secret")

    configured_url = url or VIPPS_WEBHOOK_URL
    registered = next(
        (
            webhook
            for webhook in list_webhooks().get("webhooks", [])
            if webhook.get("id") == replacement_id
        ),
        None,
    )
    if not registered or registered.get("url") != configured_url:
        raise RuntimeError("The replacement webhook could not be verified")

    print(f"Replacement webhook registered: {replacement_id}")
    print(f"Webhook URL verified: {configured_url}")
    print(f"Store this value as VIPPS_WEBHOOK_SECRET: {replacement['secret']}")
    print(f"Keep the old webhook active until deployment is verified: {old_webhook_id}")
    print(f"Then delete it with: --delete-webhook {old_webhook_id}")
    return replacement


def run_cli(args: list[str]) -> None:
    if not args:
        get_access_token()
        print("Vipps sandbox connection OK")
    elif args[0] == "--check" and len(args) == 1:
        get_access_token()
        print("Vipps sandbox connection OK")
    elif args[0] == "--payment" and len(args) == 1:
        payment = create_payment()
        print(f"Payment created: {payment['reference']}")
        print(f"Open this URL: {payment['redirectUrl']}")
    elif args[0] == "--status" and len(args) in (1, 2):
        reference = required_value(
            args[1] if len(args) == 2 else None,
            VIPPS_PAYMENT_REFERENCE,
            "VIPPS_PAYMENT_REFERENCE",
        )
        payment = get_payment(reference)
        print(f"Payment state: {payment.get('state')}")
        aggregate = payment.get("aggregate", {})
        for label, key in (
            ("Authorized", "authorizedAmount"),
            ("Captured", "capturedAmount"),
            ("Cancelled", "cancelledAmount"),
            ("Refunded", "refundedAmount"),
        ):
            amount = aggregate.get(key, {})
            print(f"{label}: {amount.get('value', 0)} {amount.get('currency', '')}".strip())
    elif args[0] == "--capture" and len(args) in (1, 2):
        reference = required_value(
            args[1] if len(args) == 2 else None,
            VIPPS_PAYMENT_REFERENCE,
            "VIPPS_PAYMENT_REFERENCE",
        )
        payment = capture_payment(reference)
        print(f"Payment captured: {payment.get('aggregate', {}).get('capturedAmount')}")
    elif args[0] == "--create-agreement" and len(args) == 1:
        agreement = create_agreement()
        print(f"Agreement created: {agreement['agreementId']}")
        print(f"Open this URL: {agreement['vippsConfirmationUrl']}")
    elif args[0] == "--agreement-status" and len(args) in (1, 2):
        agreement_id = required_value(
            args[1] if len(args) == 2 else None,
            VIPPS_AGREEMENT_ID,
            "VIPPS_AGREEMENT_ID",
        )
        agreement = get_agreement(agreement_id)
        print(f"Agreement status: {agreement.get('status')}")
    elif args[0] == "--list-agreements" and len(args) == 1:
        print(json.dumps(list_agreements(), indent=2, ensure_ascii=False))
    elif args[0] == "--accept-agreement" and len(args) in (1, 2):
        agreement_id = required_value(
            args[1] if len(args) == 2 else None,
            VIPPS_AGREEMENT_ID,
            "VIPPS_AGREEMENT_ID",
        )
        accept_agreement(agreement_id)
        print(f"Agreement accepted: {agreement_id}")
    elif args[0] == "--stop-agreement" and len(args) in (1, 2):
        agreement_id = required_value(
            args[1] if len(args) == 2 else None,
            VIPPS_AGREEMENT_ID,
            "VIPPS_AGREEMENT_ID",
        )
        stop_agreement(agreement_id)
        print(f"Agreement stopped: {agreement_id}")
    elif args[0] == "--charge-status" and len(args) in (1, 2, 3):
        agreement_id = required_value(
            args[1] if len(args) >= 2 else None,
            VIPPS_AGREEMENT_ID,
            "VIPPS_AGREEMENT_ID",
        )
        charge_id = required_value(
            args[2] if len(args) == 3 else None,
            VIPPS_CHARGE_ID,
            "VIPPS_CHARGE_ID",
        )
        charge = get_charge(agreement_id, charge_id)
        print(f"Charge status: {charge.get('status')}")
    elif args[0] == "--list-charges" and len(args) in (1, 2):
        agreement_id = required_value(
            args[1] if len(args) == 2 else None,
            VIPPS_AGREEMENT_ID,
            "VIPPS_AGREEMENT_ID",
        )
        print(json.dumps(list_charges(agreement_id), indent=2, ensure_ascii=False))
    elif args[0] == "--register-webhook" and len(args) == 1:
        webhook = register_webhook()
        print(f"Webhook registered: {webhook.get('id')}")
        print(f"Store this value as VIPPS_WEBHOOK_SECRET: {webhook.get('secret')}")
    elif args[0] == "--rotate-webhook" and len(args) in (1, 2, 3):
        old_webhook_id = required_value(
            args[1] if len(args) >= 2 else None,
            VIPPS_OLD_WEBHOOK_ID,
            "VIPPS_OLD_WEBHOOK_ID",
        )
        url = args[2] if len(args) == 3 else None
        rotate_webhook(old_webhook_id, url)
    elif args[0] == "--list-webhooks" and len(args) == 1:
        for webhook in list_webhooks().get("webhooks", []):
            print(f"{webhook.get('id')}: {webhook.get('url')} ({', '.join(webhook.get('events', []))})")
    elif args[0] == "--delete-webhook" and len(args) in (1, 2):
        webhook_id = required_value(
            args[1] if len(args) == 2 else None,
            VIPPS_OLD_WEBHOOK_ID,
            "VIPPS_OLD_WEBHOOK_ID",
        )
        delete_webhook(webhook_id)
        print(f"Webhook deleted: {webhook_id}")
    else:
        raise SystemExit(
            "Usage: vipps.py [--check | --payment | --status REFERENCE | "
            "--capture REFERENCE | --create-agreement | "
            "--agreement-status AGREEMENT_ID | --accept-agreement AGREEMENT_ID | "
            "--list-agreements | "
            "--stop-agreement AGREEMENT_ID | "
            "--charge-status AGREEMENT_ID CHARGE_ID | "
            "--list-charges AGREEMENT_ID | "
            "--register-webhook | --rotate-webhook OLD_WEBHOOK_ID [URL] | "
            "--list-webhooks | --delete-webhook ID]"
        )


if __name__ == "__main__":
    run_cli(sys.argv[1:])
