from unittest.mock import Mock

import pytest

import vipps


def test_register_webhook_sends_supported_events(monkeypatch):
    response = Mock()
    response.ok = True
    response.json.return_value = {"id": "webhook_new", "secret": "secret_new"}
    post = Mock(return_value=response)
    monkeypatch.setattr(vipps.requests, "post", post)
    monkeypatch.setattr(vipps, "webhook_headers", lambda: {"Authorization": "Bearer token"})

    webhook = vipps.register_webhook("https://test.gam-ma.dk/api/payments/vipps/webhook")

    assert webhook["id"] == "webhook_new"
    payload = post.call_args.kwargs["json"]
    assert payload["url"] == "https://test.gam-ma.dk/api/payments/vipps/webhook"
    assert payload["events"] == vipps.WEBHOOK_EVENTS
    assert "recurring.charge-captured.v1" in payload["events"]


def test_register_webhook_requires_url(monkeypatch):
    monkeypatch.setattr(vipps, "VIPPS_WEBHOOK_URL", None)

    with pytest.raises(AssertionError, match="VIPPS_WEBHOOK_URL"):
        vipps.register_webhook()


def test_list_and_delete_webhooks(monkeypatch):
    list_response = Mock()
    list_response.json.return_value = {"webhooks": [{"id": "webhook_old"}]}
    get = Mock(return_value=list_response)
    delete_response = Mock()
    delete = Mock(return_value=delete_response)
    monkeypatch.setattr(vipps.requests, "get", get)
    monkeypatch.setattr(vipps.requests, "delete", delete)
    monkeypatch.setattr(vipps, "webhook_headers", lambda: {"Authorization": "Bearer token"})

    assert vipps.list_webhooks()["webhooks"][0]["id"] == "webhook_old"
    vipps.delete_webhook("webhook_old")

    assert get.call_args.args[0].endswith("/webhooks/v1/webhooks")
    assert delete.call_args.args[0].endswith("/webhooks/v1/webhooks/webhook_old")


def test_rotate_webhook_verifies_new_registration(monkeypatch):
    replacement = {"id": "webhook_new", "secret": "secret_new"}
    register = Mock(return_value=replacement)
    list_webhooks = Mock(
        return_value={"webhooks": [{"id": "webhook_new", "url": "https://test.example/webhook"}]}
    )
    monkeypatch.setattr(vipps, "register_webhook", register)
    monkeypatch.setattr(vipps, "list_webhooks", list_webhooks)

    result = vipps.rotate_webhook("webhook_old", "https://test.example/webhook")

    assert result == replacement
    register.assert_called_once_with("https://test.example/webhook")
    list_webhooks.assert_called_once_with()


def test_rotate_webhook_rejects_missing_secret(monkeypatch):
    monkeypatch.setattr(vipps, "register_webhook", lambda url: {"id": "webhook_new"})

    with pytest.raises(RuntimeError, match="id and secret"):
        vipps.rotate_webhook("webhook_old", "https://test.example/webhook")
