#!/usr/bin/env python3
"""Fetch VRto3D wiki Compatibility List (SpatialLabs section)."""
from __future__ import annotations

import json
import re
import sys
import urllib.request
from pathlib import Path

from scrape_common import load_lkg, today_iso, write_source

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "data" / "compatibility" / "sources" / "vrto3d-wiki-v1.json"
WIKI_RAW = (
    "https://raw.githubusercontent.com/wiki/oneup03/VRto3D/Compatibility-List.md"
)


def fetch_wiki() -> str:
    req = urllib.request.Request(WIKI_RAW, headers={"User-Agent": "3d-game-optimizer-catalog/1.0"})
    with urllib.request.urlopen(req, timeout=60) as response:
        return response.read().decode("utf-8", "replace")


def strip_html(text: str) -> str:
    return re.sub(r"<[^>]+>", "", text).strip()


def clean_title(title: str) -> str:
    return strip_html(title).strip() or title.strip()


def parse_wiki_markdown(text: str) -> list[dict]:
    entries: dict[str, dict] = {}
    in_spatiallabs = False
    for line in text.splitlines():
        if re.search(r"spatial\s*labs|sr\s*display", line, re.I):
            in_spatiallabs = True
            continue
        if in_spatiallabs and line.startswith("#"):
            break
        if not in_spatiallabs:
            continue
        # markdown table or list
        if "|" in line:
            cells = [c.strip() for c in line.split("|") if c.strip()]
            if len(cells) >= 1 and not cells[0].startswith("-"):
                title = clean_title(cells[0])
                if title.lower() in ("game", "title", "name") or len(title) < 2:
                    continue
                label = cells[1] if len(cells) > 1 else "Compatible"
                entries[title.lower()] = {
                    "title": title,
                    "label": label,
                    "level": "playable3d",
                }
        elif line.strip().startswith(("-", "*")):
            title = clean_title(line.strip().lstrip("-*").strip())
            if len(title) >= 2:
                entries[title.lower()] = {
                    "title": title,
                    "label": "VRto3D wiki",
                    "level": "playable3d",
                }
    # Fallback: any game-like table rows in full doc
    if len(entries) < 5:
        for line in text.splitlines():
            if "|" not in line:
                continue
            cells = [c.strip() for c in line.split("|") if c.strip()]
            if len(cells) >= 1 and cells[0].lower() not in ("game", "title", "name", "---"):
                title = clean_title(cells[0])
                if len(title) >= 3 and not title.startswith("-"):
                    entries.setdefault(
                        title.lower(),
                        {"title": title, "label": "VRto3D wiki", "level": "playable3d"},
                    )
    return sorted(entries.values(), key=lambda e: e["title"].lower())


def main() -> int:
    lkg = load_lkg(OUTPUT, "vrto3d-wiki")
    try:
        wiki = fetch_wiki()
    except Exception as exc:
        print(f"scrape-vrto3d-wiki: fetch failed: {exc}", file=sys.stderr)
        write_source(OUTPUT, lkg)
        return 1

    entries = parse_wiki_markdown(wiki)
    if len(entries) < 3:
        print(f"scrape-vrto3d-wiki: only {len(entries)} entries — keeping LKG", file=sys.stderr)
        lkg["syncStatus"] = "degraded"
        write_source(OUTPUT, lkg)
        return 1

    payload = {
        "sourceId": "vrto3d-wiki",
        "syncedAt": today_iso(),
        "syncStatus": "ok",
        "entryCount": len(entries),
        "entries": entries,
    }
    write_source(OUTPUT, payload)
    print(f"scrape-vrto3d-wiki: wrote {len(entries)} entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
