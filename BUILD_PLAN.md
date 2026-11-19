# Build Plan

<!-- remaining-tally -->
**Remaining:** (tally on next sync)
<!-- /remaining-tally -->

Live board. Canonical shape: agent-project-bootstrap `BUILD_PLAN_TEMPLATE.md`.
Finished work: [`COMPLETED_TASKS.md`](COMPLETED_TASKS.md).

**Who:** `AGENT` code · `HUMAN` person · `ADB` device · `AUTO` CI/scripts
**Venue (AGENT only):** `[LOCAL]` This Computer · `[CLOUD]` Cursor Cloud
**State:** 🔲 open · ✅ done · ❌ blocked

Format: `🔲 [AGENT][LOCAL] Short task — scope: path/prefix`.

### Open PRs (synced)

> Auto-managed. Do not hand-edit inside the markers.

<!-- open-prs-sync:begin -->
_No open Dependabot or Release Please PRs._
<!-- open-prs-sync:end -->

### Template gaps (synced)

> Auto-managed. Do not hand-edit inside the markers.

<!-- template-gaps-sync:begin -->
_No template gaps._
<!-- template-gaps-sync:end -->

### Waiting on a person

1. 🔲 [HUMAN] Cloudflare KV namespace id → `workers/steam-library/wrangler.toml`
2. 🔲 [HUMAN] GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `STEAM_WEB_API_KEY`
3. 🔲 [HUMAN] Post-deploy smoke — [STEAM_CATALOG_SYNC.md](docs/STEAM_CATALOG_SYNC.md) § Post-deploy smoke (Connect Steam visible, OpenID, Lib ✓, `/health`)
4. 🔲 [HUMAN] Optional: copy `.cursor/hooks.json.example` → `.cursor/hooks.json` after local smoke

### Local agent (This Computer)

Standing queue for This Computer. Rows: `🔲 [AGENT][LOCAL] … — scope: path`.

<!-- local-agent-lane:begin -->
- Status: 🔲 open · ✅ done · ❌ blocked
- 🔲 [AGENT][LOCAL] Rebuild on .NET 8.0.34 and bump Microsoft.* packages — scope: src/ (news P1, 2026-11-13) https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-november-2026-servicing-updates/
- 🔲 [AGENT][LOCAL] Cut/update GitHub Release with signed Android APK/AAB — scope: examples/android/
<!-- local-agent-lane:end -->

### Cloud agent (Cursor Cloud)

Standing queue for Cursor Cloud. Rows: `🔲 [AGENT][CLOUD] … — scope: path`.

<!-- cloud-agent-lane:begin -->
_No cloud agent items._
<!-- cloud-agent-lane:end -->

## Archive

Older sprints: [`COMPLETED_TASKS.md`](COMPLETED_TASKS.md).

## Preserved source board

Quoted so no prior task is lost. Prefer the lanes above; do not invent work.

<details>
<summary>Previous board</summary>

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

## News inbox (synced)

- 🔲 [AGENT] Rebuild on .NET 8.0.34 and bump Microsoft.* packages — scope: src/ (news P1, 2026-11-13) https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-november-2026-servicing-updates/

</details>

<!-- argus-portfolio-chores:start -->
## Argus portfolio chores — Android release

Synced from argus-runner. **Agent must ship binaries**, not only docs.

| Status | Venue | Task |
|--------|-------|------|
| 🔲 | `[AGENT][LOCAL]` — scope: `examples/android/` | **Cut/update GitHub Release with signed Android APK/AAB** |

### Agent instructions (paste / follow)

```text
Repo: edwardlthompson/3d-game-optimizer
Goal: Latest GitHub Release must include a real Android binary (.apk and/or .aab).

Why: Latest release is Windows MSI/ZIP only — also ship Android APK/AAB for the Android surface.

Steps:
1) Read AGENT.md + docs for the Android ship path. Prefer gradle under `examples/android/`.
2) Ensure signing is available (local keystore / CI secrets already used by this repo). Do not invent a new Play upload.
3) From `examples/android` (or repo root if that is the documented path):
     ./gradlew assembleRelease
     # and if the project ships bundles:
     ./gradlew bundleRelease
4) Locate outputs under app/build/outputs/apk/ and app/build/outputs/bundle/.
5) Create a new tag/release OR update the latest release:
     gh release list -R edwardlthompson/3d-game-optimizer --limit 3
     # Prefer the repo's normal /ship or Release Please flow if documented; otherwise:
     gh release create <TAG> --generate-notes -R edwardlthompson/3d-game-optimizer
     gh release upload <TAG> <path-to.apk> [<path-to.aab>] -R edwardlthompson/3d-game-optimizer --clobber
6) Expected artifact names: *-release.apk / *.aab
7) Verify:
     gh release view <TAG> -R edwardlthompson/3d-game-optimizer --json assets --jq ".assets[].name"
   Gate: at least one asset ends with .apk or .aab.
8) Mark this BUILD_PLAN row ✅ and note the tag + asset names in the commit/PR body.

Do not force-push main. Do not delete the repo. Prefer PR + release assets over committing binaries to git.
```

Releases page: https://github.com/edwardlthompson/3d-game-optimizer/releases
<!-- argus-portfolio-chores:end -->
