# Planning Review

> Living evidence table for release and automation decisions.

## Parity status (2026-06-15)

| Track | State | Notes |
|-------|-------|-------|
| CI-shippable | **Shipped** | v1.1.0 on `main`; CI 168/168 (run 27548154375) |
| Feature-complete (Sprints 32–52) | Shipped | [SpatialLabsOptimizer-v1.1.0](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.1.0) |
| Local release | Framework-dependent publish | [docs/LOCAL_RELEASE.md](LOCAL_RELEASE.md) |
| Release credentials | Automated | `setup-release-credentials.sh`, sideload signing, `publish-product.ps1` |

## Release tracks

| Track | Version | Tag | Workflow |
|-------|---------|-----|----------|
| Template | 0.7.1+ | `v*` | `release.yml` |
| Product | 1.1.0 | `SpatialLabsOptimizer-v*` | `product-release.yml` ✅ run 27548631008 |

## Test count

**168** automated tests (remote CI on `main`). Local runs require `SpatialLabsOptimizer.ElevatedHelper` Release build for install-orchestrator integration test.

## Open follow-ups

| Item | Status |
|------|--------|
| WinGet PR [#387878](https://github.com/microsoft/winget-pkgs/pull/387878) (v1.0.1) | CLA queued |
| WinGet v1.1.0 manifest PR | Manifest at `packaging/winget-product/multifile/1.1.0`; `prepare-winget-submission.ps1 -OpenPr` (fork fix) |
| EV Authenticode cert | Optional [HUMAN] |
| Cover art hardware confirmation | [HUMAN] — `SLO_COVER_ART_DEBUG=1` |
| SpatialLabs 15" / ASV15 EDID on physical panel | [HUMAN] |
| Physical GPU / PCVR QA | [HARDWARE_QA_OUT_OF_BAND.md](HARDWARE_QA_OUT_OF_BAND.md) |

## Active board

See [BUILD_PLAN.md](../BUILD_PLAN.md) for open `[HUMAN]` items only.

## Closed (Sprints 28–52)

See [COMPLETED_TASKS.md](../COMPLETED_TASKS.md).
