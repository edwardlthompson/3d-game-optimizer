#!/usr/bin/env bash
# Validate data/compatibility/catalog-v2.json against schema-v2 rules.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

CATALOG="${CATALOG:-data/compatibility/catalog-v2.json}"
SCHEMA="${SCHEMA:-data/compatibility/schema-v2.json}"
MIN_GAMES="${MIN_GAMES:-10}"

if [ ! -f "$CATALOG" ]; then
  echo "FAIL: missing $CATALOG (run: python3 scripts/sync-catalog/merge-catalog.py)"
  exit 1
fi

if [ ! -f "$SCHEMA" ]; then
  echo "FAIL: missing $SCHEMA"
  exit 1
fi

python3 - "$CATALOG" "$SCHEMA" "$MIN_GAMES" <<'PY'
import json
import re
import sys
from pathlib import Path

catalog_path = Path(sys.argv[1])
schema_path = Path(sys.argv[2])
min_games = int(sys.argv[3])

level_values = {
    "ultra3d", "native3d", "optimized3d", "playable3d", "experimental3d", "unsupported2d"
}
tier_values = {"unsupported", "experimental", "playable", "optimized"}
vr_values = {"none", "nativeVr", "uevrCompatible"}
id_pattern = re.compile(r"^[a-z0-9-]+$")

errors: list[str] = []


def fail(msg: str) -> None:
    errors.append(msg)


try:
    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
except json.JSONDecodeError as exc:
    fail(f"invalid JSON in {catalog_path}: {exc}")
    print("check-compatibility-catalog FAILED")
    for err in errors:
        print(f"  {err}")
    sys.exit(1)

if catalog.get("version") != "v2":
    fail("version must be v2")

meta = catalog.get("meta")
if not isinstance(meta, dict):
    fail("meta must be an object")
elif meta.get("syncStatus") not in {"ok", "degraded", "partial"}:
    fail("meta.syncStatus invalid")

games = catalog.get("games")
if not isinstance(games, list):
    fail("games must be an array")
    games = []

if len(games) < min_games:
    fail(f"games must contain at least {min_games} entries (found {len(games)})")

seen_ids: set[str] = set()
seen_app_ids: set[int] = set()
nvidia_count = 0

for index, game in enumerate(games):
    prefix = f"games[{index}]"
    if not isinstance(game, dict):
        fail(f"{prefix} must be an object")
        continue

    for field in ("id", "title", "sources", "bestLevel", "platforms", "hardwareRequirements", "tiersByVendor"):
        if field not in game:
            fail(f"{prefix} missing required field '{field}'")

    game_id = game.get("id")
    if not isinstance(game_id, str) or not id_pattern.match(game_id):
        fail(f"{prefix}.id must match ^[a-z0-9-]+$")
    elif game_id in seen_ids:
        fail(f"duplicate game id: {game_id}")
    else:
        seen_ids.add(game_id)

    if game.get("bestLevel") not in level_values:
        fail(f"{prefix}.bestLevel invalid")

    app_id = game.get("steamAppId")
    if app_id is not None:
        if not isinstance(app_id, int) or app_id < 1:
            fail(f"{prefix}.steamAppId must be a positive integer")
        elif app_id in seen_app_ids:
            fail(f"duplicate steamAppId: {app_id}")
        else:
            seen_app_ids.add(app_id)

    sources = game.get("sources")
    if not isinstance(sources, list) or len(sources) < 1:
        fail(f"{prefix}.sources must be a non-empty array")
    else:
        for s in sources:
            if s.get("level") not in level_values:
                fail(f"{prefix}.sources level invalid")
        if any(s.get("sourceId") == "nvidia-3d-vision" for s in sources):
            nvidia_count += 1

    hw = game.get("hardwareRequirements")
    if not isinstance(hw, dict):
        fail(f"{prefix}.hardwareRequirements must be an object")
    elif "displays" not in hw or "exclusiveTo" not in hw:
        fail(f"{prefix}.hardwareRequirements missing displays/exclusiveTo")

    tiers = game.get("tiersByVendor")
    if not isinstance(tiers, dict):
        fail(f"{prefix}.tiersByVendor must be an object")
    else:
        for vendor in ("acer", "samsung", "nvidia", "generic"):
            if tiers.get(vendor) not in tier_values:
                fail(f"{prefix}.tiersByVendor.{vendor} invalid tier")

    vr = game.get("vrCapability")
    if vr is not None and vr not in vr_values:
        fail(f"{prefix}.vrCapability invalid")

try:
    json.loads(schema_path.read_text(encoding="utf-8"))
except json.JSONDecodeError as exc:
    fail(f"invalid JSON in {schema_path}: {exc}")

if nvidia_count < 5:
    fail(f"expected at least 5 nvidia-3d-vision sourced games (found {nvidia_count})")

if errors:
    print("check-compatibility-catalog FAILED")
    for err in errors:
        print(f"  {err}")
    sys.exit(1)

print(f"check-compatibility-catalog passed ({len(games)} games, {nvidia_count} with 3D Vision)")
PY
