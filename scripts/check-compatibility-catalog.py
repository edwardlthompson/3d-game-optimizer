#!/usr/bin/env python3
"""Validate catalog-v2.json (Windows-friendly entry point)."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCRIPT = ROOT / "scripts" / "check-compatibility-catalog.sh"

if __name__ == "__main__":
    # Inline validation — same logic as shell script, no bash required on Windows.
    import json
    import re

    catalog_path = ROOT / "data" / "compatibility" / "catalog-v2.json"
    schema_path = ROOT / "data" / "compatibility" / "schema-v2.json"
    min_games = int(sys.argv[1]) if len(sys.argv) > 1 else 400

    level_values = {
        "ultra3d", "native3d", "optimized3d", "playable3d", "experimental3d", "unsupported2d"
    }
    tier_values = {"unsupported", "experimental", "playable", "optimized"}
    id_pattern = re.compile(r"^[a-z0-9-]+$")
    errors: list[str] = []

    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    if catalog.get("version") != "v2":
        errors.append("version must be v2")
    games = catalog.get("games", [])
    if len(games) < min_games:
        errors.append(f"games must contain at least {min_games} entries (found {len(games)})")

    seen_ids: set[str] = set()
    seen_app_ids: set[int] = set()
    nvidia_count = 0

    for index, game in enumerate(games):
        prefix = f"games[{index}]"
        game_id = game.get("id", "")
        if not id_pattern.match(game_id):
            errors.append(f"{prefix}.id invalid")
        elif game_id in seen_ids:
            errors.append(f"duplicate game id: {game_id}")
        else:
            seen_ids.add(game_id)

        app_id = game.get("steamAppId")
        if app_id is not None:
            if app_id in seen_app_ids:
                errors.append(f"duplicate steamAppId: {app_id}")
            seen_app_ids.add(app_id)

        if game.get("bestLevel") not in level_values:
            errors.append(f"{prefix}.bestLevel invalid")

        sources = game.get("sources", [])
        if not sources:
            errors.append(f"{prefix}.sources empty")
        if any(s.get("sourceId") == "nvidia-3d-vision" for s in sources):
            nvidia_count += 1

        tiers = game.get("tiersByVendor", {})
        for vendor in ("acer", "samsung", "nvidia", "generic"):
            if tiers.get(vendor) not in tier_values:
                errors.append(f"{prefix}.tiersByVendor.{vendor} invalid")

    if nvidia_count < 5:
        errors.append(f"expected at least 5 nvidia-3d-vision sourced games (found {nvidia_count})")

    json.loads(schema_path.read_text(encoding="utf-8"))

    if errors:
        print("check-compatibility-catalog FAILED")
        for err in errors:
            print(f"  {err}")
        raise SystemExit(1)

    print(f"check-compatibility-catalog passed ({len(games)} games, {nvidia_count} with 3D Vision)")
