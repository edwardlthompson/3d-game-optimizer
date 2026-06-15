#!/usr/bin/env python3
"""Enrich catalog-v2.json Steam stats from store + reviews + player count APIs."""
from __future__ import annotations

import argparse
import json
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "data" / "compatibility" / "catalog-v2.json"
USER_AGENT = "3d-game-optimizer-catalog/1.0"


def fetch_json(url: str) -> dict | None:
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            return json.loads(response.read().decode("utf-8"))
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError, ValueError):
        return None


def fetch_app_details(app_id: int) -> dict | None:
    url = (
        f"https://store.steampowered.com/api/appdetails"
        f"?appids={app_id}&l=english&cc=US"
    )
    payload = fetch_json(url)
    if not payload:
        return None
    entry = payload.get(str(app_id), {})
    if not entry.get("success"):
        return None
    return entry.get("data") or {}


def fetch_review_summary(app_id: int) -> tuple[int | None, int | None]:
    url = (
        f"https://store.steampowered.com/appreviews/{app_id}"
        f"?json=1&filter=all&language=english&num_per_page=0"
    )
    payload = fetch_json(url)
    if not payload:
        return None, None
    summary = payload.get("query_summary") or {}
    total = int(summary.get("total_reviews") or 0)
    positive = int(summary.get("total_positive") or 0)
    if total <= 0:
        return None, None
    percent = round(100 * positive / total)
    return percent, total


def fetch_current_players(app_id: int) -> int | None:
    url = (
        "https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/"
        f"?appid={app_id}"
    )
    payload = fetch_json(url)
    if not payload:
        return None
    response = payload.get("response") or {}
    if response.get("result") != 1:
        return None
    count = response.get("player_count")
    return int(count) if count is not None else None


def build_stats(
    details: dict | None,
    reviews: tuple[int | None, int | None],
    players: int | None,
    existing_tags: list[str],
    prior: dict | None,
) -> dict:
    stats: dict = {"tags": list(existing_tags or [])}
    if prior:
        stats.update({k: v for k, v in prior.items() if k != "tags"})
        stats["tags"] = list(existing_tags or prior.get("tags") or [])

    if details:
        if details.get("release_date", {}).get("date"):
            stats["releaseDate"] = details["release_date"]["date"]
        price = details.get("price_overview") or {}
        if price.get("final") is not None:
            stats["priceUsd"] = round(price["final"] / 100, 2)
        genres = [g.get("description") for g in details.get("genres") or [] if g.get("description")]
        if genres:
            stats["tags"] = list(dict.fromkeys([*stats.get("tags", []), *genres]))

    percent, count = reviews
    if percent is not None:
        stats["reviewPercent"] = percent
    if count is not None:
        stats["reviewCount"] = count
    if players is not None:
        stats["currentPlayers"] = players

    return stats


def needs_enrich(game: dict, skip_existing: bool) -> bool:
    if skip_existing:
        stats = game.get("steamStats") or {}
        if stats.get("reviewPercent") is not None and stats.get("currentPlayers") is not None:
            return False
    return True


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--catalog", type=Path, default=CATALOG)
    parser.add_argument("--max", type=int, default=0, help="Max titles (0=all)")
    parser.add_argument("--skip-existing", action="store_true")
    args = parser.parse_args()

    if not args.catalog.exists():
        print("enrich-steam-stats: catalog missing")
        return 1

    catalog = json.loads(args.catalog.read_text(encoding="utf-8"))
    updated = 0
    attempted = 0

    for game in catalog.get("games", []):
        app_id = game.get("steamAppId")
        if not app_id or float(game.get("steamMatchConfidence") or 0) < 0.92:
            continue
        if not needs_enrich(game, args.skip_existing):
            continue
        if args.max and attempted >= args.max:
            break
        attempted += 1

        details = fetch_app_details(int(app_id))
        time.sleep(0.25)
        reviews = fetch_review_summary(int(app_id))
        time.sleep(0.25)
        players = fetch_current_players(int(app_id))
        time.sleep(0.25)

        if not details and reviews == (None, None) and players is None:
            continue

        game["steamStats"] = build_stats(
            details,
            reviews,
            players,
            game.get("steamTags") or [],
            game.get("steamStats"),
        )
        if not game.get("purchaseLinks"):
            game["purchaseLinks"] = {"steam": f"https://store.steampowered.com/app/{app_id}/"}
        updated += 1

    args.catalog.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"enrich-steam-stats: enriched {updated} games ({attempted} attempts)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
