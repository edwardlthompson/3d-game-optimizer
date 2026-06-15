#!/usr/bin/env python3
"""Export merged catalog to categorized markdown for review/import."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "data" / "compatibility" / "catalog-v2.json"
OUTPUT = ROOT / "docs" / "compatibility" / "LENTICULAR_GAMES.md"

SECTIONS = [
    ("Native — Acer TrueGame", lambda g: any(s.get("sourceId") == "acer-truegame" for s in g.get("sources", []))),
    ("Native — Samsung Odyssey 3D Hub", lambda g: any(s.get("sourceId") == "samsung-odyssey-hub" for s in g.get("sources", []))),
    ("Community — UEVR / VRto3D", lambda g: any(s.get("sourceId") in ("uevr-profiles", "vrto3d-wiki") for s in g.get("sources", []))),
    ("Depth — ReShade / srReshade", lambda g: any(s.get("sourceId") == "reshade-depth-shaders" for s in g.get("sources", []))),
    ("Legacy — NVIDIA 3D Vision", lambda g: any(s.get("sourceId") == "nvidia-3d-vision" for s in g.get("sources", []))),
]


def steam_link(game: dict) -> str:
    links = game.get("purchaseLinks") or {}
    if links.get("steam"):
        return links["steam"]
    app_id = game.get("steamAppId")
    if app_id and float(game.get("steamMatchConfidence") or 0) >= 0.92:
        return f"https://store.steampowered.com/app/{app_id}/"
    return ""


def row(game: dict) -> str:
    best = game.get("bestExperience", {})
    link = steam_link(game)
    link_cell = f"[Steam]({link})" if link else "—"
    platforms = ", ".join(game.get("platforms", []))
    return f"| {game['title']} | {best.get('label', game.get('bestLevel', ''))} | {platforms} | {link_cell} |"


def main() -> int:
    if not CATALOG.exists():
        print("export-catalog-markdown: catalog missing")
        return 1

    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    games = catalog.get("games", [])
    meta = catalog.get("meta", {})
    lines = [
        "# Lenticular 3D Game Catalog",
        "",
        f"> Auto-generated from `catalog-v2.json` — {meta.get('gameCount', len(games))} titles · merged {meta.get('mergedAt', '')}",
        "",
        "## Best overall (by 3D tier)",
        "",
        "| Title | Best experience | Platforms | Buy |",
        "| --- | --- | --- | --- |",
    ]
    for game in sorted(games, key=lambda g: (g.get("bestLevel", ""), g["title"].lower())):
        lines.append(row(game))

    for title, predicate in SECTIONS:
        section_games = [g for g in games if predicate(g)]
        if not section_games:
            continue
        lines.extend(["", f"## {title}", "", "| Title | Best experience | Platforms | Buy |", "| --- | --- | --- | --- |"])
        for game in sorted(section_games, key=lambda g: g["title"].lower()):
            lines.append(row(game))

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"export-catalog-markdown: wrote {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
