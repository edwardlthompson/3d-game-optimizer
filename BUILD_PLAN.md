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

## Status (2026-07-12)

| Track | State |
|-------|--------|
| Product | **v1.5.0** shipped — [release](https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.5.0) |
| Template | **v0.7.1** on `main` |
| GitHub Pages | **Live** — [catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/) |
| Steam sync | **Blocked on HUMAN** — KV + secrets + smoke |
| Hardware QA | **HUMAN** — [HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md) |

---

## Sequential — HUMAN gate (Steam + ship)

> Agent UX sprints A–C archived in [COMPLETED_TASKS.md](COMPLETED_TASKS.md). Commit/push includes audit + UX batches.

- ✅ [HUMAN] Review + commit/push (audit + UX A–C) — via `/ship` 2026-07-12
- ✅ [AUTO] After push — CI + Security Scan + CodeQL green; Dependabot Critical/High = 0
- ⬜ [HUMAN] Cloudflare KV namespace id → `workers/steam-library/wrangler.toml`
- ⬜ [HUMAN] GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `STEAM_WEB_API_KEY`
- ⬜ [HUMAN] Post-deploy smoke — [docs/STEAM_CATALOG_SYNC.md](docs/STEAM_CATALOG_SYNC.md) § Post-deploy smoke (Connect Steam visible, OpenID, Lib ✓, `/health`)
- ⬜ [HUMAN] Real WinUI README screenshots (replace synthetic Sprint 43 assets)
- ⬜ [HUMAN] Hardware Play in 3D / VR sign-off — [docs/HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md)
- ✅ [AGENT] UX-A trust (Simple mode, theme, pre-launch confirm, setup wizard)
- ✅ [AGENT] UX-B discovery (detail notes/queue, readiness, filter flyout, Ctrl+K, remove orphan health)
- ✅ [AGENT] UX-C catalog Steam UX (unavailable/loading/confirm/unmatched/prefs/filters)
- ✅ [AGENT] Ship v1.5.0 — tag + zip/MSI on GitHub Releases

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
| Scorecard pin + permissions | AGENT | `.github/workflows/**` (CODE_REVIEW F-007) |
| Dependabot PR #8 (github-actions) | HUMAN | Review + merge open actions bump |
| WinUI i18n resource strings | AGENT | XAML → .resw |

Run `bash scripts/check-parallel-scope.sh` before dispatch.

---

## Reference

| Topic | Location |
|-------|----------|
| Slash commands | [`.cursor/commands/README.md`](.cursor/commands/README.md) |
| UX tracker | [docs/UX_PROGRESS.md](docs/UX_PROGRESS.md) |
| Steam ops | [docs/STEAM_CATALOG_SYNC.md](docs/STEAM_CATALOG_SYNC.md) |
| Agent memory | [AGENT_MEMORY.md](AGENT_MEMORY.md) |
| Live catalog | https://edwardlthompson.github.io/3d-game-optimizer/catalog/ |
