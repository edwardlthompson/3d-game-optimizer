# SpatialLabs Optimizer v1.4.0

**Release date:** 2026-06-17  
**Tag:** `SpatialLabsOptimizer-v1.4.0`

## Highlights

### Catalog Connect Steam

The public [3D Game Catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/) can now **Connect Steam** when a Cloudflare Worker is configured at build time (`VITE_STEAM_SYNC_URL`):

1. Click **Connect Steam** in the catalog toolbar.
2. Sign in with Steam OpenID.
3. Owned games are matched to catalog titles (Steam link confidence ≥ 0.92) and marked **Lib** in your browser.

Sync tokens are delivered in the URL fragment (not sent to the server on navigation), exchanged once via POST, and stored in KV for five minutes only. No Steam API keys are stored in the browser.

Operator setup: [docs/STEAM_CATALOG_SYNC.md](STEAM_CATALOG_SYNC.md) (KV namespace, Cloudflare/Steam secrets, post-deploy smoke).

### Desktop app

This release ships the same **v1.3.0** distribution model (portable zip + WiX MSI) with reliability fixes under the hood:

- Steam Web API client splits and safer JSON parsing
- `IsCatalogTitle` database read fix
- Play-in-3D test isolation for coexistence probes

## Install

| Asset | Use |
|-------|-----|
| `SpatialLabsOptimizer-1.4.0-win-x64.zip` | Portable / manual install |
| `SpatialLabsOptimizer-1.4.0-win-x64.msi` | Per-machine WiX installer |

**Requires:** Windows 11, .NET 8 Desktop Runtime, Windows App Runtime 2.2.

## Upgrade from v1.3.0

Install over the previous version. Library database and preferences are preserved under `%LOCALAPPDATA%\3d-game-optimizer\`.

Catalog **Lib** checkmarks are browser-only (`localStorage`); they do not sync to the desktop app.

## Known follow-ups

- **[HUMAN]** Cloudflare KV id + GitHub secrets for live Connect Steam (see STEAM_CATALOG_SYNC.md)
- Hardware QA matrix spot-check on SpatialLabs / Odyssey 3D

See [CHANGELOG.md](../CHANGELOG.md) for the full change list.
