import sys
from pathlib import Path


VIPPS_DIRECTORY = Path(__file__).resolve().parents[1]
if str(VIPPS_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(VIPPS_DIRECTORY))
