#!/usr/bin/env bash
# Validate data/compatibility/seed-v1.json against schema rules and Sprint 28 minimums.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

SEED="data/compatibility/seed-v1.json"
SCHEMA="data/compatibility/schema.json"
MIN_GAMES="${MIN_GAMES:-10}"

if [ ! -f "$SEED" ]; then
  echo "FAIL: missing $SEED"
  exit 1
fi

if [ ! -f "$SCHEMA" ]; then
  echo "FAIL: missing $SCHEMA"
  exit 1
fi

python3 - "$SEED" "$SCHEMA" "$MIN_GAMES" <<'PY'
import json
import re
import sys
from pathlib import Path

seed_path = Path(sys.argv[1])
schema_path = Path(sys.argv[2])
min_games = int(sys.argv[3])

tier_values = {"unsupported", "experimental", "playable", "optimized"}
confidence_values = {"low", "medium", "high"}
vr_values = {"none", "nativeVr", "uevrCompatible"}
id_pattern = re.compile(r"^[a-z0-9-]+$")
version_pattern = re.compile(r"^v[0-9]+$")
date_pattern = re.compile(r"^\d{4}-\d{2}-\d{2}$")

errors: list[str] = []

def fail(msg: str) -> None:
    errors.append(msg)

try:
    seed = json.loads(seed_path.read_text(encoding="utf-8"))
except json.JSONDecodeError as exc:
    fail(f"invalid JSON in {seed_path}: {exc}")
    print("check-compatibility-seed FAILED")
    for err in errors:
        print(f"  {err}")
    sys.exit(1)

if not isinstance(seed, dict):
    fail("seed root must be an object")
    games = []
else:
    version = seed.get("version")
    if not isinstance(version, str) or not version_pattern.match(version):
        fail("version must match ^v[0-9]+$")
    games = seed.get("games")
    if not isinstance(games, list):
        fail("games must be an array")
        games = []

if len(games) < min_games:
    fail(f"games must contain at least {min_games} entries (found {len(games)})")

seen_ids: set[str] = set()
seen_app_ids: set[int] = set()

for index, game in enumerate(games):
    prefix = f"games[{index}]"
    if not isinstance(game, dict):
        fail(f"{prefix} must be an object")
        continue

    extra = set(game.keys()) - {
        "id", "title", "steamAppId", "steamTags", "tiersByVendor",
        "review", "vrCapability", "steamVrLaunchOptions",
    }
    if extra:
        fail(f"{prefix} has unexpected properties: {sorted(extra)}")

    for field in ("id", "title", "steamAppId", "steamTags", "tiersByVendor", "review"):
        if field not in game:
            fail(f"{prefix} missing required field '{field}'")

    game_id = game.get("id")
    if not isinstance(game_id, str) or not id_pattern.match(game_id):
        fail(f"{prefix}.id must match ^[a-z0-9-]+$")
    elif game_id in seen_ids:
        fail(f"duplicate game id: {game_id}")
    else:
        seen_ids.add(game_id)

    title = game.get("title")
    if not isinstance(title, str) or len(title) < 2:
        fail(f"{prefix}.title must be a string with min length 2")

    app_id = game.get("steamAppId")
    if not isinstance(app_id, int) or app_id < 1:
        fail(f"{prefix}.steamAppId must be a positive integer")
    elif app_id in seen_app_ids:
        fail(f"duplicate steamAppId: {app_id}")
    else:
        seen_app_ids.add(app_id)

    tags = game.get("steamTags")
    if not isinstance(tags, list) or len(tags) < 1 or not all(isinstance(t, str) for t in tags):
        fail(f"{prefix}.steamTags must be a non-empty string array")

    tiers = game.get("tiersByVendor")
    if not isinstance(tiers, dict):
        fail(f"{prefix}.tiersByVendor must be an object")
    else:
        for vendor in ("acer", "samsung", "nvidia", "generic"):
            if vendor not in tiers:
                fail(f"{prefix}.tiersByVendor missing '{vendor}'")
            elif tiers[vendor] not in tier_values:
                fail(f"{prefix}.tiersByVendor.{vendor} invalid tier")

    review = game.get("review")
    if not isinstance(review, dict):
        fail(f"{prefix}.review must be an object")
    else:
        summary = review.get("summary")
        if not isinstance(summary, str) or len(summary) < 5:
            fail(f"{prefix}.review.summary must be a string with min length 5")
        confidence = review.get("confidence")
        if confidence not in confidence_values:
            fail(f"{prefix}.review.confidence invalid")
        reviewed_at = review.get("lastReviewedAt")
        if not isinstance(reviewed_at, str) or not date_pattern.match(reviewed_at):
            fail(f"{prefix}.review.lastReviewedAt must be YYYY-MM-DD")

    vr = game.get("vrCapability")
    if vr is not None and vr not in vr_values:
        fail(f"{prefix}.vrCapability invalid")

    launch_opts = game.get("steamVrLaunchOptions")
    if launch_opts is not None and not isinstance(launch_opts, str):
        fail(f"{prefix}.steamVrLaunchOptions must be a string")

# schema file must remain valid JSON for tooling
try:
    json.loads(schema_path.read_text(encoding="utf-8"))
except json.JSONDecodeError as exc:
    fail(f"invalid JSON in {schema_path}: {exc}")

if errors:
    print("check-compatibility-seed FAILED")
    for err in errors:
        print(f"  {err}")
    sys.exit(1)

print(f"check-compatibility-seed passed ({len(games)} games)")
PY
