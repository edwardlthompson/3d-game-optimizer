# Planning Review

> Living evidence table for release and automation decisions.

## Parity status (2026-06-17)

| Track | State | Notes |
|-------|-------|-------|
| CI-shippable | **Shipped** | v1.2.0 on `main`; dotnet test green locally |
| Feature-complete (Sprints 32–52) | Shipped | [SpatialLabsOptimizer-v1.3.0](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.3.0) |
| Local release | Framework-dependent publish | [docs/LOCAL_RELEASE.md](LOCAL_RELEASE.md) |
| Distribution | GitHub Releases only | Zip + MSI; in-app updates via `UpdateService` |

## Release tracks

| Track | Version | Tag | Workflow |
|-------|---------|-----|----------|
| Template | 0.7.1+ | `v*` | `release.yml` |
| Product | 1.3.0 | `SpatialLabsOptimizer-v*` | `product-release.yml` |

## Test count

**190+** automated tests (C# + catalog Vitest + worker Vitest). Run `dotnet test` and `npm test` in `site/catalog` / `workers/steam-library` for current counts.

## Open follow-ups

| Item | Status |
|------|--------|
| Cover art hardware confirmation | [HUMAN] — `SLO_COVER_ART_DEBUG=1` |
| SpatialLabs 15" / ASV15 EDID on physical panel | [HUMAN] |
| Physical GPU / PCVR QA | [HARDWARE_QA_OUT_OF_BAND.md](HARDWARE_QA_OUT_OF_BAND.md) |
| Steam library worker deploy | [HUMAN] — Cloudflare KV + secrets |

## Active board

See [BUILD_PLAN.md](../BUILD_PLAN.md) for open items.

## Closed (Sprints 28–52)

See [COMPLETED_TASKS.md](../COMPLETED_TASKS.md).
