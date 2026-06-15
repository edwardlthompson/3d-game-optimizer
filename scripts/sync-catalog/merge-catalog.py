#!/usr/bin/env python3
"""Merge multi-source 3D catalog entries into data/compatibility/catalog-v2.json."""
from __future__ import annotations

import argparse
import json
import re
import unicodedata
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
SOURCES_DIR = ROOT / "data" / "compatibility" / "sources"
SEED_PATH = ROOT / "data" / "compatibility" / "seed-v1.json"
LOCK_PATH = ROOT / "data" / "compatibility" / "catalog-v2.lock.json"
OUTPUT_PATH = ROOT / "data" / "compatibility" / "catalog-v2.json"

LEVEL_ORDER = [
    "ultra3d",
    "native3d",
    "optimized3d",
    "playable3d",
    "experimental3d",
    "unsupported2d",
]

LEVEL_TO_TIER = {
    "ultra3d": "optimized",
    "native3d": "optimized",
    "optimized3d": "playable",
    "playable3d": "playable",
    "experimental3d": "experimental",
    "unsupported2d": "unsupported",
}

SOURCE_VENDOR = {
    "acer-truegame": "acer",
    "samsung-odyssey-hub": "samsung",
    "nvidia-3d-vision": "nvidia",
    "uevr-profiles": "generic",
    "asus-spatial-vision": "acer",
}

SOURCE_PLATFORM = {
    "acer-truegame": "truegame",
    "uevr-profiles": "uevr",
    "nvidia-3d-vision": "nvidia-3d-vision",
    "samsung-odyssey-hub": "odyssey-hub",
    "asus-spatial-vision": "asus-spatial-vision",
    "manual-curated": "manual",
}

SOURCE_DISPLAYS: dict[str, list[str]] = {
    "acer-truegame": ["acer-psv27-2", "acer-asv15-1", "acer-spatiallabs-15"],
    "samsung-odyssey-hub": ["samsung-g90xf"],
    "nvidia-3d-vision": ["nvidia-3d-vision-generic"],
    "uevr-profiles": ["generic-manual"],
    "asus-spatial-vision": ["generic-manual"],
    "manual-curated": ["generic-manual"],
}

UEVR_LABEL_TO_LEVEL = {
    "works perfectly": "ultra3d",
    "works well": "optimized3d",
    "works ok": "playable3d",
    "works poorly": "experimental3d",
}

TRUEGAME_LABEL_TO_LEVEL = {
    "3d ultra": "ultra3d",
    "3d": "native3d",
}


def slugify(title: str) -> str:
    normalized = unicodedata.normalize("NFKD", title)
    ascii_text = normalized.encode("ascii", "ignore").decode("ascii").lower()
    slug = re.sub(r"[^a-z0-9]+", "-", ascii_text).strip("-")
    return slug or "unknown"


def best_level(levels: list[str]) -> str:
    if not levels:
        return "unsupported2d"
    return min(levels, key=lambda value: LEVEL_ORDER.index(value))


def level_from_label(source_id: str, label: str) -> str:
    key = label.strip().lower()
    if source_id == "uevr-profiles":
        return UEVR_LABEL_TO_LEVEL.get(key, "playable3d")
    if source_id == "acer-truegame":
        return TRUEGAME_LABEL_TO_LEVEL.get(key, "native3d")
    return "native3d"


def tier_rank(tier: str) -> int:
    order = ["unsupported", "experimental", "playable", "optimized"]
    return order.index(tier) if tier in order else 0


def merge_tiers(existing: dict[str, str], incoming: dict[str, str]) -> dict[str, str]:
    merged = dict(existing)
    for vendor, tier in incoming.items():
        if vendor not in merged or tier_rank(tier) > tier_rank(merged[vendor]):
            merged[vendor] = tier
    return merged


def tiers_from_sources(sources: list[dict[str, Any]]) -> dict[str, str]:
    tiers = {vendor: "unsupported" for vendor in ("acer", "samsung", "nvidia", "generic")}
    for source in sources:
        vendor = SOURCE_VENDOR.get(source["sourceId"])
        if vendor is None:
            continue
        tier = LEVEL_TO_TIER.get(source["level"], "experimental")
        if tier_rank(tier) > tier_rank(tiers[vendor]):
            tiers[vendor] = tier
    if any(s["sourceId"] == "uevr-profiles" for s in sources):
        uevr_tier = LEVEL_TO_TIER.get(
            next(s["level"] for s in sources if s["sourceId"] == "uevr-profiles"),
            "playable",
        )
        if tier_rank(uevr_tier) > tier_rank(tiers["generic"]):
            tiers["generic"] = uevr_tier
    return tiers


def compute_hardware(sources: list[dict[str, Any]]) -> dict[str, Any]:
    displays: set[str] = set()
    has_uevr = any(s["sourceId"] == "uevr-profiles" for s in sources)
    has_generic_path = has_uevr or any(
        s["sourceId"] in ("manual-curated", "reshade-depth-shaders") for s in sources
    )

    for source in sources:
        displays.update(SOURCE_DISPLAYS.get(source["sourceId"], []))

    if has_uevr and "generic-manual" not in displays:
        displays.add("generic-manual")

    display_list = sorted(displays)
    exclusive: list[str] = []
    needs_review = False

    if len(display_list) == 1 and not has_generic_path:
        exclusive = display_list.copy()
        if display_list[0] == "nvidia-3d-vision-generic":
            needs_review = False
        else:
            needs_review = True

    notes_parts: list[str] = []
    if any(s["sourceId"] == "nvidia-3d-vision" for s in sources):
        notes_parts.append(
            "Legacy NVIDIA 3D Vision stack; stereoscopic driver deprecated on modern Windows."
        )
    if any(s["sourceId"] == "acer-truegame" for s in sources):
        notes_parts.append("Acer SpatialLabs TrueGame path.")
    if has_uevr:
        notes_parts.append("UEVR community profile may apply.")

    hardware: dict[str, Any] = {
        "displays": display_list,
        "exclusiveTo": exclusive,
        "notes": " ".join(notes_parts).strip(),
    }

    if any(s["sourceId"] == "nvidia-3d-vision" for s in sources):
        hardware["gpu"] = ["nvidia-geforce"]
        hardware["accessories"] = ["3d-vision-glasses-or-3dtv-play", "120hz-monitor"]

    if needs_review:
        hardware["needsHumanReview"] = True

    return hardware


def load_lock() -> dict[str, int]:
    if not LOCK_PATH.exists():
        return {}
    data = json.loads(LOCK_PATH.read_text(encoding="utf-8"))
    locked: dict[str, int] = {}
    for item in data.get("lockedAppIds", []):
        if isinstance(item, dict) and "id" in item and "steamAppId" in item:
            locked[str(item["id"])] = int(item["steamAppId"])
    return locked


def load_seed_entries() -> list[dict[str, Any]]:
    if not SEED_PATH.exists():
        return []
    seed = json.loads(SEED_PATH.read_text(encoding="utf-8"))
    today = datetime.now(UTC).date().isoformat()
    entries: list[dict[str, Any]] = []
    for game in seed.get("games", []):
        levels = []
        for tier in game.get("tiersByVendor", {}).values():
            if tier == "optimized":
                levels.append("native3d")
            elif tier == "playable":
                levels.append("playable3d")
            elif tier == "experimental":
                levels.append("experimental3d")
        level = best_level(levels) if levels else "unsupported2d"
        entry: dict[str, Any] = {
            "id": game["id"],
            "title": game["title"],
            "steamAppId": game["steamAppId"],
            "steamMatchConfidence": 1.0,
            "steamTags": game.get("steamTags", []),
            "reviewSummary": game.get("review", {}).get("summary", ""),
            "tiersByVendorSeed": game.get("tiersByVendor", {}),
            "sources": [
                {
                    "sourceId": "manual-curated",
                    "level": level,
                    "label": "Curated seed",
                    "syncedAt": today,
                }
            ],
        }
        if game.get("vrCapability"):
            entry["vrCapability"] = game["vrCapability"]
        if game.get("steamVrLaunchOptions"):
            entry["steamVrLaunchOptions"] = game["steamVrLaunchOptions"]
        entries.append(entry)
    return entries


def load_source_files() -> tuple[list[dict[str, Any]], dict[str, str]]:
    rows: list[dict[str, Any]] = []
    sync_status: dict[str, str] = {}

    for path in sorted(SOURCES_DIR.glob("*-v1.json")):
        if path.name == "registry-v1.json":
            continue
        payload = json.loads(path.read_text(encoding="utf-8"))
        source_id = payload.get("sourceId", path.stem.rsplit("-v1", 1)[0])
        sync_status[source_id] = payload.get("syncStatus", "ok")
        synced_at = payload.get("syncedAt", datetime.now(UTC).date().isoformat())

        for item in payload.get("entries", []):
            label = item.get("label", "")
            level = item.get("level") or level_from_label(source_id, label)
            source_entry = {
                "sourceId": source_id,
                "level": level,
                "label": label or level,
                "syncedAt": synced_at,
            }
            for optional in (
                "profileUrl",
                "pcgwUrl",
                "supportType",
                "supportStatus",
                "communityRating",
            ):
                if optional in item:
                    source_entry[optional] = item[optional]

            row = {
                "title": item["title"],
                "steamAppId": item.get("steamAppId"),
                "steamMatchConfidence": item.get("steamMatchConfidence", 0.95 if item.get("steamAppId") else 0),
                "sources": [source_entry],
            }
            if item.get("steamTags"):
                row["steamTags"] = item["steamTags"]
            rows.append(row)

    return rows, sync_status


def merge_rows(all_rows: list[dict[str, Any]], locked: dict[str, int]) -> list[dict[str, Any]]:
    buckets: dict[str, dict[str, Any]] = {}

    for row in all_rows:
        app_id = row.get("steamAppId")
        confidence = float(row.get("steamMatchConfidence") or 0)
        key = f"app:{app_id}" if app_id and confidence >= 0.92 else f"title:{row['title'].strip().lower()}"

        if key not in buckets:
            game_id = row.get("id") or slugify(row["title"])
            buckets[key] = {
                "id": game_id,
                "title": row["title"],
                "steamAppId": app_id,
                "steamMatchConfidence": confidence if app_id else None,
                "sources": [],
                "steamTags": row.get("steamTags", []),
                "reviewSummary": row.get("reviewSummary", ""),
                "tiersByVendorSeed": row.get("tiersByVendorSeed"),
                "vrCapability": row.get("vrCapability"),
                "steamVrLaunchOptions": row.get("steamVrLaunchOptions"),
            }
        else:
            bucket = buckets[key]
            if app_id and (not bucket.get("steamAppId") or confidence > float(bucket.get("steamMatchConfidence") or 0)):
                bucket["steamAppId"] = app_id
                bucket["steamMatchConfidence"] = confidence
            if row.get("steamTags") and not bucket.get("steamTags"):
                bucket["steamTags"] = row["steamTags"]
            if row.get("reviewSummary") and not bucket.get("reviewSummary"):
                bucket["reviewSummary"] = row["reviewSummary"]
            if row.get("tiersByVendorSeed"):
                bucket["tiersByVendorSeed"] = row["tiersByVendorSeed"]
            for field in ("vrCapability", "steamVrLaunchOptions"):
                if row.get(field) and not bucket.get(field):
                    bucket[field] = row[field]

        existing_source_ids = {s["sourceId"] for s in buckets[key]["sources"]}
        for source in row.get("sources", []):
            if source["sourceId"] in existing_source_ids:
                continue
            buckets[key]["sources"].append(source)
            existing_source_ids.add(source["sourceId"])

    games: list[dict[str, Any]] = []
    for bucket in buckets.values():
        game_id = bucket["id"]
        if game_id in locked:
            bucket["steamAppId"] = locked[game_id]
            bucket["steamMatchConfidence"] = 1.0

        sources = bucket["sources"]
        levels = [s["level"] for s in sources]
        best = best_level(levels)
        platforms = sorted({SOURCE_PLATFORM[s["sourceId"]] for s in sources if s["sourceId"] in SOURCE_PLATFORM})

        tiers = tiers_from_sources(sources)
        if bucket.get("tiersByVendorSeed"):
            tiers = merge_tiers(tiers, bucket["tiersByVendorSeed"])

        truegame = next((s for s in sources if s["sourceId"] == "acer-truegame"), None)

        game: dict[str, Any] = {
            "id": game_id,
            "title": bucket["title"],
            "sources": sources,
            "bestLevel": best,
            "platforms": platforms,
            "hardwareRequirements": compute_hardware(sources),
            "tiersByVendor": tiers,
        }
        if bucket.get("steamAppId"):
            game["steamAppId"] = bucket["steamAppId"]
        if bucket.get("steamMatchConfidence") is not None:
            game["steamMatchConfidence"] = bucket["steamMatchConfidence"]
        if bucket.get("steamTags"):
            game["steamTags"] = bucket["steamTags"]
        if bucket.get("reviewSummary"):
            game["reviewSummary"] = bucket["reviewSummary"]
        if truegame:
            game["trueGameLabel"] = truegame.get("label")
        if bucket.get("vrCapability"):
            game["vrCapability"] = bucket["vrCapability"]
        if bucket.get("steamVrLaunchOptions"):
            game["steamVrLaunchOptions"] = bucket["steamVrLaunchOptions"]
        games.append(game)

    games.sort(key=lambda g: g["title"].lower())
    return games


def build_catalog() -> dict[str, Any]:
    locked = load_lock()
    seed_rows = load_seed_entries()
    source_rows, sync_status = load_source_files()
    degraded = any(status != "ok" for status in sync_status.values())
    games = merge_rows(seed_rows + source_rows, locked)

    return {
        "version": "v2",
        "meta": {
            "mergedAt": datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
            "syncStatus": "degraded" if degraded else "ok",
            "gameCount": len(games),
            "sources": sync_status,
        },
        "games": games,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Merge 3D catalog sources into catalog-v2.json")
    parser.add_argument(
        "--output",
        type=Path,
        default=OUTPUT_PATH,
        help="Output path for catalog-v2.json",
    )
    args = parser.parse_args()

    catalog = build_catalog()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"merge-catalog: wrote {len(catalog['games'])} games to {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
