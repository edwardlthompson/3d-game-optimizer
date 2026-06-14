# Build Plan

> Active sprint board. Archives: [COMPLETED_TASKS.md](COMPLETED_TASKS.md) · Evidence: [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md)

## Owner Label Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform (e.g. purchase EV cert) |
| `AUTO` | CI/scripts/bots |

Filter: `grep '\[AGENT\]' BUILD_PLAN.md`

## Status (2026-06-14)

| Area | State |
|------|-------|
| Active sprint | **Sprint 39** — CI truth + release ops |
| Product version | 1.1.0 (`product-version.json`) |
| Git state | Sprint 39 AGENT tasks complete; pending commit/push to `main` |
| Tests | 118/118 Release locally — regenerate count from CI after push |
| CI / release automation | **Pending push** — local complete; verify after single PR to `main` |
| v2 services | Settings toggle or `SPATIALLABS_ENABLE_V2=true` (DI frozen until restart — Sprint 39) |
| Local release | `scripts/build-product-local.ps1` — [docs/LOCAL_RELEASE.md](docs/LOCAL_RELEASE.md) |
| Template track | AUTO via `release-auto-merge.yml` |

## Open follow-ups

| Item | Owner | Notes |
|------|-------|-------|
| Sprint 39 ship gate | HUMAN | Approve single commit/PR of Sprints 32–38 + automation to `main` |
| WinGet merge | HUMAN | [PR #387878](https://github.com/microsoft/winget-pkgs/pull/387878) — CLA/validation pending |
| WinGet v1.1.0 manifest | HUMAN | After merge: `scripts/prepare-winget-submission.ps1 -OpenPr` |
| EV Authenticode cert | HUMAN | Optional; sideload signing via `scripts/codesign-common.ps1` |
| Org admin for GHAS | HUMAN | Optional if API returns 403 on security settings |
| Real WinUI README screenshots | AGENT | Deferred to Sprint 43 (synthetic placeholders OK until then) |
| ADR-0002 PCVR manual QA | HUMAN | Physical SteamVR/OpenXR spot-check — [HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md) |

---

## Active Sprint 39 — CI truth + release ops `[SEQUENTIAL]`

**Goal:** One approved PR lands Sprints 32–38 on `main`; CI and docs match reality.

| Task | Owner | Priority | Key paths |
|------|-------|----------|-----------|
| Single commit/PR to `main` (workflows + product + scripts + docs) | HUMAN + AGENT | P0 | entire working tree |
| Verify CI green on `main` | AUTO | P0 | `.github/workflows/ci.yml` |
| Fix `check-legal-consistency.sh` ViewModels path | AGENT | P0 | `scripts/check-legal-consistency.sh` |
| Confirm `.gitignore` excludes build artifacts | AGENT | P0 | `.gitignore`, `artifacts/` |
| Dispatch `product-release.yml` for `SpatialLabsOptimizer-v1.1.0` | AUTO | P1 | `.github/workflows/product-release.yml` |
| v2 toggle restart prompt when DI differs from saved pref | AGENT | P1 | `Global3DSettingsView.xaml.cs`, `ServiceCollectionExtensions.cs` |
| Shell `UpdateAvailable` badge / InfoBar | AGENT | P1 | `ShellViewModel.cs`, `ShellPage.xaml` |
| `UpdateApplyService` + applier unit tests | AGENT | P1 | `Infrastructure/Updates/UpdateApplyService.cs` |
| About **Retry update** banner for `update_restart_pending` | AGENT | P1 | `AboutView.xaml.cs`, `UpdateApplyService.cs` |
| Sync `UX_PROGRESS.md` (Sprints 35–38) | AGENT | P1 | `docs/UX_PROGRESS.md` |
| Refresh `PLANNING_REVIEW.md` evidence | AGENT | P1 | `docs/PLANNING_REVIEW.md` |

**Exit criteria:** `main` CI green; `check-legal-consistency.sh` passes; test count matches CI; `product-release.yml` dispatchable from remote.

---

## Forward backlog — Sprints 40–44

### Sprint 40 — MVVM and shell UX `[SEQUENTIAL]`

| Task | Owner | Key paths |
|------|-------|-----------|
| Command palette global keyboard shortcut | AGENT | `ShellPage.xaml`, `CommandPaletteView.xaml.cs` |
| Register streamer hotkeys (not copy-only) | AGENT | `FutureServices.cs`, `StreamFriendlyProfileService.cs` |
| GameLibrary toolbar → ViewModel commands | AGENT | `GameLibraryView.xaml.cs`, `GameLibraryViewModel.cs` |
| ViewModels for About, Global3D, Troubleshooting, LibrarySettings, ToolchainHealth | AGENT | `Views/*`, `ViewModels/*` |
| Setup wizard `x:Bind` / bindings | AGENT | `SetupWizardView.xaml`, `SetupWizardViewModel.cs` |
| Glossary dynamic content from seed/docs | AGENT | `GlossaryView.xaml` |

**Exit criteria:** Palette reachable via keyboard; library toolbar uses VM commands.

> **Sprint 37 partial carryover:** streamer hotkeys and palette shortcut ship here (nav/actions exist; hotkeys deferred).

### Sprint 41 — Launch depth and honesty `[SEQUENTIAL]`

| Task | Owner | Key paths |
|------|-------|-----------|
| Replace PlayIn3D fake progress loop with real readiness/config steps | AGENT | `UseCases.cs` — wire `_readiness`, `_configWriter` |
| Wire `MultiMonitorLaunchPicker` into launch path | AGENT | `MultiMonitorLaunchPicker.cs`, `Infrastructure/Launch/*` |
| OpenXR handoff beyond SteamVR delegate stub | AGENT | `PcvrServices.cs` |
| Full PlayIn3D rollback integration test | AGENT | `LaunchIntegrationTests.cs`, `QaMatrixAutomationTests.cs` |
| PCVR OpenXR override tests | AGENT | `PcvrOpenXrTests.cs` |

**Exit criteria:** Progress overlay reflects real operations; P0 rollback exercises launch path, not snapshot-only.

### Sprint 42 — v2 productization `[SEQUENTIAL]` (`FeatureFlags.V2Enabled`)

| Task | Owner | Key paths |
|------|-------|-----------|
| Epic/GOG install/launch metadata (ADR-0004 scope) | AGENT | `EpicGogLibraryScanner.cs`, `LibraryServices.cs` |
| Surface workshop/LAN/hybrid outside Troubleshooting | AGENT | Library or shell nav |
| README v2 accuracy (toggle vs stubs) | AGENT | `README.md` |
| Expand V2_MAP for LAN export | AGENT | `check-qa-matrix-coverage.sh` |

### Sprint 43 — QA gate hardening `[SEQUENTIAL]`

| Task | Owner | Key paths |
|------|-------|-----------|
| Gate QA_MATRIX P1 offline/Steam scenarios | AGENT | `docs/QA_MATRIX.md`, `check-qa-matrix-coverage.sh` |
| UI smoke tests (About update, palette, filters) | AGENT | `SpatialLabsOptimizer.Tests` |
| Incremental Steam scan true delta | AGENT | `FutureServices.cs` |
| HDR watchdog honesty or explicit OS handoff | AGENT | `HdrWatchdogService` |
| Real WinUI README screenshots | AGENT | `docs/assets/readme/`, `generate-brand-assets.py` |

### Sprint 44 — Modularization `[PARALLEL]`

Split files exceeding AGENTS.md limits: `FutureServices.cs`, `DiagnosticsServices.cs`, `GameLibraryViewModel.cs`, `UseCases.cs`, `UserPreferencesService.cs`, `GameDatabase.cs`, `Global3DSettingsView.xaml.cs`, `LaunchServices.cs`. Move `CommandPaletteService` out of `PcvrServices.cs`.

---

## Feature backlog index

| # | Feature | Sprint |
|---|---------|--------|
| 33 | Local game folder watch list | 35 |
| 34 | Local signed zip + MSIX + MSI build | 32 |
| 35 | WiX MSI installer | 32 |
| 36 | About update checker + Venmo donate | 36 |
| 37 | PlayIn3D real progress / readiness wiring | 41 |
| 38 | Multi-monitor launch in launch path | 41 |
| 39 | Command palette hotkey | 40 |
| 40 | Update apply tests + retry banner | 39 |
| 41 | MVVM consolidation | 40 |
| 42 | Real README screenshots | 43 |
| 43 | File size budget compliance | 44 |
| 1–32 | Prior recommendations | 28–38 (see [COMPLETED_TASKS.md](COMPLETED_TASKS.md)) |

---

## Completed roadmap

Sprints 28–38 archived in [COMPLETED_TASKS.md](COMPLETED_TASKS.md).

| Sprint | Theme | Archive |
|--------|-------|---------|
| 28–31 | v1.0.1 closure | [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
| 32 | Distribution + local build/sign (zip, MSIX, MSI) | [Sprint 32](COMPLETED_TASKS.md#sprint-32--distribution--local-release-archived-2026-06-14) |
| 33 | Trainer/mod-manager coexistence | [Sprint 33](COMPLETED_TASKS.md#sprint-33--trainer--mod-manager-coexistence-archived-2026-06-14) |
| 34 | Hardware honesty & trust UX | [Sprint 34](COMPLETED_TASKS.md#sprint-34--hardware-honesty--trust-ux-archived-2026-06-14) |
| 35 | Library intelligence & local installs | [Sprint 35](COMPLETED_TASKS.md#sprint-35--library-intelligence--local-installs-archived-2026-06-14) |
| 36 | v2 toggle, About updates, launch depth | [Sprint 36](COMPLETED_TASKS.md#sprint-36--launch-depth--v2-productization-archived-2026-06-14) |
| 37 | Display, PCVR & command palette | [Sprint 37](COMPLETED_TASKS.md#sprint-37--display-pcvr--power-ux-archived-2026-06-14) |
| 38 | Diagnostics & onboarding polish | [Sprint 38](COMPLETED_TASKS.md#sprint-38--community-diagnostics--onboarding-polish-archived-2026-06-14) |

---

## Documentation sync (each sprint boundary)

- Archive completed sprint → [COMPLETED_TASKS.md](COMPLETED_TASKS.md)
- Update [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md)
- Extend [docs/QA_MATRIX.md](docs/QA_MATRIX.md) when new scenarios land
- Run: `dotnet test`, `run-post-sprint-validation.ps1`, `check-file-encoding.py`
- Append [DECISION_LOG.md](DECISION_LOG.md) at Sprint 39 milestone (single PR ship)

---

## Release sign-off (automated)

| Check | Enforcer |
|-------|----------|
| CI + tests | `ci.yml` WinUI job |
| Test count | AUTO from CI job output (not hand-counted) |
| QA matrix P0–v1.5 | `scripts/check-qa-matrix-coverage.sh` |
| QA matrix P1 offline/Steam | `check-qa-matrix-coverage.sh` (Sprint 43) |
| Compatibility seed | `scripts/check-compatibility-seed.sh` |
| Legal consistency | `scripts/check-legal-consistency.sh` |
| Local release scripts | `scripts/check-local-release-scripts.sh` |
| Post-sprint validation | `scripts/run-post-sprint-validation.ps1` / `.sh` |
| Optional next steps | `scripts/automate-optional-next-steps.ps1` |
| README UI previews | `readme-assets.yml`, `generate-brand-assets.py` |
| Release credentials posture | `scripts/check-release-credentials.sh` |
| Product pre-release | `product-release.yml` → `pre-release-gate.sh --product-release` |
| Product zip + MSI + MSIX + signing | `product-release.yml`, `build-product-local.ps1` |
| Template release | `release.yml` on `v*` matching `.template-version` |
| Quarterly maintenance | `quarterly-maintenance.yml` |

---

## Ongoing maintenance (automated)

**Weekly:** `health-check.yml`, `scorecard.yml` · **On PR:** `ci.yml` · **Monthly:** `license-audit.yml`, `template-upgrade-simulation.yml` · **Quarterly:** `quarterly-maintenance.yml` + credential audit

One-time per repo:

```bash
bash scripts/setup-release-credentials.sh owner/repo
bash scripts/check-release-credentials.sh owner/repo
```

Optional: GitHub Actions **Release Credentials Setup** workflow_dispatch.
