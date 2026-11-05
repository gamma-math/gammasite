import os
from pathlib import Path

from dotenv import load_dotenv


ENV_FILE = Path(__file__).resolve().parent / ".env"
if ENV_FILE.is_file():
    load_dotenv(ENV_FILE)


STRIPE_API_BASE_URL = os.getenv("STRIPE_API_BASE_URL", "https://api.stripe.com").rstrip("/")
STRIPE_PUBLIC_API_KEY = os.getenv("STRIPE_PUBLIC_API_KEY")
STRIPE_SECRET_API_KEY = os.getenv("STRIPE_SECRET_API_KEY")
STRIPE_API_VERSION = os.getenv("STRIPE_API_VERSION")
STRIPE_PAYOUT_ID = os.getenv("STRIPE_PAYOUT_ID")

