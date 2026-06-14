# Build Plan

> Prioritized task board with owner labels, Sequential and Parallel lanes per sprint.
> Move completed items to `COMPLETED_TASKS.md`.

## Owner Label Legend

| Label | Owner | When to use |
|-------|-------|-------------|
| `AGENT` | Cursor Agent | Code, docs, scaffolding, tests, CI config |
| `HUMAN` | Human developer | Approvals, credentials, GitHub settings, product decisions |
| `ADB` | Human (Android) | Android SDK, emulator/device testing, F-Droid submission |
| `AUTO` | CI/scripts/bots | GitHub Actions, Dependabot, pre-commit, update checker |
**Task format:** `- [ ] [OWNER] Description`

**Filter by label:**

```bash
grep '\[AGENT\]' BUILD_PLAN.md
grep '\[HUMAN\]' BUILD_PLAN.md
grep '\[ADB\]' BUILD_PLAN.md
grep '\[AUTO\]' BUILD_PLAN.md

```

**Agent rule:** Execute all `[AGENT]` Sequential items first, then dispatch Parallel agents with isolated file scopes. Shared schema/types are Sequential-only.

> **Sprint M0–M5** are template-maintainer sprints (this repo). Child repos created from the template use Sprint 0–2 only.

---

## Child Repo — Sprint 0–2 (not applicable to template maintainer)

> Run these after **Use this template** to create a child project. See `docs/INITIALIZATION_PROMPT.md`.

### Sprint 0 — Template Customization

1. [ ] [HUMAN] Create/publish `edwardlthompson/3d-game-optimizer` repo on GitHub (cloned from bootstrap)
2. [x] [AGENT] Fill placeholders in `docs/INITIALIZATION_PROMPT.md` (WinUI 3 + .NET 8, purpose)
3. [x] [AGENT] Run `init-stack-sync` / project customization (init-project.ps1 partial — manual completion)
4. [ ] [HUMAN] Run `scripts/setup-github-repo.ps1` for Dependabot alerts, private vulnerability reporting, and branch protection
5. [x] [AGENT] Stack: WinUI desktop hub (`modules/winui`); template examples retained
6. [ ] [HUMAN] Approve Sprint 0 only after CI green on `main` and Build Verification Gate passes

### Sprint 1 — Golden Path Foundation

1. [ ] [HUMAN] Approve ADR-0001 (`docs/adr/0001-core-architecture.md`)

### Sprint 2 — First Real Feature

1. [x] [AGENT] Define first feature milestone: v1.0 3D Game Optimizer hub (see product sprints below)
2. [x] [AGENT] Implementation plan in `spatiallabs_3d_optimizer_fa3dbeb5.plan.md`
3. [ ] [HUMAN] Sign off for v1.0 release (Sprint 8 gate)

---

## 3d-game-optimizer — Product Sprints

> PC 3D display setup hub. Policies: **zero-friction launch**, **discovery-first library**, **silent installs**, **verbose progress**, **zero data sharing**.
> Prerequisite: complete Sprint 0–2 above.

### Sprint 3 — WinUI Foundation

#### Sequential (must complete in order)

1. [x] [AGENT] Scaffold `src/SpatialLabsOptimizer` WinUI 3 solution + `src/SpatialLabsOptimizer.ElevatedHelper`
2. [x] [AGENT] Add `modules/winui/MODULE.md` and `examples/winui` Golden Path stub
3. [x] [AGENT] Draft ADR-0001 (MVVM + Clean + silent install + zero-data-sharing + legal disclaimer policy)
4. [ ] [HUMAN] Approve ADR-0001
5. [x] [AGENT] Implement DI host, Serilog local logging, SQLite settings store (scaffold)
6. [x] [AGENT] Implement `OperationProgressHub` + `IProgressReportingOperation` contract
7. [x] [AGENT] Build shell `OperationProgressDialog`, `LaunchProgressOverlay`, `ShellActivityInfoBar`, status bar chip
8. [x] [AGENT] Create `DesignSystem.xaml`, `ControlStyles.xaml`, `AppButton`, `AppIconButton`, `ResponsiveStateService`
9. [x] [AGENT] Scaffold `docs/DESIGN_SYSTEM.md`, `docs/LEGAL.md`, `docs/TRADEMARKS.md`, `docs/UX_PROGRESS.md`, `docs/README_STYLE.md`
10. [x] [AGENT] Write `README.md` — HTML hero, shields badges, quick start, collapsible `<details>` sections
11. [x] [AGENT] Add `PrivacyGuard` HTTP allowlist wrapper
12. [x] [AGENT] Replace CI web/python/android jobs with `winui` + `privacy-audit` jobs
13. [ ] [HUMAN] Approve Sprint 3 after CI, Security Scan, and CodeQL green on `main`

#### Parallel (safe after Sequential step 7)

| Task | Owner | Isolated scope |
|------|-------|----------------|
| WinUI navigation shell | AGENT | `src/SpatialLabsOptimizer/Views/**`, `ViewModels/Shell*` |
| Design system + base controls | AGENT | `Resources/DesignSystem.xaml`, `Controls/AppButton*` |
| README scaffold | AGENT | `README.md`, `docs/README_STYLE.md` |
| Domain entities | AGENT | `src/SpatialLabsOptimizer/Domain/**` |
| Elevated helper scaffold | AGENT | `src/SpatialLabsOptimizer.ElevatedHelper/**` |

### Sprint 4 — Display Catalog, Data Layer & Defaults

> Services implemented — hardware validation and GitHub CI push remain human gates.

1. [x] [AGENT] Create `data/displays/display-catalog-v1.json`
2. [x] [AGENT] Create `data/compatibility/schema.json` and `seed-v1.json`
3. [x] [AGENT] Create `data/presets/preset-manifest-v1.json`
4. [x] [AGENT] Create `data/defaults/optimal-displays-v1.json`
5. [x] [AGENT] Create `data/performance/performance-tiers-v1.json`
6. [x] [AGENT] Create `data/tools/tool-manifest-v1.json`
7. [x] [AGENT] Implement `DisplayAutoDetector` and vendor adapters
8. [x] [AGENT] Implement `CompatibilityRepository`, `ExternalDataGateway`, Steam clients, `GameArtworkService`
9. [x] [AGENT] Write `docs/DISPLAY_VENDORS.md`, `docs/STEAM_INTEGRATION.md`, `docs/PRIVACY.md`, `docs/TOOL_AUTOMATION.md`, `docs/SEED_MAINTENANCE.md`

> Sprints 5–8 implemented (agent). Sprints 9–14 scaffolded. Human gates: GitHub push, ADR approval, hardware QA.

### Sprint 5 — Silent Setup Wizard

1. [x] [AGENT] `SilentInstallOrchestrator` + elevated helper audit log
2. [x] [AGENT] Legal consent step + `ManualDisplayPickerView`
3. [x] [AGENT] Optional benchmark step in wizard
4. [x] [AGENT] Dual-GPU / MUX warning (`MuxGpuDetector`)
5. [x] [AGENT] Viewing distance coach per display profile
6. [ ] [HUMAN] Validate silent setup on reference hardware

### Sprint 6 — Discovery Library

1. [x] [AGENT] Library indexer + SQLite materialized views
2. [x] [AGENT] `GameLibraryView` with box-cover grid + warm start chip
3. [x] [AGENT] Wilson review sort via `LibrarySortService`
4. [x] [AGENT] Responsive grid columns via `ResponsiveStateService`
5. [ ] [HUMAN] UX review at 700+ fixture scale

### Sprint 7 — Zero-Friction Launch

1. [x] [AGENT] `PlayIn3D` with 8-step `LaunchProgressOverlay`
2. [x] [AGENT] `TrainerCoexistenceService` (WeMod/Wand detection)
3. [x] [AGENT] `LaunchAuditService` + `launch-audit.log`
4. [x] [AGENT] Auto-fallback ReShade → UEVR + config rollback
5. [ ] [HUMAN] Manual QA on reference titles

### Sprint 8 — Polish & Ship

1. [x] [AGENT] Hardware & Benchmark panel in settings
2. [x] [AGENT] Trainer + Safe launch + Simple mode toggles
3. [x] [AGENT] `scripts/logo-audit.sh` in CI
4. [x] [AGENT] Winget manifest (`packaging/winget/`)
5. [ ] [HUMAN] Legal copy review + v1.0 release tag

### Sprint 9–14 — Post-v1.0 Roadmap (scaffolded)

1. [x] [AGENT] ADR-0002 PCVR connector (`docs/adr/0002-pcvr-connector.md`)
2. [x] [AGENT] `PcvrRuntimeConnector` + `PlayInVR`
3. [x] [AGENT] v1.0.1: `IncrementalSteamScanService`, `HdrWatchdogService`
4. [x] [AGENT] v1.1: `PlayQueueService`, `SessionProfileService`, `CommandPaletteService`
5. [x] [AGENT] v2+: `SteamGridDbClient`, `LanPartyExportService`, `StreamerHotkeyService`, `HybridSessionService`
6. [ ] [HUMAN] Approve ADR-0002 and post-v1.0 release tags per sprint

---

## Ongoing Maintenance (template + child repos)

### Weekly (recurring)

- [ ] [HUMAN] Run weekly CVE triage pass per `docs/SECURITY_TRIAGE.md` (recommended: Monday)
- [ ] [AGENT] Apply Dependabot dependency bumps and open PRs as needed
- [ ] [AUTO] Trivy + CodeQL + CI matrix green after merges
- [ ] [AUTO] OpenSSF Scorecard workflow result reviewed
- [ ] [AUTO] `scripts/pre-release-gate.sh` run before any version bump
- [ ] [AGENT] Triage Scorecard findings into BUILD_PLAN `[AUTO]` items

### Monthly (recurring)

- [ ] [AUTO] Run `scripts/simulate-template-upgrade.sh` on schedule (first Monday)
- [ ] [HUMAN] Review `THIRD_PARTY_LICENSES.md` and SBOM for distribution
- [ ] [AGENT] Dependabot auto-merge PRs reviewed for override/transitive policy (KB-007)

---

## Template Maintainer — Sprint M5: README Visual Refresh

> AGENT work complete. Pending human visual review after push.

1. [ ] [HUMAN] Visual review on GitHub — badges load, `<dl>`/tables render as single blocks, all relative links resolve

---

## Template Maintainer — Release Approvals

> Sequential gates before tagging. Automation handles CI and pre-release checks.

| Release | Status | Remaining |
|---------|--------|-----------|
| v0.2.2 | Ready | [ ] [HUMAN] Approve v0.2.2 release tag and GitHub Release |
| v0.3.0 | Ready | [ ] [HUMAN] Approve v0.3.0 release |
| v0.4.0 | Ready | [ ] [HUMAN] Approve v0.4.0 release |
| v0.5.0 | Ready | [ ] [HUMAN] Approve v0.5.0 release scope |
| v0.6.0 | Ready | [ ] [HUMAN] Approve v0.6.0 release |
| v0.7.0 | Open PR | [ ] [HUMAN] Review and merge Release Please PR #7 |
---

## Milestone Gates — Release Sign-off (every version)

- [ ] [HUMAN] Weekly CVE triage completed within last 7 days
- [ ] [HUMAN] Zero open Critical/High Dependabot alerts (or documented exception)
- [ ] [HUMAN] State persistence survives simulated upgrade
- [ ] [HUMAN] CHANGELOG.md updated (Keep a Changelog format)
- [ ] [HUMAN] Version bumped and GitHub Release drafted
- [ ] [AUTO] SBOM attached to GitHub Release
- [ ] [HUMAN] `THIRD_PARTY_LICENSES.md` reviewed for distribution
- [ ] [HUMAN] Conventional Commits enforced on merged PRs

---

## Template Maintainer — Device / Distribution (ADB)

- [ ] [ADB] Run F-Droid submission dry-run checklist (`modules/android/MODULE.md`) on physical device or emulator before first F-Droid release
- [ ] [ADB] Complete F-Droid `metadata/` blocks and verify reproducible APK hashes locally
