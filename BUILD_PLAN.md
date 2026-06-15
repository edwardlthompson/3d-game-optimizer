# Build Plan

> Active board only. Archives: [COMPLETED_TASKS.md](COMPLETED_TASKS.md) · Design: [`.cursor/plans/catalog_ux_v2.plan.md`](.cursor/plans/catalog_ux_v2.plan.md)

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform |
| `AUTO` | CI / scripts / bots |

**Status key:** ✅ done · 🔄 in progress · ⬜ open

---

## Status (2026-06-15)

- ✅ **Lenticular catalog v1** — 686 titles, TanStack grid, Pages deploy
- ✅ **Catalog UX v2** — spreadsheet filters, play methods, wishlist PWA, price history
- ⬜ [HUMAN] Confirm GitHub Pages source = **GitHub Actions**
- ⬜ [HUMAN] SteamDB price backfill ToS review (Phase 4b)

---

## Sequential lane — Catalog UX v2

### Phase 0 — Steam stats fix ✅

- ✅ [AGENT] Steam reviews (`appreviews`) + concurrent players (606 / 623 of 638 linked)
- ✅ [AGENT] `test_enrich_steam_stats.py` · VRto3D HTML title strip

### Phase 1 — Spreadsheet column filters ✅

- ✅ [AGENT] Checkbox popovers · price $5 · reviews 10% · players ×100 buckets

### Phase 2 — Play methods column ✅

- ✅ [AGENT] Full `platformSupport[]` column + filter (all platform keys)

### Phase 3 — Wishlist + PWA ✅

- ✅ [AGENT] localStorage wishlist · manifest · service worker · export/import

### Phase 4 — Price history + graph ✅

- ✅ [AGENT] `append-price-history.py` · click price → SVG chart · sync banner
- ⬜ [HUMAN] SteamDB backfill (deferred)

### Phase 5 — Ship ✅

- ✅ [AUTO] Push → GitHub Pages redeploy (`865d739`)
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
| Live catalog site | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
| Completed work | [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
