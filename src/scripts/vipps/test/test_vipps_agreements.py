from unittest.mock import Mock

import vipps


def test_create_agreement_sends_recurring_configuration(monkeypatch):
    response = Mock()
    response.json.return_value = {
        "agreementId": "agr_test",
        "vippsConfirmationUrl": "https://vipps.example/confirm",
    }
    post = Mock(return_value=response)
    monkeypatch.setattr(vipps.requests, "post", post)
    monkeypatch.setattr(vipps, "api_headers", lambda **kwargs: {"Authorization": "Bearer token"})
    monkeypatch.setattr(vipps, "VIPPS_PHONE_NUMBER", "+4512345678")
    monkeypatch.setattr(vipps, "VIPPS_RETURN_URL", "https://localhost:5001/pay")
    monkeypatch.setattr(
        vipps,
        "VIPPS_MERCHANT_AGREEMENT_URL",
        "https://localhost:5001/account/manage/subscriptions",
    )

    agreement = vipps.create_agreement()

    assert agreement["agreementId"] == "agr_test"
    payload = post.call_args.kwargs["json"]
    assert payload["pricing"] == {"type": "LEGACY", "amount": 100, "currency": "DKK"}
    assert payload["interval"] == {"unit": "YEAR", "count": 1}
    assert payload["phoneNumber"] == "+4512345678"


def test_get_and_list_agreements(monkeypatch):
    response = Mock()
    response.json.side_effect = [{"agreementId": "agr_test"}, {"agreements": []}]
    get = Mock(return_value=response)
    monkeypatch.setattr(vipps.requests, "get", get)
    monkeypatch.setattr(vipps, "api_headers", lambda **kwargs: {"Authorization": "Bearer token"})

    assert vipps.get_agreement("agr_test")["agreementId"] == "agr_test"
    assert vipps.list_agreements() == {"agreements": []}
    assert get.call_count == 2
    assert get.call_args_list[1].kwargs["params"] == {
        "status": "ACTIVE",
        "pageNumber": 1,
        "pageSize": 100,
    }


def test_accept_and_stop_agreement(monkeypatch):
    response = Mock()
    patch = Mock(return_value=response)
    monkeypatch.setattr(vipps.requests, "patch", patch)
    monkeypatch.setattr(vipps, "api_headers", lambda **kwargs: {"Authorization": "Bearer token"})
    monkeypatch.setattr(vipps, "VIPPS_PHONE_NUMBER", "+4512345678")

    vipps.accept_agreement("agr_test")
    vipps.stop_agreement("agr_test")

    assert patch.call_count == 2
    assert patch.call_args_list[0].args[0].endswith("/agreements/agr_test/accept")
    assert patch.call_args_list[0].kwargs["json"] == {"phoneNumber": "+4512345678"}
    assert patch.call_args_list[1].kwargs["json"] == {"status": "STOPPED"}


def test_list_and_get_charges(monkeypatch):
    response = Mock()
    response.json.side_effect = [{"id": "charge_test"}, {"charges": []}]
    get = Mock(return_value=response)
    monkeypatch.setattr(vipps.requests, "get", get)
    monkeypatch.setattr(vipps, "api_headers", lambda **kwargs: {"Authorization": "Bearer token"})

    assert vipps.get_charge("agr_test", "charge_test")["id"] == "charge_test"
    assert vipps.list_charges("agr_test") == {"charges": []}
    assert get.call_args_list[0].args[0].endswith("/agreements/agr_test/charges/charge_test")
    assert get.call_args_list[1].args[0].endswith("/agreements/agr_test/charges")
