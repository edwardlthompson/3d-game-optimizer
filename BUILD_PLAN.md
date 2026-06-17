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
| Product | **v1.4.0** shipped |
| CI | Local Steam sync batch ready to merge |
| GitHub Pages | **Live** — [catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/) |
| Steam sync | **Blocked on HUMAN** — KV id + Cloudflare/Steam secrets |

---

## Sequential — Steam library sync

> [Design](.cursor/plans/steam_library_sync.plan.md) · [Ops](docs/STEAM_CATALOG_SYNC.md)

- ⬜ [HUMAN] Cloudflare KV namespace → `workers/steam-library/wrangler.toml`
- ⬜ [HUMAN] GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `STEAM_WEB_API_KEY`
- ⬜ [HUMAN] Post-deploy smoke — checklist in `docs/STEAM_CATALOG_SYNC.md` § Post-deploy smoke
- ✅ [AUTO] Deploy worker → `STEAM_SYNC_WORKER_URL` → rebuild Pages
- ✅ [AGENT] KV guard, worker + catalog tests, CI wiring

---

## Parallel — HUMAN backlog (hardware only)

| Task | Notes |
|------|-------|
| GPU / display QA | After `run-out-of-band-qa.ps1` |
| Headset VR launch | SteamVR + native/UEVR titles |
| Odyssey Hub CSV export | From installed app |
| CodeQL SARIF upload | Enable for product-release `--strict` gate |

```powershell
pwsh scripts/run-out-of-band-qa.ps1 -UserCache
bash scripts/run-out-of-band-qa.sh
```

See [docs/HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md).

---

## Reference

| Topic | Location |
|-------|----------|
| Planning review | [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md) |
| Release gate | [docs/PRODUCT_RELEASE_GATE.md](docs/PRODUCT_RELEASE_GATE.md) |
| Catalog maintenance | [docs/SEED_MAINTENANCE.md](docs/SEED_MAINTENANCE.md) |
| Live catalog | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
