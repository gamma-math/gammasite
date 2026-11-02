from unittest.mock import Mock

import stripe


def test_api_get_uses_secret_key_and_optional_api_version(monkeypatch):
    response = Mock()
    response.ok = True
    response.json.return_value = {"object": "balance"}
    get = Mock(return_value=response)
    monkeypatch.setattr(stripe.requests, "get", get)
    monkeypatch.setattr(stripe, "STRIPE_SECRET_API_KEY", "sk_test_example")
    monkeypatch.setattr(stripe, "STRIPE_API_VERSION", "2026-10-28")

    assert stripe.api_get("/v1/balance") == {"object": "balance"}

    assert get.call_args.args[0] == "https://api.stripe.com/v1/balance"
    assert get.call_args.kwargs["headers"]["Authorization"] == "Bearer sk_test_example"
    assert get.call_args.kwargs["headers"]["Stripe-Version"] == "2026-10-28"


def test_missing_secret_fails_before_request(monkeypatch):
    monkeypatch.setattr(stripe, "STRIPE_SECRET_API_KEY", "")
    get = Mock()
    monkeypatch.setattr(stripe.requests, "get", get)

    try:
        stripe.get_balance()
        assert False, "Expected SystemExit"
    except SystemExit as exception:
        assert "STRIPE_SECRET_API_KEY" in str(exception)

    get.assert_not_called()


def test_list_payouts_follows_pagination(monkeypatch):
    responses = [
        {"data": [{"id": "po_1"}], "has_more": True},
        {"data": [{"id": "po_2"}], "has_more": False},
    ]
    get = Mock(
        side_effect=[
            Mock(ok=True, json=lambda response=response: response)
            for response in responses
        ]
    )
    monkeypatch.setattr(stripe.requests, "get", get)
    monkeypatch.setattr(stripe, "STRIPE_SECRET_API_KEY", "sk_test_example")

    assert [item["id"] for item in stripe.list_payouts(limit=2)] == ["po_1", "po_2"]
    assert get.call_args_list[1].kwargs["params"]["starting_after"] == "po_1"


def test_list_payout_transactions_filters_by_payout(monkeypatch):
    response = Mock()
    response.ok = True
    response.json.return_value = {
        "data": [{"id": "txn_1", "amount": 30000, "fee": 900, "net": 29100}],
        "has_more": False,
    }
    get = Mock(return_value=response)
    monkeypatch.setattr(stripe.requests, "get", get)
    monkeypatch.setattr(stripe, "STRIPE_SECRET_API_KEY", "sk_test_example")

    transactions = stripe.list_payout_transactions("po_1")

    assert transactions[0]["id"] == "txn_1"
    assert get.call_args.args[0].endswith("/v1/balance_transactions")
    assert get.call_args.kwargs["params"] == {"payout": "po_1", "limit": 25}

