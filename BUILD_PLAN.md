# Build Plan

> Active board only. Archives: [COMPLETED_TASKS.md](COMPLETED_TASKS.md)

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform |
| `AUTO` | CI / scripts / bots |

**Status key:** ✅ done · 🔄 in progress · ⬜ open

---

## Status (2026-06-15)

- 🔄 **Steam library sync** — AGENT complete; awaiting Cloudflare deploy
- ⬜ **ValidateSteamAsync tests** — mocked gateway unit tests
- ⬜ [HUMAN] GitHub Pages source = **GitHub Actions**
- ⬜ [HUMAN] SteamDB price backfill ToS (Phase 4b)

---

## Sequential lane — Steam library sync

> Design: [`.cursor/plans/steam_library_sync.plan.md`](.cursor/plans/steam_library_sync.plan.md) · Ops: [docs/STEAM_CATALOG_SYNC.md](docs/STEAM_CATALOG_SYNC.md)

- ⬜ [HUMAN] Cloudflare account + `wrangler kv namespace create` → update `workers/steam-library/wrangler.toml`
- ⬜ [HUMAN] GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `STEAM_WEB_API_KEY`
- ⬜ [HUMAN] Deploy worker; set repo variable **`STEAM_SYNC_WORKER_URL`** → rebuild Pages (enables Connect Steam)

### Critique

- Connect Steam hidden until `VITE_STEAM_SYNC_URL` is set at build time
- Empty library → privacy banner (Game details must be Public)
- Token: 5 min KV TTL, single use; URL param stripped via `replaceState`

---

## Sequential lane — Desktop tests

- ⬜ [AGENT] `ValidateSteamAsync` unit tests with mocked `ExternalDataGateway`

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
| Steam sync ops | [docs/STEAM_CATALOG_SYNC.md](docs/STEAM_CATALOG_SYNC.md) |
| Live catalog | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
| Completed work | [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
