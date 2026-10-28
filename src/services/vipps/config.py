import os
from pathlib import Path

from dotenv import load_dotenv


ENV_FILE = Path(__file__).resolve().parent / ".env"
if ENV_FILE.is_file():
    load_dotenv(ENV_FILE)

VIPPS_BASE_URL = os.getenv("VIPPS_BASE_URL", "https://apitest.vipps.no").rstrip("/")
VIPPS_CLIENT_ID = os.getenv("VIPPS_CLIENT_ID")
VIPPS_CLIENT_SECRET = os.getenv("VIPPS_CLIENT_SECRET")
VIPPS_SUBSCRIPTION_KEY = os.getenv("VIPPS_OCP_APIM_SUBSCRIPTION_KEY")
VIPPS_MSN = os.getenv("VIPPS_MSN")
VIPPS_PHONE_NUMBER = os.getenv("VIPPS_PHONE_NUMBER")
VIPPS_RETURN_URL = os.getenv("VIPPS_RETURN_URL", "https://localhost:5001/pay")
VIPPS_MERCHANT_AGREEMENT_URL = os.getenv(
    "VIPPS_MERCHANT_AGREEMENT_URL",
    "https://localhost:5001/account/manage/subscriptions",
)
VIPPS_WEBHOOK_URL = os.getenv("VIPPS_WEBHOOK_URL")
VIPPS_PAYMENT_REFERENCE = os.getenv("VIPPS_PAYMENT_REFERENCE")
VIPPS_AGREEMENT_ID = os.getenv("VIPPS_AGREEMENT_ID")
VIPPS_CHARGE_ID = os.getenv("VIPPS_CHARGE_ID")
VIPPS_OLD_WEBHOOK_ID = os.getenv("VIPPS_OLD_WEBHOOK_ID")
