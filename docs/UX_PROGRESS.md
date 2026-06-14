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

## Completed UX Actions

- ~~Add glossary for terms like SBS, depth, convergence, and tier levels.~~ **Done** — GlossaryView in shell nav.
- Include "What changed" summary before applying presets — launch preview covers platform/depth/toolchain before Play in 3D.
- Ensure every automated action has a rollback or undo guidance link — Safe launch + troubleshooting copy remain primary paths.
