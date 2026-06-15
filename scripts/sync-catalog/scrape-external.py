#!/usr/bin/env python3
"""Legacy entry point — delegates to dedicated scrapers."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = [
    "scrape-truegame.py",
    "scrape-uevr-profiles.py",
    "scrape-vrto3d-wiki.py",
]


def main() -> int:
    code = 0
    for name in SCRIPTS:
        path = ROOT / "scripts" / "sync-catalog" / name
        result = subprocess.run([sys.executable, str(path)], check=False)
        if result.returncode != 0:
            code = result.returncode
    return code


if __name__ == "__main__":
    raise SystemExit(main())
