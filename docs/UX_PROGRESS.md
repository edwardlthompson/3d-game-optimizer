# UX Progress Tracker

## Current UX North Star

Deliver a one-click flow that helps users select a supported display, detect compatible games, apply safe presets, and launch with clear rollback options.

## Milestones

| Milestone | Scope | Status | Exit Criteria |
|---|---|---|---|
| M1 Foundation | shell layout, navigation, theme tokens, glossary | **Shipped** | keyboard navigation + baseline accessibility pass |
| M2 Setup Flow | display detection, WMI hardware scan, MUX warning, benchmark | **Shipped** | user reaches playable profile in <= 5 clicks |
| M3 Game Compatibility | game catalog, filter, tier indicators | **Shipped** | top 20 seeded games display compatibility reliably |
| M4 Tool Automation | install/verify external tools, launch preview summary | **Shipped** | unattended tool setup with clear error messages |
| M5 Library Intelligence | local folders, badges, filters, notes, collections | **Shipped** | local-only games launch without Steam ID |
| M6 Updates & Trust | About checker, apply+restart, diagnostic bundle, readiness score | **Shipped** | user-initiated update path with retry on failure |

## Sprint 34 deliverables (2026-06-14)

- **SystemSpecsScanner** — real WMI probes for CPU, GPU, VRAM, RAM, and display via `ManagementObjectSearcher`.
- **BenchmarkService** — deterministic timed micro-benchmark (no random score).
- **MuxGpuDetector** — hybrid graphics detection from `Win32_VideoController`.
- **PerformanceTierEstimator** — tiers driven by detected VRAM.
- **LaunchPreviewService** — depth/platform/toolchain summary shown in Play in 3D progress.
- **GlossaryView** — in-app glossary linked from shell navigation (`glossary` tag).

## Sprint 35 deliverables (2026-06-14)

- **LocalGameFolderRepository** — user watch-list paths with SQLite persistence.
- **LocalFolderGameScanner** — exe discovery with setup/uninstall heuristics.
- **LibrarySettingsView** — add/remove folders, re-scan.
- **Compatibility badges**, Why not ready filter, recent launches, notes, collections, preset freshness indicators.

## Sprint 36 deliverables (2026-06-14)

- **v2 experimental toggle** in Global 3D Settings (restart required for DI — Sprint 39).
- Per-game overrides, session profiles, HDR notice, config snapshot list UI.
- **About update checker** — Off/Startup/Daily/Weekly intervals, install-type detection, **Update and restart**.
- Voluntary Venmo donate link (external browser only).

## Sprint 37 deliverables (2026-06-14)

- **DisplayChangeMonitor** — EDID hot-plug re-prompt.
- **ViewingDistanceCoachView** — interactive coach.
- Multi-monitor launch picker (settings only — launch path wiring Sprint 41).
- OpenXR runtime override picker; unified Play in 3D / Play in VR library actions.
- Command palette real actions (cache, rescan, logs, safe launch) — global hotkey Sprint 40.

## Sprint 38 deliverables (2026-06-14)

- **LaunchDryRunService** — simulate launch from Troubleshooting.
- Enhanced **DiagnosticBundleService** (audit, display, toolchain, coexistence, updates).
- LAN preset export, seed contribution export (redacted JSON).
- **ReadinessScoreService** + offline onboarding path in setup wizard.
- `3dgo://play/{appid}` protocol handler.
- README screenshots remain synthetic — real WinUI captures deferred to Sprint 43.

## UX Risks

- Confusion between "display supported" and "game fully supported" — mitigated by glossary and launch preview copy.
- Overly technical terms around stereoscopic rendering methods — mitigated by GlossaryView.
- Silent tool automation trust concerns if status feedback is weak — mitigated by launch preview step and progress overlay.
- PlayIn3D progress may show steps before real work completes — mitigated in Sprint 41 (readiness/config wiring).

## Immediate UX Actions (Sprints 39–44)

| Action | Sprint |
|--------|--------|
| Shell update-available badge | 39 |
| v2 toggle restart prompt | 39 |
| Command palette keyboard shortcut | 40 |
| Streamer hotkey registration | 40 |
| PlayIn3D progress honesty | 41 |
| Real README screenshots | 43 |

## Sprint 45 deliverables (2026-06-14)

- **Startup progress** — specs scan, library index, and benchmark publish `IsComplete`; activity bar marshaled to UI thread; cover art deferred to background prefetch.
- **SpatialLabs 15" detection** — EDID/PNP probe wired into `DisplayAutoDetector` with catalog wildcard signatures and name heuristics.
- **Orange high-contrast theme** — brand/accent tokens, app icon pipeline, splash logo.
- **Setup wizard verbosity** — install progress bar + log, async benchmark feedback, detection status with EDID signature.
- **Settings reorg** — Health merged into Settings expanders; Config Snapshots hidden; OpenXR runtime `DisplayMemberPath="Label"`; footer gear disabled.
- **Session profiles + v2 checkboxes** — LAN/hybrid/Epic-GOG integrations with restart banner.
- **Steam fetch transparency** — per-AppID status messages via `ExternalDataGateway`.
- **Quick Actions** — renamed Commands nav item, `Ctrl+K` shortcut, palette descriptions.
- **Validation** — `DisplayAutoDetectorTests`, `Sprint45UxTests`, `scripts/smoke-ui-flows.ps1`.

## Sprint 46 deliverables (2026-06-15)

- **Silent install JSON fix** — `OptimalDefaultsService` matches seed array schema; all catalog `recommendedProfileId` entries present; wizard stays on step 1 when install fails.
- **Cover art pipeline** — CDN-first resolution, soft-fail HTTP, per-app prefetch with `artwork-prefetch.log`, library auto-refresh after prefetch, placeholder tile when art missing.
- **ASV15 catalog** — SpatialLabs View / View Pro 15.6" (`acer-asv15-1`) in picker; laptop signatures tightened; external monitor preferred in matcher; live viewing-distance update on picker change.
- **Settings layout** — full-width Expanders with gutters and uniform header height via `SettingsExpanderStyle`.
- **Validation** — `Sprint46Tests` (130 total passing); smoke scripts verify process stays alive post-startup.

## Sprint 47 deliverables (2026-06-15)

- **Library platform connections** — platform link UI for Steam, Epic, GOG, and Ubisoft in Library Settings; local install path validation.
- **Filter/sort persistence** — library toolbar preferences saved across sessions.
- **Cover art URI fix** — expanded prefetch batch; live Steam review/metadata prefetch.
- **Settings gutters** — inner category spacing in Global 3D Settings expanders.

## Sprint 48 deliverables (2026-06-15)

- **Cover art UI-thread refresh** — observable tile binding + cache bust after prefetch.
- **Wizard toolchain checklist** — profile-filtered install steps from tool manifest.
- **3D Display settings** — launch target display + OpenXR runtime override with Off option.
- **AppWindow icon** — taskbar/window icon via `SetIcon`.

## Sprint 49 deliverables (2026-06-15)

- **Elevated helper installs** — real bundled tool install path; OpenXR Off end-to-end.
- **Cover tile refresh** — store placeholder tiles when CDN art missing.
- **Review hardening** — production gap fixes from Sprint 49 QA pass.

## Sprint 50 deliverables (2026-06-15)

- **Tool manifest bundled installs** — `installMode` in manifest; observable cover binding polish.
- **Archive hygiene** — `.gitignore` for `artifacts/test-publish/` and `artifacts/fd-test/`.

## Sprint 51 deliverables (2026-06-15)

- **Modularization** — `GameLibraryViewModel` partials; library indexer/prefetch/merger split; diagnostics per-service files.
- **ToolInstallDetector** — manifest cache + bounded Program Files scan.
- **FOR_AGENTS.md** — `UiThreadDispatcher` / background host contract documented.
- **150/150 tests green** after file-budget splits.

## Sprint 52 deliverables (2026-06-15)

- **Vendor toolchain policy** — manual-only vendor tools documented in `docs/TOOLCHAIN.md` and `tool-manifest-v1.json`.
- **LibraryIndexMerger split** — external/Steam-owned/placeholder assigner orchestration.
- **Config snapshots** — real JSON snapshot/rollback via `ConfigSnapshotService`; restore UI in Settings → Advanced.
- **Platform scope** — Epic/GOG/Ubisoft local-only scope in `docs/PLATFORM_CONNECTIONS.md` and Library Settings copy.

## Sprint 40 deliverables (2026-06-15)

- **Streamer hotkeys** — `StreamerHotkeyService` registers Ctrl+Shift+3/S/L/M/H/R when app has focus.
- **Library toolbar MVVM** — primary toolbar buttons bind to `GameLibraryViewModel` commands via `x:Bind`.
- **ViewModels** — `AboutViewModel`, `TroubleshootingViewModel`, `Global3DSettingsViewModel` (launch safety); `LibrarySettingsViewModel` retained.
- **Glossary seed** — `data/glossary/glossary-v1.json` drives dynamic `GlossaryView` content.

## Sprint 41 deliverables (2026-06-15)

- **PlayIn3D honesty** — progress steps map to real readiness, preset cache, config apply, optimal defaults, snapshot, and launch (12 steps).
- **Multi-monitor handoff** — selected launch display flows through `LaunchContext` and audit trail.
- **OpenXR handoff** — resolves SteamVR root from runtime.json; starts vrstartup before Steam VR applaunch.
- **Rollback integration** — failed launch restores override snapshot; P0 QA verifies round-trip.

## Completed UX Actions

- ~~Add glossary for terms like SBS, depth, convergence, and tier levels.~~ **Done** — GlossaryView in shell nav.
- Include "What changed" summary before applying presets — launch preview covers platform/depth/toolchain before Play in 3D.
- Ensure every automated action has a rollback or undo guidance link — Safe launch + troubleshooting copy remain primary paths.
