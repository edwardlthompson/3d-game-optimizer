#!/usr/bin/env python3
"""Shared helpers for catalog scrapers."""
from __future__ import annotations

import json
import re
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
SOURCES_DIR = ROOT / "data" / "compatibility" / "sources"

TITLE_TAG_RE = re.compile(
    r"^(?P<title>.+?)\s*\[(?P<label>3D Ultra|3D\+|3D)\]\s*$",
    re.IGNORECASE,
)


def today_iso() -> str:
    return datetime.now(UTC).date().isoformat()


def write_source(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def load_lkg(path: Path, source_id: str) -> dict[str, Any]:
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return {
        "sourceId": source_id,
        "syncedAt": today_iso(),
        "syncStatus": "degraded",
        "entries": [],
    }


def parse_truegame_text(text: str) -> list[dict[str, Any]]:
    """Parse visible TrueGame list text into entries."""
    buckets: dict[str, dict[str, Any]] = {}
    level_rank = {"ultra3d": 0, "native3d": 1}

    def level_for_label(label: str) -> str:
        key = label.strip().lower()
        if key == "3d ultra":
            return "ultra3d"
        return "native3d"

    for raw_line in text.splitlines():
        line = raw_line.strip()
        if not line or line.startswith("http"):
            continue
        match = TITLE_TAG_RE.match(line)
        if not match:
            continue
        title = match.group("title").strip()
        label = match.group("label")
        if label.upper() == "3D":
            label = "3D+"
        level = level_for_label(label)
        key = title.lower()
        flags = {"hot": "hot" in line.lower(), "newRelease": "new" in line.lower()}
        if key not in buckets or level_rank[level] < level_rank[buckets[key]["level"]]:
            entry = {
                "title": title,
                "label": label if label != "3D" else "3D+",
                "level": level,
            }
            if flags["hot"]:
                entry["hot"] = True
            if flags["newRelease"]:
                entry["newRelease"] = True
            buckets[key] = entry
    return sorted(buckets.values(), key=lambda e: e["title"].lower())


def run_playwright_scrape(url: str, extract_fn: str) -> str | None:
    """Run Playwright page scrape; returns page innerText or None."""
    try:
        from playwright.sync_api import sync_playwright  # noqa: PLC0415
    except ImportError:
        return None

    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        page = browser.new_page(user_agent=(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
            "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        ))
        page.goto(url, wait_until="domcontentloaded", timeout=120_000)
        page.wait_for_timeout(5000)
        if extract_fn == "truegame":
            text = _extract_truegame(page)
        elif extract_fn == "uevr":
            text = _extract_uevr(page)
        else:
            text = page.inner_text("body")
        browser.close()
        return text


def _extract_truegame(page: Any) -> str:
    # Paginate through all pages (TrueGame uses numbered pagination 1-9).
    chunks: list[str] = []
    for page_num in range(1, 10):
        if page_num > 1:
            clicked = page.evaluate(
                """(num) => {
                    const buttons = [...document.querySelectorAll('button, a, li, span')];
                    const target = buttons.find(el => el.textContent.trim() === String(num));
                    if (target) { target.click(); return true; }
                    return false;
                }""",
                page_num,
            )
            if not clicked:
                break
            page.wait_for_timeout(1500)
        chunks.append(page.inner_text("body"))
    return "\n".join(chunks)


def _extract_uevr(page: Any) -> str:
    page.wait_for_timeout(2000)
    return page.inner_text("body")
