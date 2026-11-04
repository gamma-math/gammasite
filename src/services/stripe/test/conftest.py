import sys
from pathlib import Path


STRIPE_DIRECTORY = Path(__file__).resolve().parents[1]
if str(STRIPE_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(STRIPE_DIRECTORY))

