from unittest.mock import Mock

import pytest

import vipps


def test_cli_uses_configured_payment_reference(monkeypatch):
    get_payment = Mock(return_value={"state": "CAPTURED", "aggregate": {}})
    monkeypatch.setattr(vipps, "get_payment", get_payment)
    monkeypatch.setattr(vipps, "VIPPS_PAYMENT_REFERENCE", "payment-from-env")

    vipps.run_cli(["--status"])

    get_payment.assert_called_once_with("payment-from-env")


def test_cli_requires_old_webhook_id(monkeypatch):
    monkeypatch.setattr(vipps, "VIPPS_OLD_WEBHOOK_ID", None)

    with pytest.raises(SystemExit, match="VIPPS_OLD_WEBHOOK_ID"):
        vipps.run_cli(["--delete-webhook"])
