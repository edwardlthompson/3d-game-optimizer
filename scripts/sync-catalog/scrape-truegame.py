#!/usr/bin/env python3
"""Scrape Acer SpatialLabs TrueGame supported titles."""
from __future__ import annotations

import re
import sys
from pathlib import Path

from scrape_common import load_lkg, parse_truegame_text, run_playwright_scrape, today_iso, write_source

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "data" / "compatibility" / "sources" / "acer-truegame-v1.json"
URL = "https://spatiallabs.acer.com/truegame/list"
MIN_ENTRIES = 150


def extract_supported_section(text: str) -> str:
    """Keep SUPPORTED GAME TITLES section if present."""
    marker = "SUPPORTED GAME TITLES"
    upper = text.upper()
    idx = upper.find(marker)
    if idx >= 0:
        return text[idx:]
    return text


def main() -> int:
    lkg = load_lkg(OUTPUT, "acer-truegame")
    text = run_playwright_scrape(URL, "truegame")
    if not text:
        print("scrape-truegame: Playwright unavailable or failed — using LKG", file=sys.stderr)
        write_source(OUTPUT, lkg)
        return 1

    text = extract_supported_section(text)
    entries = parse_truegame_text(text)
    if len(entries) < MIN_ENTRIES:
        print(
            f"scrape-truegame: only {len(entries)} entries (min {MIN_ENTRIES}) — keeping LKG",
            file=sys.stderr,
        )
        lkg["syncStatus"] = "degraded"
        write_source(OUTPUT, lkg)
        return 1

    payload = {
        "sourceId": "acer-truegame",
        "syncedAt": today_iso(),
        "syncStatus": "ok",
        "entryCount": len(entries),
        "entries": entries,
    }
    write_source(OUTPUT, payload)
    print(f"scrape-truegame: wrote {len(entries)} entries to {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
