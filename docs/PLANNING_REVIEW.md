# Planning Review

> Living evidence table for release and automation decisions.

## Parity status (2026-06-15)

| Track | State | Notes |
|-------|-------|-------|
| CI-shippable | **Pending push** | Sprints 32–50 local; verify after single PR to `main` |
| Feature-complete (Sprints 32–50) | Local complete | Sprint 50 P0/P1 shipped locally; P2 deferred to Sprint 51 |
| Local release | Framework-dependent publish | WinUI launch verified; self-contained deferred |
| Release credentials | Automated | `setup-release-credentials.sh`, sideload signing, `publish-product.ps1` |

## Release tracks

| Track | Version | Tag | Workflow |
|-------|---------|-----|----------|
| Template | 0.7.1+ | `v*` | `release.yml` |
| Product | 1.1.0 | `SpatialLabsOptimizer-v*` | `product-release.yml` (pending on `main`) |

## Test count

**156** automated tests locally (Release). Regenerate from CI after Sprint 39 push.

## Open follow-ups

| Item | Status |
|------|--------|
| Sprint 39 ship gate (single PR to `main`) | **Pending** [HUMAN] |
| WinGet PR [#387878](https://github.com/microsoft/winget-pkgs/pull/387878) | Submitted — merge pending CLA/validation |
| WinGet v1.1.0 manifest PR | Pending after merge |
| EV Authenticode cert | Optional |
| Cover art hardware confirmation | **Pending** [HUMAN] — Sprint 50 ships debug instrumentation |
| Real WinUI README screenshots | Sprint 43 |
| SpatialLabs 15" / ASV15 EDID on physical panel | [HUMAN] |
| Physical GPU / PCVR QA | Optional [HARDWARE_QA_OUT_OF_BAND.md](HARDWARE_QA_OUT_OF_BAND.md) |
| Vendor silent install flags | **Done** — manual-only vendor policy in Sprint 52 (`docs/TOOLCHAIN.md`) |

## Active sprint

| Sprint | Theme | State |
|--------|-------|-------|
| **42** | v2 productization (Epic/GOG metadata, LAN surfacing) | **Active** |
| 41 | Launch depth + PlayIn3D honesty | Shipped locally |
| 51 | Modularization + Sprint 39 AGENT hygiene | Shipped locally |
| 52 | Vendor toolchain docs + platform deferred work | Shipped locally |
| 39 | CI truth + release ops (push, legal script, update tests) | **Blocking** — pending [HUMAN] PR to `main` |

## Forward work

> Active board: [BUILD_PLAN.md](../BUILD_PLAN.md) — checkbox task format restored.

| Sprint | Theme | Priority |
|--------|-------|----------|
| 44 | Modularization (remaining file splits) | Planned — partial debt in Sprint 51 |
| 42–43 | v2 productization, QA gate, real screenshots | Planned |
| 40–41 | MVVM consolidation, launch depth | Planned |

## Closed (Sprints 28–50)

See [COMPLETED_TASKS.md](../COMPLETED_TASKS.md).
