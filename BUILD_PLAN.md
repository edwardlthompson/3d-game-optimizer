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
| Template | **v0.7.1** aligned — commit `d198d92` pending push |
| CI | Green on `main` (last push `8948427`) |
| GitHub Pages | **Live** — [catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/) |
| Steam sync | **Blocked on HUMAN** — KV id + Cloudflare/Steam secrets (F-002) |

---

## Sequential — Audit sprint (2026-06-18)

> Findings: `CODE_REVIEW.md` (local, gitignored) · Invoke `/audit` again after push.

1. ✅ [AGENT] Commit template migration + slash commands + gate scripts batch (F-001) — `d198d92`
2. ⬜ [HUMAN] Push to `main` and run `bash scripts/check-github-ci.sh HEAD --wait 600` (or `/push`)
3. ✅ [AGENT] Document Windows gate fallback in `docs/FOR_AGENTS.md` — WSL/Git Bash vs CI (F-003)
4. ✅ [AUTO] Local product tests — dotnet 223/223, catalog 42/42, worker 35/35

---

## Sequential — Steam library sync

> [Design](.cursor/plans/steam_library_sync.plan.md) · [Ops](docs/STEAM_CATALOG_SYNC.md) · F-002

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

## Parallel — Deferred from audit

| Task | Owner | Finding | Isolated scope |
|------|-------|---------|----------------|
| WinUI file-budget sweep | AGENT | F-004 | `src/SpatialLabsOptimizer/**`, `ElevatedHelper/**` |

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
