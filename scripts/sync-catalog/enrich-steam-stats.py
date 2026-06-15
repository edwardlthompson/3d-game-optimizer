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
    url = (
        f"https://store.steampowered.com/api/appdetails"
        f"?appids={app_id}&l=english&cc=US"
    )
    req = urllib.request.Request(url, headers={"User-Agent": "3d-game-optimizer-catalog/1.0"})
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            payload = json.loads(response.read().decode("utf-8"))
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError):
        return None

    entry = payload.get(str(app_id), {})
    if not entry.get("success"):
        return None
    return entry.get("data") or {}


def build_stats(data: dict, existing_tags: list[str]) -> dict:
    stats: dict = {"tags": existing_tags or []}
    if data.get("release_date", {}).get("date"):
        stats["releaseDate"] = data["release_date"]["date"]
    price = data.get("price_overview") or {}
    if price.get("final") is not None:
        stats["priceUsd"] = round(price["final"] / 100, 2)
    recommendations = data.get("recommendations") or {}
    if recommendations.get("total"):
        stats["reviewCount"] = recommendations["total"]
    if data.get("metacritic", {}).get("score"):
        stats["reviewPercent"] = data["metacritic"]["score"]
    genres = [g.get("description") for g in data.get("genres") or [] if g.get("description")]
    if genres:
        stats["tags"] = list(dict.fromkeys([*stats.get("tags", []), *genres]))
    return stats


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
        if float(game.get("steamMatchConfidence") or 0) < 0.92:
            continue

        data = fetch_app_details(int(app_id))
        time.sleep(0.35)
        if not data:
            continue

        game["steamStats"] = build_stats(data, game.get("steamTags") or [])
        if not game.get("purchaseLinks"):
            game["purchaseLinks"] = {"steam": f"https://store.steampowered.com/app/{app_id}/"}
        updated += 1

    CATALOG.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"enrich-steam-stats: enriched {updated} games")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
