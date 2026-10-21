from unittest.mock import Mock

import pytest

import vipps


def test_get_access_token_posts_credentials(monkeypatch):
    response = Mock()
    response.json.return_value = {"access_token": "access-token"}
    monkeypatch.setattr(vipps.requests, "post", Mock(return_value=response))
    monkeypatch.setattr(vipps, "VIPPS_CLIENT_ID", "client-id")
    monkeypatch.setattr(vipps, "VIPPS_CLIENT_SECRET", "client-secret")
    monkeypatch.setattr(vipps, "VIPPS_SUBSCRIPTION_KEY", "subscription-key")
    monkeypatch.setattr(vipps, "VIPPS_MSN", "2070420")

    assert vipps.get_access_token() == "access-token"

    request = vipps.requests.post.call_args
    assert request.args[0] == "https://apitest.vipps.no/accesstoken/get"
    assert request.kwargs["headers"]["client_id"] == "client-id"
    assert request.kwargs["headers"]["Merchant-Serial-Number"] == "2070420"
    response.raise_for_status.assert_called_once_with()


def test_missing_api_configuration_fails_before_request(monkeypatch):
    monkeypatch.setattr(vipps, "VIPPS_CLIENT_ID", "")
    monkeypatch.setattr(vipps.requests, "post", Mock())

    with pytest.raises(SystemExit, match="VIPPS_CLIENT_ID"):
        vipps.get_access_token()

    vipps.requests.post.assert_not_called()


def test_api_headers_include_optional_idempotency_key(monkeypatch):
    monkeypatch.setattr(vipps, "get_access_token", lambda: "access-token")
    monkeypatch.setattr(vipps, "VIPPS_SUBSCRIPTION_KEY", "subscription-key")
    monkeypatch.setattr(vipps, "VIPPS_MSN", "2070420")

    headers = vipps.api_headers(idempotency_key="idempotency-key")

    assert headers["Authorization"] == "Bearer access-token"
    assert headers["Idempotency-Key"] == "idempotency-key"
