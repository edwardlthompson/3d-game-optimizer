# SpatialLabs Optimizer v1.2.0

**Release date:** 2026-06-16  
**Tag:** `SpatialLabsOptimizer-v1.2.0`

## Highlights

This release focuses on **library discovery UX** and **parity with the public [3D Game Catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/)** ranking model.

### Game Rank sorting

Sort your installed 3D library by **Game Rank** — the same composite score used on the catalog site:

- **72%** weighted Steam score (review %, review count, current players)
- **28%** best 3D path score (TrueGame Ultra, UEVR Works Perfectly, etc.)

Ties break on higher 3D Rank, then compatibility tier.

### Min 3D quality filter

Filter titles by minimum 3D path tier: Any · Experimental (26+) · Playable (42+) · Optimized (58+) · Native (72+) · Ultra (88+).

### Library UI polish

- Condensed toolbar — more room for the game grid
- Thumbnail click opens a **detail popup** (metadata, Play in 3D/VR, pin, favorite)
- **Recent launches** panel is resizable (drag the grip between grid and footer)
- Cover art letterboxed (`Uniform`) — no cropping
- Remembers last navigation screen on startup

### Reliability

- Fixed cover art prefetch (`PrivacyGuardHttpHandler` inner handler)
- Skip Steam CDN for Epic/GOG/local hashed app IDs
- Sync disk cover cache paths into `library.db`
- Auto-download presets when games are discovered during indexing

## Install

| Asset | Use |
|-------|-----|
| `SpatialLabsOptimizer-1.2.0-win-x64.zip` | Portable / manual install |
| `SpatialLabsOptimizer-1.2.0-win-x64.msi` | Per-machine WiX installer |
| `SpatialLabsOptimizer-1.2.0-win-x64.msix` | Sideload MSIX (when included) |

**Requires:** Windows 11, .NET 8 Desktop Runtime, Windows App Runtime 2.2.

## Upgrade from v1.1.0

Install over the previous version. Library database and preferences are preserved under `%LOCALAPPDATA%\3d-game-optimizer\`.

## Known follow-ups

- Hardware QA matrix spot-check on SpatialLabs / Odyssey 3D

See [CHANGELOG.md](../CHANGELOG.md) for the full change list.
