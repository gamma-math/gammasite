from unittest.mock import Mock

import pytest

import vipps


def test_create_payment_returns_reference_and_redirect_url(monkeypatch):
    response = Mock()
    response.json.return_value = {"redirectUrl": "https://vipps.example/pay"}
    post = Mock(return_value=response)
    monkeypatch.setattr(vipps.requests, "post", post)
    monkeypatch.setattr(vipps, "get_access_token", lambda: "access-token")
    monkeypatch.setattr(vipps, "VIPPS_RETURN_URL", "https://localhost:5001/pay")

    payment = vipps.create_payment()

    assert payment["redirectUrl"] == "https://vipps.example/pay"
    assert payment["reference"].startswith("gamma-test-")
    assert post.call_args.args[0] == "https://apitest.vipps.no/epayment/v1/payments"
    assert post.call_args.kwargs["json"]["amount"] == {"currency": "DKK", "value": 100}


def test_get_payment_uses_reference(monkeypatch):
    response = Mock()
    response.json.return_value = {"reference": "payment-reference", "state": "AUTHORIZED"}
    get = Mock(return_value=response)
    monkeypatch.setattr(vipps.requests, "get", get)
    monkeypatch.setattr(vipps, "api_headers", lambda: {"Authorization": "Bearer token"})

    assert vipps.get_payment("payment-reference")["state"] == "AUTHORIZED"
    assert get.call_args.args[0].endswith("/epayment/v1/payments/payment-reference")


def test_capture_payment_requires_authorized_state(monkeypatch):
    monkeypatch.setattr(
        vipps,
        "get_payment",
        lambda reference: {"state": "AUTHORIZED", "amount": {"currency": "DKK", "value": 100}},
    )
    monkeypatch.setattr(vipps, "get_access_token", lambda: "access-token")
    response = Mock()
    response.json.return_value = {"aggregate": {"capturedAmount": {"value": 100}}}
    post = Mock(return_value=response)
    monkeypatch.setattr(vipps.requests, "post", post)

    result = vipps.capture_payment("payment-reference")

    assert result["aggregate"]["capturedAmount"]["value"] == 100
    assert post.call_args.args[0].endswith("/epayment/v1/payments/payment-reference/capture")
    assert post.call_args.kwargs["json"]["modificationAmount"] == {
        "currency": "DKK",
        "value": 100,
    }


def test_capture_payment_rejects_non_authorized_state(monkeypatch):
    monkeypatch.setattr(vipps, "get_payment", lambda reference: {"state": "ABORTED"})

    with pytest.raises(AssertionError, match="not authorized"):
        vipps.capture_payment("payment-reference")
