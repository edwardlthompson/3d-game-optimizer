#!/usr/bin/env python3
"""Enrich catalog-v2.json Steam stats from store API (best-effort, rate-limited)."""
from __future__ import annotations

import json
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "data" / "compatibility" / "catalog-v2.json"


def fetch_app_details(app_id: int) -> dict | None:
    url = f"https://store.steampowered.com/api/appdetails?appids={app_id}&filters=price_overview"
    try:
        with urllib.request.urlopen(url, timeout=20) as response:
            payload = json.loads(response.read().decode("utf-8"))
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError):
        return None

    entry = payload.get(str(app_id), {})
    if not entry.get("success"):
        return None
    return entry.get("data") or {}


def main() -> int:
    if not CATALOG.exists():
        print("enrich-steam-stats: catalog missing — run merge-catalog.py first")
        return 1

    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    updated = 0
    for game in catalog.get("games", []):
        app_id = game.get("steamAppId")
        if not app_id:
            continue
        if game.get("steamStats"):
            continue

        data = fetch_app_details(int(app_id))
        time.sleep(0.35)
        if not data:
            continue

        game["steamStats"] = {
            "tags": game.get("steamTags", []),
        }
        updated += 1

    CATALOG.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"enrich-steam-stats: enriched {updated} games")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
