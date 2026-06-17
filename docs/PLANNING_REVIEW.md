# Planning Review

> Living evidence table for release and automation decisions.

## Parity status (2026-06-17)

| Track | State | Notes |
|-------|-------|-------|
| CI / GitHub Actions | **Green** | CI, Pages, CodeQL, Security Scan, worker lint all passing on `main` |
| Dependabot | **Clear** | 0 open alerts (esbuild/ws/js-yaml resolved) |
| Product | **Shipped** | [SpatialLabsOptimizer-v1.4.0](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.4.0) — zip + MSI; catalog Connect Steam |
| Distribution | GitHub Releases only | In-app updates via `UpdateService` |
| GitHub Pages | **Confirmed** | Actions source (`build_type: workflow`); catalog live |
| Local release | Framework-dependent publish | [docs/LOCAL_RELEASE.md](LOCAL_RELEASE.md) |

## Release tracks

| Track | Version | Tag | Workflow |
|-------|---------|-----|----------|
| Template | 0.7.1+ | `v*` | `release.yml` |
| Product | 1.4.0 | `SpatialLabsOptimizer-v*` | `product-release.yml` |

## Test count

**223** C# tests + **42** catalog Vitest + **35** worker Vitest (run `dotnet test`, `npm test` in `site/catalog` and `workers/steam-library` for current counts).

## Open follow-ups

| Item | Status |
|------|--------|
| Steam library worker deploy | [AUTO] on `main` when secrets + KV id set; `[HUMAN]` KV + secrets + post-deploy smoke |
| Catalog grid / worker file limits | ✅ `catalog-shell.ts` split; worker modules under 150L |
| GPU / display / headset QA | [HUMAN] — manual only; run `run-out-of-band-qa.ps1` first |

## Active board

See [BUILD_PLAN.md](../BUILD_PLAN.md) for open items.

## Closed (Sprints 28–52)

See [COMPLETED_TASKS.md](../COMPLETED_TASKS.md).
