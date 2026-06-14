# Planning Review

> Living evidence table for release and automation decisions.

## Parity status (2026-06-14)

| Track | State | Notes |
|-------|-------|-------|
| CI-shippable | **Pending push** | Sprints 32–38 local; verify after single PR to `main` |
| Feature-complete (Sprints 32–38) | Local complete | Gaps tracked in Sprints 39–44 — see [BUILD_PLAN.md](../BUILD_PLAN.md) |
| Release credentials | Automated | `setup-release-credentials.sh`, sideload signing, `build-product-local.ps1` |

## Release tracks

| Track | Version | Tag | Workflow |
|-------|---------|-----|----------|
| Template | 0.7.1+ | `v*` | `release.yml` |
| Product | 1.1.0 | `SpatialLabsOptimizer-v*` | `product-release.yml` (pending on `main`) |

## Test count

114 automated tests locally (Release). Regenerate from CI after Sprint 39 push.

## Open follow-ups

| Item | Status |
|------|--------|
| Sprint 39 ship gate (single PR to `main`) | **Pending** [HUMAN] |
| WinGet PR [#387878](https://github.com/microsoft/winget-pkgs/pull/387878) | Submitted — merge pending CLA/validation |
| WinGet v1.1.0 manifest PR | Pending after merge |
| EV Authenticode cert | Optional |
| Real WinUI README screenshots | Sprint 43 |
| Physical GPU / PCVR QA | Optional [HARDWARE_QA_OUT_OF_BAND.md](HARDWARE_QA_OUT_OF_BAND.md) |

## Forward work (Sprints 39–44)

> Active board: [BUILD_PLAN.md](../BUILD_PLAN.md).

| Sprint | Theme | Priority |
|--------|-------|----------|
| 39 | CI truth + release ops (push, legal script, update tests) | **Active** |
| 40 | MVVM and shell UX (palette hotkey, streamer keys) | Planned |
| 41 | Launch depth and honesty (PlayIn3D wiring, multi-monitor) | Planned |
| 42 | v2 productization (Epic/GOG launch, surface v2 features) | Planned |
| 43 | QA gate hardening (P1 offline/Steam, real screenshots) | Planned |
| 44 | Modularization (file size budgets) | Planned |

## Closed (Sprints 28–38)

| Gap | Sprint | Priority |
|-----|--------|----------|
| Sprints 28–31 (data, library, v2, PCVR, Winget submit) | 28–31 | **Closed** |
| Local build zip/MSIX/MSI | 32 | **Closed** |
| Trainer/mod coexistence | 33 | **Closed** |
| Hardware scan, glossary, launch preview | 34 | **Closed** |
| Local game folders, library intelligence | 35 | **Closed** |
| v2 toggle, About updates, apply+restart | 36 | **Closed** |
| Display/PCVR/command palette (partial hotkeys → Sprint 40) | 37 | **Closed** |
| Diagnostics, protocol, readiness score (screenshots → Sprint 43) | 38 | **Closed** |
