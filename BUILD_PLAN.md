# Build Plan

> Active board only. Archives: [COMPLETED_TASKS.md](COMPLETED_TASKS.md) · Evidence: [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md)

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform |
| `AUTO` | CI / scripts / bots |

**Status key:** ✅ done · ⬜ open

Filter open: `grep '^- ⬜' BUILD_PLAN.md`

---

## Status (2026-06-15)

- ✅ **v1.1.0 shipped** — [PR #2](https://github.com/edwardlthompson/3d-game-optimizer/pull/2) merged; [Product Release](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.1.0) run 27548631008
- ✅ **Remote CI** — 168/168 tests on `main` (run 27548154375)
- ✅ **Local gates** — `dotnet test`, `run-post-sprint-validation.ps1`

---

## Active follow-ups

### Distribution

- ⬜ [HUMAN] WinGet **1.0.1** — [microsoft/winget-pkgs#387878](https://github.com/microsoft/winget-pkgs/pull/387878) (CLA queued)
- ⬜ [HUMAN] WinGet **1.1.0** — [microsoft/winget-pkgs#388074](https://github.com/microsoft/winget-pkgs/pull/388074) (CLA if required)
- ⬜ [HUMAN] EV Authenticode cert — optional; sideload via `scripts/codesign-common.ps1`

### Release ops (optional)

- ⬜ [HUMAN] `bash scripts/setup-release-credentials.sh owner/repo`
- ⬜ [HUMAN] `bash scripts/check-release-credentials.sh owner/repo`
- ⬜ [HUMAN] GitHub Actions **Release Credentials Setup** workflow_dispatch
- ⬜ [HUMAN] Org admin for GHAS — only if API returns 403 on security settings

### Hardware & manual QA

- ⬜ [HUMAN] Cover art on user hardware — Sprint 50 build; `SLO_COVER_ART_DEBUG=1` → `%LocalAppData%\3d-game-optimizer\logs\debug-2ca1ae.log` if tiles blank
- ⬜ [HUMAN] ASV15 EDID capture — confirm `5986:PROD` on SpatialLabs View / View Pro 15.6"
- ⬜ [HUMAN] SpatialLabs 15" laptop EDID — tighten catalog wildcards after panel capture
- ⬜ [HUMAN] ADR-0002 PCVR manual QA — [docs/HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md)

---

## Release sign-off (open items)

| Gate | Status | Notes |
|------|--------|-------|
| Release credentials | ⬜ | `check-release-credentials.sh` |
| Template `release.yml` | — | Skipped for `SpatialLabsOptimizer-v*` tags; product uses `product-release.yml` |
| WinGet public listing | ⬜ | Blocked on winget-pkgs PR merge |

All other v1.1.0 gates (CI, QA matrix, legal, post-sprint, product zip/MSI/MSIX) are ✅ — see [COMPLETED_TASKS.md](COMPLETED_TASKS.md) Sprint 39 ship gate.

---

## Reference

| Topic | Location |
|-------|----------|
| Ship automation (done) | `scripts/ship-sprint39-gate.ps1` |
| Local release | [docs/LOCAL_RELEASE.md](docs/LOCAL_RELEASE.md) |
| Completed sprints 28–52 | [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
| Ongoing CI (AUTO) | `ci.yml` on PR; scheduled `health-check.yml`, `scorecard.yml`, `license-audit.yml`, `quarterly-maintenance.yml` |
