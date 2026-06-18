# Build Plan

> Active board only. Finished work: [COMPLETED_TASKS.md](COMPLETED_TASKS.md)

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform |
| `ADB` | Android device/emulator testing |
| `AUTO` | CI / scripts / bots |

---

## Status (2026-06-18)

| Track | State |
|-------|--------|
| Product | **v1.4.0** shipped — [release](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.4.0) |
| Template | **v0.7.1** on `main` (`a5d4f67`) — slash commands + migration live |
| CI | **Green** on `a5d4f67` — CI, Security Scan, CodeQL |
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

| Task | Owner | Isolated scope |
|------|-------|----------------|
| GPU / display QA | HUMAN | Manual — `docs/HARDWARE_QA_OUT_OF_BAND.md` |
| Headset VR launch | HUMAN | SteamVR + native/UEVR titles |
| Odyssey Hub CSV export | HUMAN | From installed app |
| CodeQL SARIF upload | HUMAN | Enable for product-release `--strict` gate |

```powershell
pwsh scripts/run-out-of-band-qa.ps1 -UserCache
bash scripts/run-out-of-band-qa.sh
```

---

## Parallel — Deferred

| Task | Owner | Isolated scope |
|------|-------|----------------|
| WinUI file-budget sweep | AGENT | `src/SpatialLabsOptimizer/**`, `ElevatedHelper/**` |

Run `bash scripts/check-parallel-scope.sh` before dispatch.

---

## Reference

| Topic | Location |
|-------|----------|
| Slash commands | [`.cursor/commands/README.md`](.cursor/commands/README.md) |
| Planning review | [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md) |
| Product release gate | [docs/PRODUCT_RELEASE_GATE.md](docs/PRODUCT_RELEASE_GATE.md) |
| Agent memory | [AGENT_MEMORY.md](AGENT_MEMORY.md) |
| Live catalog | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
