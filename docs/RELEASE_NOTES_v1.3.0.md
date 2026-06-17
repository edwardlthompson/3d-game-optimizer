# SpatialLabs Optimizer v1.3.0

**Release date:** 2026-06-17  
**Tag:** `SpatialLabsOptimizer-v1.3.0`

## Highlights

Distribution is simplified to **GitHub Releases only** — portable zip and WiX MSI. In-app updates (About → Check now) continue to pull from GitHub.

### Removed packaging channels

- **MSIX** sideload and Windows Store manifest
- **WinGet** manifest pipeline — no `winget install`; download from [Releases](https://github.com/edwardlthompson/3d-game-optimizer/releases) or use the in-app updater

### Reliability and parity (since v1.2.0)

- Cover refresh uses prefetch pipeline; parallel cover hydration
- Golden rank fixtures for C# / catalog Vitest parity
- Catalog integrity fail-closed; import validation tests
- Steam worker CORS on 429; truncated Steam ID in exchange response only

## Install

| Asset | Use |
|-------|-----|
| `SpatialLabsOptimizer-1.3.0-win-x64.zip` | Portable / manual install |
| `SpatialLabsOptimizer-1.3.0-win-x64.msi` | Per-machine WiX installer |

**Requires:** Windows 11, .NET 8 Desktop Runtime, Windows App Runtime 2.2.

## Upgrade from v1.2.0

Install over the previous version. Library database and preferences are preserved under `%LOCALAPPDATA%\3d-game-optimizer\`.

If you previously sideloaded an MSIX build, uninstall it and install the zip or MSI from this release.

## Known follow-ups

- Hardware QA matrix spot-check on SpatialLabs / Odyssey 3D

See [CHANGELOG.md](../CHANGELOG.md) for the full change list.
