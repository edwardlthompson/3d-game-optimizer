#!/usr/bin/env python3
"""Write catalog-v2.sha256 next to catalog-v2.json for Pages and desktop verify."""
from __future__ import annotations

import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "data" / "compatibility" / "catalog-v2.json"
OUTPUT = ROOT / "data" / "compatibility" / "catalog-v2.sha256"


def main() -> int:
    if not CATALOG.exists():
        print("compute-catalog-hash: catalog-v2.json missing")
        return 1

    digest = hashlib.sha256(CATALOG.read_bytes()).hexdigest().upper()
    OUTPUT.write_text(f"{digest}  catalog-v2.json\n", encoding="utf-8")
    print(f"compute-catalog-hash: {digest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
