# Seed Maintenance Guide

## Seed Files

- `data/displays/display-catalog-v1.json`
- `data/compatibility/schema.json` (seed v1)
- `data/compatibility/schema-v2.json` (catalog v2)
- `data/compatibility/seed-v1.json` (legacy curated seed)
- `data/compatibility/catalog-v2.json` (merged multi-source catalog — **app primary when present**)
- `data/compatibility/catalog-v2.lock.json` (locked Steam app IDs for merge)
- `data/compatibility/sources/*.json` (per-source sync payloads)
- `data/presets/preset-manifest-v1.json`
- `data/defaults/optimal-displays-v1.json`
- `data/performance/performance-tiers-v1.json`
- `data/tools/tool-manifest-v1.json`

## Catalog v2 merge

Regenerate the living catalog after editing sources or seed:

```bash
python3 scripts/sync-catalog/scrape-truegame.py
python3 scripts/sync-catalog/scrape-uevr-profiles.py
python3 scripts/sync-catalog/scrape-vrto3d-wiki.py
python3 scripts/sync-catalog/merge-catalog.py
python3 scripts/sync-catalog/resolve-steam-appids.py
python3 scripts/sync-catalog/export-catalog-markdown.py
python3 scripts/check-compatibility-catalog.py 400
```

Source payloads live under `data/compatibility/sources/` (TrueGame ~220, UEVR ~471, VRto3D wiki, Odyssey Hub seed, ReShade curated, NVIDIA 3D Vision).

Markdown export: `docs/compatibility/LENTICULAR_GAMES.md`

Public browser: `site/catalog/` (deployed to `/catalog/` on GitHub Pages).

## Versioning Rules

- Use semantic dataset versions: `v1`, `v1.1`, `v2`.
- Backward-incompatible schema changes require a new schema version file.
- Seed changes should include changelog notes and validation run output.

## Validation Checklist

1. Validate JSON syntax.
2. Run `scripts/check-compatibility-seed.sh` (seed v1).
3. Run `scripts/check-compatibility-catalog.py` (catalog v2).
4. Verify all IDs referenced across files resolve correctly.
5. Confirm no duplicate game IDs or display IDs.
6. Smoke-test recommendation pipeline with seeded examples.
7. Run `python3 scripts/sync-catalog/test_merge_catalog.py`.

## Update Cadence

- Monthly scheduled review for new games/displays/tools.
- Weekly CI catalog sync (when workflow enabled) for TrueGame, UEVR, PCGW 3D Vision.
- Out-of-band patch updates for high-impact compatibility corrections.

## Critique mitigations

- Steam Store enrichment only (not SteamDB); site footer documents gaps.
- Steam app ID auto-link requires confidence ≥ 0.92; use `catalog-v2.lock.json` for overrides.
- Scraper failures retain last-known-good source JSON with `syncStatus: degraded`.
