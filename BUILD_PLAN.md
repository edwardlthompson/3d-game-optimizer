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
- ✅ **185/185** tests local
- 🔄 **Living 3D catalog + GitHub Pages browser** — Phases 1–2 shipped locally; Phases 3–6 open
- ✅ **186/186** tests local

---

## Sequential lane — Living 3D catalog

> Full design: mitigations in plan `.cursor/plans/living_3d_catalog_2994d8d7.plan.md` · Public URL target: `https://<user>.github.io/3d-game-optimizer/catalog/`

### Phase 1 — Schema & bootstrap data

- ✅ [AGENT] `schema-v2.json`, `sources/registry-v1.json`, `catalog-v2.json` (37 titles, 27 with 3D Vision)
- ✅ [AGENT] `merge-catalog.py` + `check-compatibility-catalog.py` + unit tests
- ✅ [AGENT] Extend `CompatibilityRepository` — load v2, fallback seed-v1

### Phase 2 — GitHub Pages catalog site

- ✅ [AGENT] `site/catalog/` — sortable table (Steam Store + 3D Ultra/hardware columns)
- ✅ [AGENT] Unified [`.github/workflows/pages.yml`](.github/workflows/pages.yml) → `dist/catalog/`
- ⬜ [HUMAN] Confirm GitHub Pages source = **GitHub Actions** ([docs/WEB_PROJECT_LAYOUT.md](docs/WEB_PROJECT_LAYOUT.md))

### Phase 3 — CI sync scripts

- ✅ [AGENT] [`.github/workflows/catalog-sync.yml`](.github/workflows/catalog-sync.yml) merge + validate (weekly + manual)
- ⬜ [AGENT] `scripts/sync-catalog/` — PCGW 3D Vision scrape, Steam enrich
- ⬜ [AGENT] Playwright scrapers (TrueGame, UEVR) + LKG fallback

### Phase 4 — Desktop library & sync

- ⬜ [AGENT] Library filters (Ultra / UEVR / TrueGame / 3D Vision), badges, catalog site link
- ⬜ [AGENT] `CatalogUpdateService` opt-in + SHA256 verify
- ⬜ [AGENT] Fix duplicate preset line on library tiles

### Phase 5 — Toolchain expansion

- ⬜ [AGENT] Extend `tool-manifest-v1.json` (VRto3D, bridges, 3D Vision manual)
- ⬜ [AGENT] Per-game recommended stack install in Settings

### Phase 6 — Tests & ship

- ⬜ [AGENT] Mitigation tests (confidence, LKG, hash verify, legal gate)
- ⬜ [AGENT] Archive to COMPLETED_TASKS when green

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
| Local release | [docs/LOCAL_RELEASE.md](docs/LOCAL_RELEASE.md) |
| Completed work | [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
