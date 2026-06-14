# Seed Maintenance Guide

## Seed Files

- `data/displays/display-catalog-v1.json`
- `data/compatibility/schema.json`
- `data/compatibility/seed-v1.json`
- `data/presets/preset-manifest-v1.json`
- `data/defaults/optimal-displays-v1.json`
- `data/performance/performance-tiers-v1.json`
- `data/tools/tool-manifest-v1.json`

## Versioning Rules

- Use semantic dataset versions: `v1`, `v1.1`, `v2`.
- Backward-incompatible schema changes require a new schema version file.
- Seed changes should include changelog notes and validation run output.

## Validation Checklist

1. Validate JSON syntax.
2. Validate compatibility entries against schema.
3. Verify all IDs referenced across files resolve correctly.
4. Confirm no duplicate game IDs or display IDs.
5. Smoke-test recommendation pipeline with seeded examples.

## Update Cadence

- Monthly scheduled review for new games/displays/tools.
- Out-of-band patch updates for high-impact compatibility corrections.
