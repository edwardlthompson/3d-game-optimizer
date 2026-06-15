# Build Plan

> Active board only. Archives: [COMPLETED_TASKS.md](COMPLETED_TASKS.md) · Evidence: [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md)

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform |
| `AUTO` | CI / scripts / bots |

**Status key:** ✅ done · 🔄 in progress · ⬜ open

---

## Status (2026-06-15)

- ✅ **Settings toolchain + library performance** — archived to [COMPLETED_TASKS.md](COMPLETED_TASKS.md#settings-toolchain--library-performance-archived-2026-06-15)
- ✅ **Living 3D catalog + GitHub Pages browser** — Phases 1–6 shipped locally; **191/191** tests green
- ⬜ [HUMAN] Confirm GitHub Pages source = **GitHub Actions** ([docs/WEB_PROJECT_LAYOUT.md](docs/WEB_PROJECT_LAYOUT.md))

---

## Active follow-ups

### Distribution

- ⬜ [HUMAN] WinGet **1.0.1** — [microsoft/winget-pkgs#387878](https://github.com/microsoft/winget-pkgs/pull/387878)
- ⬜ [HUMAN] WinGet **1.1.0** — [microsoft/winget-pkgs#388074](https://github.com/microsoft/winget-pkgs/pull/388074)
- ⬜ [HUMAN] EV Authenticode cert — optional
- ⬜ [HUMAN] Optional `AIRTABLE_PAT` secret for MTBS3D community 3D Vision ratings in CI

### Hardware & manual QA

- ⬜ [HUMAN] Cover art on user hardware — `SLO_COVER_ART_DEBUG=1`
- ⬜ [HUMAN] ASV15 EDID capture — confirm `5986:PROD`
- ⬜ [HUMAN] SpatialLabs 15" laptop EDID — tighten catalog wildcards
- ⬜ [HUMAN] ADR-0002 PCVR manual QA — [docs/HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md)

---

## Reference

| Topic | Location |
|-------|----------|
| Catalog maintenance | [docs/SEED_MAINTENANCE.md](docs/SEED_MAINTENANCE.md) |
| Live catalog site | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
| Local release | [docs/LOCAL_RELEASE.md](docs/LOCAL_RELEASE.md) |
| Completed work | [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
