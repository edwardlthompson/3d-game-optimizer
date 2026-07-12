# SpatialLabs Optimizer v1.5.0

**Release date:** 2026-07-12  
**Tag:** `SpatialLabsOptimizer-v1.5.0`

## Highlights

### Desktop trust and discovery

- **Setup wizard** — first-run disclaimer → display → toolchain instead of dumping into Settings
- **Play in 3D confirmation** — preview platform, depth, toolchain, and tier before config mutation
- **Simple mode** hides Advanced / Integrations and dense library filters; **theme** (light / dark / system) applies via `AppThemeService`
- **Readiness score** on Settings; library **filter flyout** + chips; game detail notes, queue, and playlist
- Global **Ctrl+K** Quick Actions; Safe Launch enables and plays; orphan Toolchain Health view removed

### Catalog Steam UX

- Unavailable / loading / Resync states when the sync worker is missing or busy
- Confirm before replace or disconnect; unmatched App ID modal
- Toolbar prefs persistence; TrueGame / UEVR / min 3D Rank filters

### Security and CI hygiene

- `undici >=7.28.0` overrides (catalog worker + examples/web)
- Security Triage label bootstrap; Health Check dispatches CI when Dependabot merges skip push CI
- Catalog large-file allowlist; CodeQL double-escaping fix in `stripHtml`

## Install

| Asset | Use |
|-------|-----|
| `SpatialLabsOptimizer-1.5.0-win-x64.zip` | Portable / manual install |
| `SpatialLabsOptimizer-1.5.0-win-x64.msi` | Per-machine WiX installer |

**Requires:** Windows 11, .NET 8 Desktop Runtime, Windows App Runtime 2.2.

## Upgrade from v1.4.0

Install over the previous version. Library database and preferences are preserved under `%LOCALAPPDATA%\3d-game-optimizer\`.

Catalog **Lib** checkmarks remain browser-only (`localStorage`); they do not sync to the desktop app.

## Known follow-ups

- **[HUMAN]** Cloudflare KV id + GitHub secrets for live Connect Steam (see [STEAM_CATALOG_SYNC.md](STEAM_CATALOG_SYNC.md))
- Hardware QA matrix spot-check on SpatialLabs / Odyssey 3D
- Real README product screenshots

See [CHANGELOG.md](../CHANGELOG.md) for the full change list.
