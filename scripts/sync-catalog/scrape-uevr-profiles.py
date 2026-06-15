#!/usr/bin/env python3
"""Scrape UEVR profiles site for game compatibility ratings."""
from __future__ import annotations

import re
import sys
from pathlib import Path

from scrape_common import load_lkg, run_playwright_scrape, today_iso, write_source

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "data" / "compatibility" / "sources" / "uevr-profiles-v1.json"
URL = "https://uevr-profiles.com/"
MIN_ENTRIES = 200

UEVR_LABEL_TO_LEVEL = {
    "works perfectly": "ultra3d",
    "works well": "optimized3d",
    "works ok": "playable3d",
    "works poorly": "experimental3d",
    "doesn't work": "unsupported2d",
    "does not work": "unsupported2d",
}

UEVR_RATINGS = (
    "Works Perfectly",
    "Works Well",
    "Works OK",
    "Works Poorly",
    "Doesn't Work",
    "Does Not Work",
)


def parse_uevr_text(text: str) -> list[dict]:
    entries: dict[str, dict] = {}
    lines = [ln.strip() for ln in text.splitlines() if ln.strip()]
    skip_prefixes = (
        "please consider",
        "support us",
        "uevr profiles",
        "find your favorites",
        "games and counting",
        "6dof motion",
    )

    i = 0
    while i < len(lines):
        line = lines[i]
        lower = line.lower()
        if any(lower.startswith(p) for p in skip_prefixes):
            i += 1
            continue
        if line in UEVR_RATINGS or line == "6DOF Motion Controls":
            i += 1
            continue

        rating_line = lines[i + 1] if i + 1 < len(lines) else ""
        rating = None
        for label in UEVR_RATINGS:
            if rating_line.startswith(label):
                rating = label
                break
        if rating is None:
            i += 1
            continue

        title = line
        level = UEVR_LABEL_TO_LEVEL.get(rating.lower(), "playable3d")
        motion = "6DOF Motion Controls" in rating_line
        entry = {"title": title, "label": rating, "level": level}
        if motion:
            entry["motionControls"] = True
        entries[title.lower()] = entry
        i += 2

    return sorted(entries.values(), key=lambda e: e["title"].lower())


def main() -> int:
    lkg = load_lkg(OUTPUT, "uevr-profiles")
    text = run_playwright_scrape(URL, "uevr")
    if not text:
        print("scrape-uevr-profiles: Playwright failed — using LKG", file=sys.stderr)
        write_source(OUTPUT, lkg)
        return 1

    entries = parse_uevr_text(text)
    if len(entries) < MIN_ENTRIES:
        try:
            from playwright.sync_api import sync_playwright  # noqa: PLC0415

            with sync_playwright() as playwright:
                browser = playwright.chromium.launch(headless=True)
                page = browser.new_page()
                page.goto(URL, wait_until="domcontentloaded", timeout=120_000)
                page.wait_for_timeout(8000)
                text = page.inner_text("body")
                browser.close()
            entries = parse_uevr_text(text)
        except Exception as exc:
            print(f"scrape-uevr-profiles: retry failed: {exc}", file=sys.stderr)

    if len(entries) < MIN_ENTRIES:
        print(
            f"scrape-uevr-profiles: only {len(entries)} entries (min {MIN_ENTRIES}) — keeping LKG",
            file=sys.stderr,
        )
        lkg["syncStatus"] = "degraded"
        write_source(OUTPUT, lkg)
        return 1

    payload = {
        "sourceId": "uevr-profiles",
        "syncedAt": today_iso(),
        "syncStatus": "ok",
        "entryCount": len(entries),
        "entries": entries,
    }
    write_source(OUTPUT, payload)
    print(f"scrape-uevr-profiles: wrote {len(entries)} entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
