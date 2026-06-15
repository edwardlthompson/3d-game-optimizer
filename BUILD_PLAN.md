# Build Plan

> Active board only. Archives: [COMPLETED_TASKS.md](COMPLETED_TASKS.md) · Design: [`.cursor/plans/steam_library_sync.plan.md`](.cursor/plans/steam_library_sync.plan.md)

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
- ✅ **Game Rank** — Steam + 3D blend; default sort descending
- 🔄 **Steam library sync** — agent code complete; `[HUMAN]` Cloudflare deploy + `STEAM_SYNC_WORKER_URL`
- ⬜ [HUMAN] Confirm GitHub Pages source = **GitHub Actions**
- ⬜ [HUMAN] SteamDB price backfill ToS review (Phase 4b)

---

## Sequential lane — Steam library sync

> Design: [`.cursor/plans/steam_library_sync.plan.md`](.cursor/plans/steam_library_sync.plan.md) · Ops: [docs/STEAM_CATALOG_SYNC.md](docs/STEAM_CATALOG_SYNC.md)

- ✅ [AGENT] Cloudflare Worker — OpenID, `GetOwnedGames`, KV single-use tokens, rate limits
- ✅ [AGENT] Catalog client — map owned App IDs → library checkmarks; Connect Steam UI
- ✅ [AGENT] CI — `steam-library-worker.yml`, `VITE_STEAM_SYNC_URL` in Pages build
- ⬜ [HUMAN] Cloudflare account + `wrangler kv namespace create` + GitHub secrets (`CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `STEAM_WEB_API_KEY`, `STEAM_SYNC_WORKER_URL`)
- ⬜ [HUMAN] Deploy worker + set repo variable `STEAM_SYNC_WORKER_URL` → enable Connect Steam on live site

### Critique (ship gate)

- Empty Steam library → privacy-help banner + optional user API key (localStorage, forward-only)
- Token: 5 min KV TTL, single use, `replaceState` strips URL param
- Post-sync banner: matched / owned / unmatched counts
- Connect Steam hidden when `VITE_STEAM_SYNC_URL` unset

---

## Sequential lane — Catalog layout polish (archived)

- ✅ [AGENT] Text wrap on title / play methods / hardware; drop TrueGame + 3D Vision columns
- ✅ [AGENT] Left-align filter popover checkboxes
- ✅ [AGENT] Title opens Steam store (`steam://store/{appId}`); Buy column removed
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
