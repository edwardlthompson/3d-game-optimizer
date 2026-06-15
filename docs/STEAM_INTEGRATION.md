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

- Local library manifest paths (`steamapps/appmanifest_*.acf`).
- Steam Web API key + Steam ID64 (Library Settings → Test connection) for owned-games catalog merge.
- App IDs and install states.
- Seed mapping in `data/compatibility/seed-v1.json`.
- Steam Store API for cover art, titles, and review summaries (user-initiated prefetch).

## Library Settings connection flow (Sprint 47)

1. User opens **Library Settings** from the sidebar.
2. Enters Steam ID64 + Web API key → **Test connection** calls `IPlayerService/GetOwnedGames`.
3. API key is stored locally with Windows DPAPI (`DpapiSecretStore`); never uploaded or logged.
4. Successful validation merges owned AppIDs into `library.db` and triggers artwork/metadata prefetch.
5. Epic, GOG, and Ubisoft use local install path validation only (ADR-0004); online catalog slots reserved for future sync.

## Privacy Requirements

- No background scanning outside user-approved library paths.
- Steam Web API calls occur only after explicit Test/Save in Library Settings.
- No transmission of complete local library inventories by default.
- Cache only what is required for compatibility matching.

## Failure Strategy

- If Steam data is unavailable, continue with local seed browsing mode.
- Display partial-data banner instead of hard blocking setup.
