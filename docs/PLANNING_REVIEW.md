# Planning Review

> Living evidence table for release and automation decisions.

## Parity status (2026-06-17)

| Track | State | Notes |
|-------|-------|-------|
| CI / GitHub Actions | **Green** | CI, Pages, CodeQL, Security Scan, worker lint all passing on `main` |
| Dependabot | **Clear** | 0 open alerts (esbuild/ws/js-yaml resolved) |
| Product | **Shipped** | [SpatialLabsOptimizer-v1.3.0](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.3.0) — zip + MSI |
| Distribution | GitHub Releases only | In-app updates via `UpdateService` |
| Local release | Framework-dependent publish | [docs/LOCAL_RELEASE.md](LOCAL_RELEASE.md) |

## Release tracks

| Track | Version | Tag | Workflow |
|-------|---------|-----|----------|
| Template | 0.7.1+ | `v*` | `release.yml` |
| Product | 1.3.0 | `SpatialLabsOptimizer-v*` | `product-release.yml` |

## Test count

**217** C# tests + catalog/worker Vitest. Run `dotnet test` and `npm test` in `site/catalog` / `workers/steam-library` for current counts.

## Open follow-ups

| Item | Status |
|------|--------|
| Steam library worker deploy | [HUMAN] — Cloudflare KV + secrets; deploy via workflow_dispatch |
| Cover art hardware confirmation | [HUMAN] — `SLO_COVER_ART_DEBUG=1` |
| Physical GPU / PCVR QA | [HARDWARE_QA_OUT_OF_BAND.md](HARDWARE_QA_OUT_OF_BAND.md) |
| GitHub Pages source | Confirm repo setting = **GitHub Actions** |

## Active board

See [BUILD_PLAN.md](../BUILD_PLAN.md) for open items.

## Closed (Sprints 28–52)

See [COMPLETED_TASKS.md](../COMPLETED_TASKS.md).
