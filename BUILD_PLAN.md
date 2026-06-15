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

- ✅ **Catalog UX v2** — spreadsheet filters, play methods, wishlist PWA, price history
- ✅ **Catalog layout polish** — wrap, column trim, buy links
- ⬜ [HUMAN] Confirm GitHub Pages source = **GitHub Actions**
- ⬜ [HUMAN] SteamDB price backfill ToS review (Phase 4b)

---

## Sequential lane — Catalog layout polish

> Design: [`.cursor/plans/catalog_table_layout_fix.plan.md`](.cursor/plans/catalog_table_layout_fix.plan.md)

- ✅ [AGENT] Text wrap on title / play methods / hardware; drop TrueGame + 3D Vision columns
- ✅ [AGENT] Left-align filter popover checkboxes
- ✅ [AGENT] Buy on Steam → `target="_blank"` + `steam://store/{appId}` handoff
- ✅ [AUTO] Push → GitHub Pages redeploy (`f8d93ed`)

---

## Sequential lane — Catalog UX v2 (archived)

### Phase 0–5 ✅

Steam stats, spreadsheet filters, play methods, wishlist PWA, price history, ship (`865d739`).

---

## Active follow-ups

### Distribution

- ⬜ [HUMAN] WinGet **1.0.1** — [microsoft/winget-pkgs#387878](https://github.com/microsoft/winget-pkgs/pull/387878)
- ⬜ [HUMAN] WinGet **1.1.0** — [microsoft/winget-pkgs#388074](https://github.com/microsoft/winget-pkgs/pull/388074)

### Hardware & manual QA

- ⬜ [HUMAN] Cover art on user hardware — `SLO_COVER_ART_DEBUG=1`
- ⬜ [HUMAN] ADR-0002 PCVR manual QA — [docs/HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md)
- ⬜ [HUMAN] Odyssey Hub CSV export from installed app

---

## Reference

| Topic | Location |
|-------|----------|
| Catalog maintenance | [docs/SEED_MAINTENANCE.md](docs/SEED_MAINTENANCE.md) |
| Live catalog site | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
| Completed work | [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
