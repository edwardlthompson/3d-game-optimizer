#!/usr/bin/env python3
"""Refresh NVIDIA 3D Vision source file from committed official-ready list."""
from __future__ import annotations

import json
from datetime import UTC, datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "data" / "compatibility" / "sources" / "nvidia-official-ready-v1.json"
PCGW_STUB = ROOT / "data" / "compatibility" / "sources" / "pcgw-3dvision-stub-v1.json"


def main() -> int:
    today = datetime.now(UTC).date().isoformat()
    if PCGW_STUB.exists():
        payload = json.loads(PCGW_STUB.read_text(encoding="utf-8"))
        payload["syncedAt"] = today
        payload["syncStatus"] = payload.get("syncStatus", "ok")
    elif OUTPUT.exists():
        payload = json.loads(OUTPUT.read_text(encoding="utf-8"))
        payload["syncedAt"] = today
        payload["syncStatus"] = "ok"
    else:
        payload = {
            "sourceId": "nvidia-3d-vision",
            "syncedAt": today,
            "syncStatus": "degraded",
            "entries": [],
        }

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"scrape-pcgw-3dvision: wrote {len(payload.get('entries', []))} entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
