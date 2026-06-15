#!/usr/bin/env python3
"""Resolve Steam app IDs for catalog titles missing steamAppId."""
from __future__ import annotations

import argparse
import json
import re
import time
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "data" / "compatibility" / "catalog-v2.json"
LOCK = ROOT / "data" / "compatibility" / "catalog-v2.lock.json"

STRIP_SUFFIXES = (
    " remastered",
    " definitive edition",
    " complete edition",
    " goty",
    " game of the year",
    " enhanced",
    " special edition",
    " director's cut",
    " directors cut",
)


def normalize_title(title: str) -> str:
    text = title.lower()
    text = re.sub(r"[™®©]", "", text)
    for suffix in STRIP_SUFFIXES:
        if text.endswith(suffix):
            text = text[: -len(suffix)]
    text = re.sub(r"[^a-z0-9]+", " ", text)
    return text.strip()


def search_steam(title: str) -> tuple[int | None, float]:
    term = urllib.parse.quote(title)
    url = f"https://store.steampowered.com/api/storesearch/?term={term}&l=english&cc=US"
    req = urllib.request.Request(url, headers={"User-Agent": "3d-game-optimizer-catalog/1.0"})
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            payload = json.loads(response.read().decode("utf-8"))
    except Exception:
        return None, 0.0

    items = payload.get("items") or []
    if not items:
        return None, 0.0

    target = normalize_title(title)
    best_id = None
    best_score = 0.0
    for item in items[:8]:
        candidate = normalize_title(item.get("name", ""))
        if not candidate:
            continue
        if candidate == target:
            return int(item["id"]), 1.0
        overlap = len(set(target.split()) & set(candidate.split()))
        score = overlap / max(len(target.split()), 1)
        if target in candidate or candidate in target:
            score = max(score, 0.85)
        if score > best_score:
            best_score = score
            best_id = int(item["id"])
    return best_id, best_score


def load_lock() -> dict[str, int]:
    if not LOCK.exists():
        return {}
    data = json.loads(LOCK.read_text(encoding="utf-8"))
    locked: dict[str, int] = {}
    for item in data.get("lockedAppIds", []):
        if isinstance(item, dict):
            locked[str(item["id"])] = int(item["steamAppId"])
    return locked


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--max", type=int, default=0, help="Max titles to resolve (0=all missing)")
    parser.add_argument("--catalog", type=Path, default=CATALOG)
    args = parser.parse_args()

    if not args.catalog.exists():
        print("resolve-steam-appids: catalog missing")
        return 1

    catalog = json.loads(args.catalog.read_text(encoding="utf-8"))
    locked = load_lock()
    resolved = 0
    attempted = 0
    used_app_ids = {
        int(g["steamAppId"])
        for g in catalog.get("games", [])
        if g.get("steamAppId") and float(g.get("steamMatchConfidence") or 0) >= 0.92
    }

    for game in catalog.get("games", []):
        if game.get("steamAppId") and float(game.get("steamMatchConfidence") or 0) >= 0.92:
            continue
        if game["id"] in locked:
            app_id = locked[game["id"]]
            if app_id not in used_app_ids:
                game["steamAppId"] = app_id
                game["steamMatchConfidence"] = 1.0
                used_app_ids.add(app_id)
                resolved += 1
            continue
        if args.max and attempted >= args.max:
            break
        attempted += 1
        app_id, score = search_steam(game["title"])
        time.sleep(0.4)
        if app_id and score >= 0.92 and app_id not in used_app_ids:
            game["steamAppId"] = app_id
            game["steamMatchConfidence"] = round(score, 3)
            game["purchaseLinks"] = {"steam": f"https://store.steampowered.com/app/{app_id}/"}
            used_app_ids.add(app_id)
            resolved += 1

    args.catalog.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"resolve-steam-appids: resolved {resolved} titles ({attempted} attempts)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
