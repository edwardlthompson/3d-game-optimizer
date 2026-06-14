# Steam Integration Plan

## Goals

- Import installed game metadata from Steam libraries.
- Map titles to compatibility seeds and vendor-specific tiers.
- Use Steam tags and optional review sentiment as recommendation signals.

## Integration Boundaries

- Steam integration is an infrastructure adapter, never called directly from views.
- Use case layer requests normalized `GameCatalogItem` objects only.
- Network operations are user-initiated and transparent.

## Data Inputs

- Local library manifest paths.
- App IDs and install states.
- Seed mapping in `data/compatibility/seed-v1.json`.
- Optional web metadata for tags/reviews where API terms allow.

## Privacy Requirements

- No background scanning outside user-approved library paths.
- No transmission of complete local library inventories by default.
- Cache only what is required for compatibility matching.

## Failure Strategy

- If Steam data is unavailable, continue with local seed browsing mode.
- Display partial-data banner instead of hard blocking setup.
