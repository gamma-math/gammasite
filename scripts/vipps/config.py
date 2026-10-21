import os
from pathlib import Path

from dotenv import load_dotenv


ENV_FILE = Path(__file__).resolve().parent / ".env"
assert ENV_FILE.is_file(), (
    f"Missing environment file: {ENV_FILE}. "
    "Copy .env.example to .env and fill in the values."
)

load_dotenv(ENV_FILE)

REQUIRED_VARIABLES = (
    "VIPPS_CLIENT_ID",
    "VIPPS_CLIENT_SECRET",
    "VIPPS_OCP_APIM_SUBSCRIPTION_KEY",
    "VIPPS_MSN",
)

for variable in REQUIRED_VARIABLES:
    assert os.getenv(variable), f"Missing {variable} in {ENV_FILE}"


VIPPS_BASE_URL = os.getenv("VIPPS_BASE_URL", "https://apitest.vipps.no").rstrip("/")
VIPPS_CLIENT_ID = os.environ["VIPPS_CLIENT_ID"]
VIPPS_CLIENT_SECRET = os.environ["VIPPS_CLIENT_SECRET"]
VIPPS_SUBSCRIPTION_KEY = os.environ["VIPPS_OCP_APIM_SUBSCRIPTION_KEY"]
VIPPS_MSN = os.environ["VIPPS_MSN"]
VIPPS_PHONE_NUMBER = os.getenv("VIPPS_PHONE_NUMBER")
VIPPS_RETURN_URL = os.getenv("VIPPS_RETURN_URL", "https://localhost:5001/react/pay")
VIPPS_MERCHANT_AGREEMENT_URL = os.getenv(
    "VIPPS_MERCHANT_AGREEMENT_URL",
    "https://localhost:5001/react/account/manage/subscriptions",
)
VIPPS_WEBHOOK_URL = os.getenv("VIPPS_WEBHOOK_URL")
VIPPS_PAYMENT_REFERENCE = os.getenv("VIPPS_PAYMENT_REFERENCE")
VIPPS_AGREEMENT_ID = os.getenv("VIPPS_AGREEMENT_ID")
VIPPS_CHARGE_ID = os.getenv("VIPPS_CHARGE_ID")
VIPPS_OLD_WEBHOOK_ID = os.getenv("VIPPS_OLD_WEBHOOK_ID")
