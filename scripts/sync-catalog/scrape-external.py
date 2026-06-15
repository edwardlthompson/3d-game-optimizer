#!/usr/bin/env python3
"""Playwright scrapers for TrueGame / UEVR with LKG fallback to committed source files."""
from __future__ import annotations

import json
from datetime import UTC, datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / "data" / "compatibility" / "sources"
REGISTRY = SOURCES / "registry-v1.json"

PLAYWRIGHT_SOURCES = {
    "acer-truegame": "acer-truegame-v1.json",
    "uevr-profiles": "uevr-profiles-v1.json",
}


def lkg_payload(source_id: str, filename: str) -> dict:
    path = SOURCES / filename
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))

    return {
        "sourceId": source_id,
        "syncedAt": datetime.now(UTC).date().isoformat(),
        "syncStatus": "degraded",
        "entries": [],
    }


def try_playwright_scrape(source_id: str) -> dict | None:
    try:
        from playwright.sync_api import sync_playwright  # noqa: PLC0415
    except ImportError:
        return None

    # Placeholder: site-specific selectors belong here. Until implemented, return None
    # so CI uses last-known-good committed JSON.
    _ = sync_playwright
    _ = source_id
    return None


def main() -> int:
    registry = json.loads(REGISTRY.read_text(encoding="utf-8")) if REGISTRY.exists() else {"sources": []}
    known = {item["sourceId"] for item in registry.get("sources", [])}

    for source_id, filename in PLAYWRIGHT_SOURCES.items():
        if source_id not in known:
            continue

        scraped = try_playwright_scrape(source_id)
        payload = scraped if scraped is not None else lkg_payload(source_id, filename)
        payload["syncStatus"] = "ok" if scraped is not None else payload.get("syncStatus", "degraded")
        payload["syncedAt"] = datetime.now(UTC).date().isoformat()

        out = SOURCES / filename
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        status = payload.get("syncStatus", "unknown")
        count = len(payload.get("entries", []))
        print(f"scrape-external: {source_id} -> {count} entries ({status})")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
