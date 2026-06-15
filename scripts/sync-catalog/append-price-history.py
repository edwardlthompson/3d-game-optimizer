#!/usr/bin/env python3
"""Append daily Steam prices from catalog into price-history-v1.json."""
from __future__ import annotations

import json
import statistics
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "data" / "compatibility" / "catalog-v2.json"
OUTPUT = ROOT / "data" / "compatibility" / "price-history-v1.json"


def median_recent(points: list[dict], days: int = 30) -> float | None:
    recent = [p["priceUsd"] for p in points[-days:] if p.get("priceUsd") is not None]
    if not recent:
        return None
    return float(statistics.median(recent))


def append_point(app: dict, price: float, today: str) -> None:
    points: list[dict] = app.setdefault("points", [])
    if points and points[-1].get("date") == today:
        if points[-1].get("priceUsd") == price:
            return
        points[-1] = {"date": today, "priceUsd": price, "onSale": points[-1].get("onSale", False)}
    else:
        med = median_recent(points)
        on_sale = med is not None and price <= med * 0.9
        points.append({"date": today, "priceUsd": price, "onSale": on_sale})

    prices = [p["priceUsd"] for p in points if p.get("priceUsd") is not None]
    if prices:
        app["lowUsd"] = min(prices)
        app["highUsd"] = max(prices)


def main() -> int:
    if not CATALOG.exists():
        print("append-price-history: catalog missing")
        return 1

    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    history = {"version": "v1", "updatedAt": date.today().isoformat(), "apps": {}}
    if OUTPUT.exists():
        history = json.loads(OUTPUT.read_text(encoding="utf-8"))
        history.setdefault("apps", {})

    today = date.today().isoformat()
    updated = 0
    for game in catalog.get("games", []):
        app_id = game.get("steamAppId")
        price = (game.get("steamStats") or {}).get("priceUsd")
        if not app_id or price is None:
            continue
        key = str(int(app_id))
        app = history["apps"].setdefault(key, {})
        append_point(app, float(price), today)
        updated += 1

    history["updatedAt"] = today
    OUTPUT.write_text(json.dumps(history, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"append-price-history: updated {updated} app entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
