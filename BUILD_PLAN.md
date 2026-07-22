# Build Plan

<!-- cursor-hooks: off -->

> Active board only. Finished work: [COMPLETED_TASKS.md](COMPLETED_TASKS.md). Alignment notes: [docs/BOOTSTRAP_ALIGNMENT.md](docs/BOOTSTRAP_ALIGNMENT.md).
>
> Cursor hooks are **opt-in**: copy `.cursor/hooks.json.example` → `.cursor/hooks.json` and remove the `cursor-hooks: off` marker above after a local smoke test.

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform |
| `ADB` | Android device/emulator testing |
| `AUTO` | CI / scripts / bots |

Status: 🔲 open · ✅ done · ❌ blocked

---

## Status (2026-07-21)

| Track | State |
|-------|--------|
| Product | **v1.5.0** — [release](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.5.0) |
| Template | **v0.15.0** — see [BOOTSTRAP_ALIGNMENT.md](docs/BOOTSTRAP_ALIGNMENT.md) |
| GitHub Pages | **Live** — [catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/) |
| Next gate | **HUMAN** — Steam Connect live + hardware QA |

---

## Sequential — AGENT (bootstrap alignment)

> Archived to [COMPLETED_TASKS.md](COMPLETED_TASKS.md) after `/push` (template v0.15.0; product remains v1.5.0).

---

## Sequential — Human & device (after automation)

> Steam Connect remains the product blocker. Template hooks enablement is optional.

1. 🔲 [HUMAN] Cloudflare KV namespace id → `workers/steam-library/wrangler.toml`
2. 🔲 [HUMAN] GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `STEAM_WEB_API_KEY`
3. 🔲 [HUMAN] Post-deploy smoke — [STEAM_CATALOG_SYNC.md](docs/STEAM_CATALOG_SYNC.md) § Post-deploy smoke (Connect Steam visible, OpenID, Lib ✓, `/health`)
4. 🔲 [HUMAN] Optional: copy `.cursor/hooks.json.example` → `.cursor/hooks.json` after local smoke

---

## Parallel — HUMAN backlog

| Task | Scope |
|------|--------|
| Real WinUI README screenshots | Replace synthetic Sprint 43 assets |
| GPU / display QA | [HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md) |
| Headset VR launch | SteamVR + native/UEVR titles |
| Odyssey Hub CSV export | From installed app |
| CodeQL SARIF upload | Enable for product-release `--strict` gate |

```powershell
pwsh scripts/run-out-of-band-qa.ps1 -UserCache
bash scripts/run-out-of-band-qa.sh
```

---

## Parallel — Deferred (AGENT)

| Task | Isolated scope |
|------|----------------|
| WinUI file-budget sweep | `src/SpatialLabsOptimizer/**`, `ElevatedHelper/**` |
| Scorecard pin + permissions | `.github/workflows/**` (CODE_REVIEW F-007) |
| WinUI i18n resource strings | XAML → .resw |

Run `bash scripts/check-parallel-scope.sh` before dispatch.

---

## Reference

| Topic | Location |
|-------|----------|
| Slash commands | [`.cursor/commands/README.md`](.cursor/commands/README.md) |
| Batch commands | [`docs/help/BATCH_COMMANDS.md`](docs/help/BATCH_COMMANDS.md) |
| UX tracker | [docs/UX_PROGRESS.md](docs/UX_PROGRESS.md) |
| Steam ops | [docs/STEAM_CATALOG_SYNC.md](docs/STEAM_CATALOG_SYNC.md) |
| Agent memory | [AGENT_MEMORY.md](AGENT_MEMORY.md) |
| Live catalog | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
