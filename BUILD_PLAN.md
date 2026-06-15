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

- ✅ **Living 3D catalog Phases 1–6** — archived
- ✅ **Lenticular multi-source catalog** — **686 titles**, **638 Steam buy links**, TanStack grid shipped locally
- ⬜ [AUTO] Push → GitHub Pages redeploy
- ⬜ [HUMAN] Confirm GitHub Pages source = **GitHub Actions**

---

## Sequential lane — Lenticular 3D catalog

> Design: `.cursor/plans/full_3d_catalog_grid_38dd0a52.plan.md` · Export: [LENTICULAR_GAMES.md](docs/compatibility/LENTICULAR_GAMES.md)

### Phase 1 — Multi-source scrape ✅

- ✅ [AGENT] TrueGame, UEVR, VRto3D wiki, Odyssey seed, ReShade curated, registry

### Phase 2 — Merge, Steam links, export ✅

- ✅ [AGENT] merge, resolve (638 links), markdown export, CI pipeline

### Phase 3 — Airtable-style public grid ✅

- ✅ [AGENT] TanStack Table per-column filters + pagination (25/50/100/250 rows)

### Phase 4 — Ship

- ✅ [AGENT] Validate 686 games · site `npm run build` passes
- ⬜ [AUTO] Push → GitHub Pages redeploy
- ✅ [AGENT] Steam resolve batch (+230 this session; ~48 remain for lock file)
- ⬜ [HUMAN] Odyssey Hub CSV export from installed app

---

## Active follow-ups

### Distribution

- ⬜ [HUMAN] WinGet **1.0.1** — [microsoft/winget-pkgs#387878](https://github.com/microsoft/winget-pkgs/pull/387878)
- ⬜ [HUMAN] WinGet **1.1.0** — [microsoft/winget-pkgs#388074](https://github.com/microsoft/winget-pkgs/pull/388074)

### Hardware & manual QA

- ⬜ [HUMAN] Cover art on user hardware — `SLO_COVER_ART_DEBUG=1`
- ⬜ [HUMAN] ADR-0002 PCVR manual QA — [docs/HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md)

---

## Reference

| Topic | Location |
|-------|----------|
| Catalog maintenance | [docs/SEED_MAINTENANCE.md](docs/SEED_MAINTENANCE.md) |
| Lenticular markdown export | [docs/compatibility/LENTICULAR_GAMES.md](docs/compatibility/LENTICULAR_GAMES.md) |
| Live catalog site | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
| Completed work | [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
