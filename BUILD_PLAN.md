# Build Plan

> Active board only. Finished work: [COMPLETED_TASKS.md](COMPLETED_TASKS.md)

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform |
| `AUTO` | CI / scripts / bots |

---

## Status (2026-06-17)

| Track | State |
|-------|--------|
| Product | **v1.3.0** shipped — [release](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.3.0) |
| Distribution | GitHub Releases only — zip + MSI; in-app updates |
| Steam sync | AGENT done — blocked on Cloudflare deploy |

---

## Sequential — Steam library sync

> [Design](.cursor/plans/steam_library_sync.plan.md) · [Ops](docs/STEAM_CATALOG_SYNC.md)

- ⬜ [HUMAN] Cloudflare KV namespace → update `workers/steam-library/wrangler.toml`
- ⬜ [HUMAN] GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `STEAM_WEB_API_KEY`
- ⬜ [HUMAN] Deploy worker; set **`STEAM_SYNC_WORKER_URL`** → rebuild Pages

### Critique

- Connect Steam hidden until `VITE_STEAM_SYNC_URL` is set at build time
- Token: 5 min KV TTL, single use; URL param stripped via `replaceState`

---

## Parallel — AGENT backlog

| Priority | Task |
|----------|------|
| P2 | Worker Vitest — exchange, rate limits, KV lifecycle (Miniflare) |
| P2 | Extend `scripts/smoke-grid.mjs` — Game Rank sort assertions |
| P2 | Extend `check-github-ci.sh` — catalog-site / steam-worker workflows |
| P2 | CodeQL SARIF upload — warn in release gate on failure |
| P2 | Split `site/catalog/src/grid.ts` (~300 lines; currently exempt) |
| P3 | `DpapiSecretStore` — per-install random entropy |
| P3 | `smoke-cover-art.ps1` — document manual-only or add UI automation |

---

## Parallel — HUMAN backlog

| Task | Notes |
|------|-------|
| GitHub Pages source | Set to **GitHub Actions** in repo settings |
| SteamDB price backfill | Phase 4b ToS review |
| Cover art hardware QA | `SLO_COVER_ART_DEBUG=1` |
| PCVR manual QA | [HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md) |
| Odyssey Hub CSV export | From installed app |

---

## Reference

| Topic | Location |
|-------|----------|
| Release notes | [docs/RELEASE_NOTES_v1.3.0.md](docs/RELEASE_NOTES_v1.3.0.md) |
| Planning review | [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md) |
| Release gate | [docs/PRODUCT_RELEASE_GATE.md](docs/PRODUCT_RELEASE_GATE.md) |
| Catalog maintenance | [docs/SEED_MAINTENANCE.md](docs/SEED_MAINTENANCE.md) |
| Live catalog | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
