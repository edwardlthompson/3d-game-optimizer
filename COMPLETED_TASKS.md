# Completed Tasks

> Archive of finished BUILD_PLAN items.

## Sprint 38 — Community, diagnostics & onboarding polish (archived 2026-06-14)

- [x] [AGENT] `LaunchDryRunService` + Troubleshooting simulate button
- [x] [AGENT] Enhanced `DiagnosticBundleService` (audit, display, toolchain, coexistence, update settings)
- [x] [AGENT] `LanPresetExportService`, `SeedContributionExportService`
- [x] [AGENT] `ReadinessScoreService` + offline onboarding in setup wizard
- [x] [AGENT] `3dgo://play/{appid}` protocol + `scripts/register-3dgo-protocol.ps1`
- [x] [AUTO] `DiagnosticsTests` (7 tests)

> **Partial carryover (Sprint 43):** real WinUI README screenshots deferred — synthetic placeholders from `generate-brand-assets.py` remain until Sprint 43.

## Sprint 37 — Display, PCVR & power UX (archived 2026-06-14)

- [x] [AGENT] `DisplayChangeMonitor` EDID hot-plug + setup re-prompt
- [x] [AGENT] `ViewingDistanceCoachView` interactive coach
- [x] [AGENT] Multi-monitor launch picker, OpenXR runtime override, stream-friendly hotkeys
- [x] [AGENT] Unified Play in 3D / Play in VR library actions
- [x] [AGENT] Command palette real actions (cache, rescan, logs, safe launch)
- [x] [AUTO] `DisplayPcvrUxTests` (9 tests)

> **Partial carryover (Sprint 40):** streamer hotkey registration and command palette global keyboard shortcut deferred — nav/actions shipped in Sprint 37.

## Sprint 36 — Launch depth & v2 productization (archived 2026-06-14)

- [x] [AGENT] v2 experimental settings toggle + DI registration
- [x] [AGENT] Per-game overrides, session profiles, HDR notice, config snapshot UI
- [x] [AGENT] Full `UpdateService` + download/apply/restart pipeline; About intervals + Venmo
- [x] [AGENT] `ElevatedHelper apply-update` for zip/msi
- [x] [AGENT] Product version 1.1.0
- [x] [AUTO] `UpdateServiceTests`, `InstallArtifactDetectorTests`, `UpdateSchedulerTests`

## Sprint 35 — Library intelligence & local installs (archived 2026-06-14)

- [x] [AGENT] `LocalGameFolderRepository`, `LocalFolderGameScanner`, `MergeLocalGamesAsync`
- [x] [AGENT] `LocalGameInstallResolver` direct exe launch
- [x] [AGENT] Library badges, Why not ready filter, recent launches, notes, collections, preset freshness
- [x] [AGENT] `LibrarySettingsView` + `docs/LOCAL_GAME_FOLDERS.md`
- [x] [AUTO] `LocalFolderGameTests`, `LibraryIntelligenceTests`; `V14_MAP` + `V15_MAP`

## Sprint 34 — Hardware honesty & trust UX (archived 2026-06-14)

- [x] [AGENT] WMI `SystemSpecsScanner`, deterministic `BenchmarkService`, `MuxGpuDetector`
- [x] [AGENT] `LaunchPreviewService` pre-launch summary
- [x] [AGENT] `GlossaryView` in shell navigation
- [x] [AGENT] `docs/UX_PROGRESS.md` milestones updated
- [x] [AUTO] `HardwareTrustTests`

## Sprint 33 — Trainer & mod-manager coexistence (archived 2026-06-14)

- [x] [AGENT] `ExternalToolCoexistenceService`, `GameFirstLaunchOrchestrator`, `LaunchContext`
- [x] [AGENT] `3DGO-0004` when coexistence off; game-first UEVR skip
- [x] [AGENT] Toolchain Health mod-manager toggle + detected tools display
- [x] [AGENT] `docs/TRAINER_COEXISTENCE.md`
- [x] [AUTO] `CoexistenceLaunchTests`; `V13_MAP`

## Sprint 32 — Distribution & local release (archived 2026-06-14)

- [x] [HUMAN] WinGet PR [#387878](https://github.com/microsoft/winget-pkgs/pull/387878) submitted (merge pending CLA)
- [x] [AGENT] `build-product-local.ps1` orchestrator (zip + MSIX + WiX MSI)
- [x] [AGENT] `codesign-common.ps1`, `verify-product-signatures.ps1`, `packaging/msi/`
- [x] [AGENT] `docs/LOCAL_RELEASE.md`, ADR-0003 MSI distribution
- [x] [AUTO] `check-local-release-scripts.sh`, `product-release.yml` MSI attach

## Sprint 31 — PCVR depth & distribution polish (archived 2026-06-14)

- [x] [AGENT] `OpenXrRuntimeProbe` registry + runtime.json active runtime detection
- [x] [AGENT] Session profile save/list UI + streamer hotkey display in settings
- [x] [AGENT] Sprint 31 UI preview screenshots via `generate-brand-assets.py`
- [x] [AGENT] Winget submission checklist in `packaging/winget/README.md`
- [x] [HUMAN] Submit manifest PR to `microsoft/winget-pkgs` (post-release) — see DECISION_LOG 2026-06-14 Winget entry

## Sprint 30 — v2.0 production quality (archived 2026-06-14)

- [x] [AGENT] `EpicGogLibraryScanner` parses Epic `.item` + GOG `goggame-*.info` manifests
- [x] [AGENT] `WorkshopPresetImporter` + `workshop-sources-v1.json` with PrivacyGuard allowlist
- [x] [AGENT] Troubleshooting UI: library checkboxes for LAN export + hybrid co-op session
- [x] [AGENT] `HybridSessionService` persists session code; structured LAN export JSON
- [x] [AUTO] `V2IntegrationTests` (8 tests) + `V2_MAP` QA coverage

## Sprint 29 — library UX completion (archived 2026-06-14)

- [x] [AGENT] Favorites toggle/filter UI; `SetFavoriteAsync` preserves across re-index
- [x] [AGENT] Pin unpin, play-next queue, playlist save/load UI
- [x] [AGENT] `SteamGridDbClient` artwork fallback in `GameArtworkService`
- [x] [AUTO] `LibraryUxTests` + `V12_MAP` in `check-qa-matrix-coverage.sh`

## Sprint 28 — data & manifest alignment (archived 2026-06-14)

- [x] [AGENT] Migrated `preset-manifest-v1.json` to `UevrProfiles` + preserved `displayPresets`
- [x] [AGENT] Expanded `seed-v1.json` to 12 titles with `vrCapability` / `steamVrLaunchOptions`
- [x] [AGENT] `PlayInVR` + `PcvrRuntimeConnector` consume seed launch options; empty preset URL guard
- [x] [AUTO] `check-compatibility-seed.sh` wired in `ci.yml` + `pre-release-gate.sh`
- [x] [AUTO] 46 tests pass; bulk preset cache exercises manifest profiles

## Post-v1 Refresh — Sprints 18–23 (archived 2026-06-14)

### Sprint 18 — v1.0 closure automation

- [x] [AUTO] `check-qa-matrix-coverage.sh`, `QaMatrixAutomationTests`, `AccessibilitySmokeTests`
- [x] [AUTO] `product-release.yml` pre-release gate; optional Authenticode signing
- [x] [AUTO] `quarterly-maintenance.yml`, `check-quarterly-maintenance.sh`, `check-release-please-pr.sh`
- [x] [AGENT] ADR-0003 release policy; `docs/HARDWARE_QA_OUT_OF_BAND.md`
- [x] [AUTO] Extended `verify-github-settings.sh` (code scanning WARN, RELEASE_BOT_TOKEN WARN)

### Sprint 19 — v1.0.1

- [x] [AGENT] `IncrementalSteamScanService` wired to library load; real `HdrWatchdogService`
- [x] [AGENT] Bulk preset cache UI; `FeatureFlags.V101Enabled`; product version 1.0.1

### Sprint 20 — v1.1

- [x] [AGENT] `PcvrRuntimeConnector` OpenXR probe; `PlayInVR` SteamVR launch
- [x] [AGENT] `GameOverrideRepository` + `PreferredOutput`; `LibrarySortMode.Genre`
- [x] [AGENT] `CommandPaletteView`; PCVR + milestone tests

### Sprint 21 — v1.x polish

- [x] [AGENT] Update/diagnostic/safe-launch/pinned shelf/queue/command palette/bulk preset
- [x] [AGENT] MSIX PNG assets (`StoreLogo.png`, `Square44x44Logo.png`)

### Sprint 22 — v2.0

- [x] [AGENT] ADR-0004; `EpicGogLibraryScanner`, `WorkshopPresetImporter`, v2 feature flag

### Sprint 23 — release hardening

- [x] [AGENT] Winget SHA256 in CI; README screenshots; QA matrix v2 exit criteria

## Plan parity closure — Sprints 24–27 (archived 2026-06-14)

### Sprint 24 — automation parity

- [x] [AGENT] CodeQL upload-sarif; AccessibilitySmokeTests → Core `SetupWizardFlow` / `AccessibilityIds`
- [x] [AGENT] QA matrix v1.1 coverage map; stale Release Please PR fail; RUNBOOK `CODESIGN_*`; TEMPLATE_INDEX scripts

### Sprint 25 — v1.0.1 / v1.1 feature parity

- [x] [AGENT] HDR registry disable; bulk cache progress test; PreferredOutput in resolve + library UI
- [x] [AGENT] Play in VR UI; command palette nav + execution; Genre sort; vrCapability seeds; PCVR tests

### Sprint 26 — v1.x polish UI

- [x] [AGENT] Update check in About; safe-launch preference in PlayIn3D; pin/queue UI; settings prefs

### Sprint 27 — v2 integration

- [x] [AGENT] Epic/GOG library merge; workshop/LAN troubleshooting UI; SteamGridDb JSON parse; v2 tests

## Product v1.0 Ship Track — Sprints 15–17 (archived 2026-06-14)

- [x] [AGENT] Real launch/install, product publish pipeline, Release Please split, CodeQL C#
- [x] [AGENT] Sprint 16 hygiene; Sprint 17 MSIX scaffold, ViewModel splits, feature flags

## Human Task Automation (2026-06-14)

- [x] [AUTO] Gate scripts: `build-verification-gate`, `sprint-signoff-gate`, `check-readme-health`, `check-adr-status`, `check-legal-consistency`, `check-security-triage`, `verify-github-settings`; extended `pre-release-gate`, `setup-github-repo`
- [x] [AUTO] CI smoke tests: `QaSmokeTests.cs` (silent install, 700+ library perf, P0 launch, schema migration)
- [x] [AUTO] Workflows: `template-upgrade-simulation.yml`, `security-triage.yml`, `license-audit.yml`, `release-auto-merge.yml`; extended `ci.yml`, `health-check.yml`
- [x] [AUTO] Release harmonization: Release Please creates release; `release.yml` attaches SBOM on `release: published`
- [x] [AUTO] BUILD_PLAN human gates relabeled; ADR-0002 accepted

## Child Repo — Sprint 0–2 (archived 2026-06-14)

- [x] [AUTO] Create/publish `edwardlthompson/3d-game-optimizer` repo (`scripts/verify-github-settings.sh`)
- [x] [AGENT] Fill placeholders in `docs/INITIALIZATION_PROMPT.md`
- [x] [AGENT] Run `init-stack-sync` / project customization
- [x] [AUTO] Run `scripts/setup-github-repo.ps1`
- [x] [AGENT] Stack: WinUI desktop hub (`modules/winui`)
- [x] [AUTO] Sprint 0 sign-off (`scripts/sprint-signoff-gate.sh`)
- [x] [AUTO] Approve ADR-0001 (`scripts/check-adr-status.sh`)
- [x] [AGENT] Define v1.0 feature milestone + implementation plan
- [x] [AUTO] v1.0 release sign-off (`pre-release-gate.sh` + `release-auto-merge.yml`)

## 3d-game-optimizer — Product Sprints 3–14 (archived 2026-06-14)

> **Note (2026-06-14):** Sprints 5–8 and v1.0 sign-off items marked complete refer to **scaffolding, gate automation, and AUTO smoke tests**. Product feature completeness is tracked in BUILD_PLAN Sprint 15+.

### Sprint 3 — WinUI Foundation

- [x] [AGENT] Scaffold WinUI solution + ElevatedHelper, DI, progress hub, design system, docs, README, PrivacyGuard, CI jobs
- [x] [AUTO] ADR-0001 + Sprint 3 sign-off

### Sprint 4 — Display Catalog & Data Layer

- [x] [AGENT] Seed data (`data/displays`, compatibility, presets, defaults, performance, tools)
- [x] [AGENT] `DisplayAutoDetector`, `CompatibilityRepository`, Steam/artwork services, integration docs

### Sprint 5 — Silent Setup Wizard

- [x] [AGENT] `SilentInstallOrchestrator`, legal consent, benchmark, MUX warning, viewing distance coach
- [x] [AUTO] `QaSmokeTests.SilentInstallOrchestrator_RecordsExpectedAuditSteps`

### Sprint 6 — Discovery Library

- [x] [AGENT] Library indexer, `GameLibraryView`, Wilson sort, responsive grid
- [x] [AUTO] `QaSmokeTests.LibrarySortService_Sorts700Games_Under200ms`

### Sprint 7 — Zero-Friction Launch

- [x] [AGENT] `PlayIn3D`, trainer coexistence, launch audit, ReShade→UEVR fallback
- [x] [AUTO] `QaSmokeTests.LaunchPipeline_P0_*`

### Sprint 8 — Polish & Ship

- [x] [AGENT] Settings panels, toggles, logo-audit CI, Winget manifest
- [x] [AUTO] Legal consistency + v1.0 release automation

### Sprint 9–14 — Post-v1.0 Roadmap

- [x] [AGENT] ADR-0002, PCVR connector, v1.0.1–v2+ services scaffolded
- [x] [AUTO] ADR-0002 accepted; post-v1.0 tags via `release-auto-merge.yml`

## Template Maintainer — Release Approvals (archived 2026-06-14)

| Release | Gate |
|---------|------|
| v0.2.2–v0.7.0 | [x] [AUTO] `release-auto-merge.yml` + `pre-release-gate.sh` |

## Milestone Gates — Release Sign-off (archived 2026-06-14)

- [x] [AUTO] Weekly CVE triage, Dependabot alerts, state persistence, CHANGELOG, version bump, SBOM, licenses, Conventional Commits

## Ongoing Maintenance (archived 2026-06-14)

- [x] [AUTO] Weekly CVE triage — `security-triage.yml`
- [x] [AGENT] Dependabot ecosystems: github-actions, nuget, npm (`examples/web`), pip (`examples/python`)
- [x] [AUTO] Trivy + CodeQL + CI — CodeQL `upload: false` until code scanning enabled; workflow permissions for Release Please
- [x] [AUTO] Scorecard — `check-scorecard-recency.sh` + `health-check.yml`
- [x] [AUTO] Pre-release gate — `pre-release-gate.sh` + `release-auto-merge.yml`
- [x] [AGENT] Scorecard triage — no open findings blocking release; SARIF upload best-effort
- [x] [AGENT] KB-007 review — `check-kb007-policy.sh` + `DECISION_LOG.md` 2026-06-14 entry
- [x] N/A [ADB] F-Droid tasks — WinUI-only product; Android stack inactive

## Sprint M5 — README Visual Refresh (2026-06-12)

- [x] [AGENT] Harden `scripts/normalize-markdown-whitespace.py` — table-aware blank-line collapse
- [x] [AGENT] Add `scripts/check-markdown-tables.sh`; hook into `validate-bootstrap.sh`
- [x] [AGENT] Redesign README sections — shields.io badges + HTML `<dl>`/tables for What's Included, BUILD_PLAN Labels, Template Update Checker, Supported Stacks
- [x] [AGENT] Add README badge conventions to `docs/MAINTAINING_THE_TEMPLATE.md`
- [x] [AGENT] Run verification — encoding, design cohesion, markdown table lint, TEMPLATE_INDEX validation
- [x] [AUTO] Visual review via `scripts/check-readme-health.sh` — badges, tables, relative links

## Template Maintainer — v0.2.1 Full Bootstrap Hardening (2026-06-13)

- [x] [AGENT] Normalize `.gitignore` UTF-16 to UTF-8; extend encoding scan and pre-commit hook
- [x] [AGENT] Sync `PROMPT_LIBRARY.md` entries 4, 6, 8, 9; populate `KNOWLEDGE_BASE.md` (6 entries)
- [x] [AGENT] Document Lighthouse 3-run median in `modules/web/MODULE.md`
- [x] [AGENT] SHA-pin `release.yml` actions; add pin policy to `docs/SECURITY_TRIAGE.md`
- [x] [AGENT] Add `check-workflow-action-ref-format.sh` pre-commit hook
- [x] [AGENT] Init scripts: `validate-workflow-actions` + `check-github-ci` reminder
- [x] [AGENT] Devcontainer: encoding check, gh CLI feature, CI gate tip
- [x] [AGENT] Add `health-check.yml` weekly workflow
- [x] [AGENT] Bootstrap Gradle wrapper; CI `android-build` assembleDebug job
- [x] [AGENT] Bump to v0.2.1; sync `TEMPLATE_INDEX.json`, `CHANGELOG.md`, `README.md`
- [x] [HUMAN] Set GitHub About from `docs/GITHUB_ABOUT.md` (via `gh repo edit`)
- [x] [HUMAN] Create GitHub Release tag `v0.2.1` (https://github.com/edwardlthompson/agent-project-bootstrap/releases/tag/v0.2.1)
- [x] [HUMAN] GitHub settings: Dependabot alerts, private vulnerability reporting, branch protection (CI + Security Scan + CodeQL)
- [x] [HUMAN] Replace `@[PROJECT_OWNER]` in CODEOWNERS with `@edwardlthompson` (template maintainer)

## Template Maintainer — v0.2.0 Backlog Fix (2026-06-12)

- [x] [AGENT] Normalize UTF-16 files to UTF-8; add `scripts/check-file-encoding.sh` + CI + pre-commit
- [x] [AGENT] Add `package-lock.json`, `uv.lock`, `.env.example`; expand `validate-bootstrap.sh`
- [x] [AGENT] Sync `TEMPLATE_INDEX.json` with LICENSE, scripts, workflows, rules
- [x] [AGENT] Sync README, SECURITY_TRIAGE, RUNBOOK, UPGRADING_FROM_TEMPLATE, PROMPT_LIBRARY, CHANGELOG
- [x] [AGENT] Harden license-compliance CI; web coverage budget; android ops checklist
- [x] [AGENT] Harden INITIALIZATION_PROMPT Sections 2/7/8 with Build Verification Gate
- [x] [AGENT] Update BUILD_PLAN Sprint 0 + Milestone Gates
- [x] [AGENT] Bump `.template-version` to 0.2.0; finalize CHANGELOG
- [x] [HUMAN] GitHub settings: Dependabot alerts, private vulnerability reporting, branch protection, About
- [x] [HUMAN] Replace `@[PROJECT_OWNER]` in CODEOWNERS with `@edwardlthompson`

## Template Maintainer — v0.6.0+ Web Layout & CI Fixes (2026-06-13)

- [x] [AGENT] Add `docs/WEB_PROJECT_LAYOUT.md` and agent routing for docs/ vs examples/web/
- [x] [AGENT] Localization scaffold docs (web `locales/` + Android `strings.xml`) separated from styles
- [x] [AGENT] Android `NetworkStatusMonitor` for online/offline status parity with web
- [x] [AGENT] Harden `check-design-cohesion` (CSS content guard, main.ts i18n, PS1 parity)
- [x] [AUTO] CI, Security Scan, CodeQL, and GitHub Pages green on `main` (commit `38ce003`)
- [x] [HUMAN] Enable GitHub Pages (Actions source) and workflow PR permissions via repo settings

## Sprint M0 — Template Hardening v0.2.2

- [x] [AGENT] Add `scripts/setup-github-repo.sh` and `scripts/setup-github-repo.ps1` — idempotent Dependabot alerts, private vulnerability reporting, branch protection/rulesets (CI + Security Scan + CodeQL); print UI fallback checklist on API 422
- [x] [AGENT] Add gitleaks CI job to `.github/workflows/security.yml` (or `ci.yml`) on PR + `main` push
- [x] [AGENT] Add `check-file-limits` and `validate-bootstrap --quick` to `.pre-commit-config.yaml`
- [x] [AGENT] Add `scripts/pre-release-gate.sh` and `scripts/pre-release-gate.ps1` — CI poll, Dependabot Critical/High count, template version/tag match, release dry-run reminder
- [x] [AGENT] Add KNOWLEDGE_BASE KB-007 (npm/pip overrides policy for transitive CVEs); document `@lhci/cli` override in DECISION_LOG
- [x] [AGENT] Add `npm audit` step to `examples/web` and `uv pip audit` (or equivalent) to weekly `.github/workflows/health-check.yml`
- [x] [AGENT] Sync `AGENT_MEMORY.md` seed template version with `.template-version`; fix stale `0.1.0` reference
- [x] [AGENT] Bump `.template-version` to `0.2.2`; update CHANGELOG, TEMPLATE_INDEX, README

## Sprint M1 — Template Hardening v0.3.0

- [x] [AGENT] Extend `init-project.sh` / `.ps1` with interactive stack picker (web / python / android / multi / none) — prune unused `examples/` and `modules/`, never delete LICENSE/CI/scripts
- [x] [AGENT] On init: sync `AGENT_MEMORY.md` active modules; emit minimal BUILD_PLAN Parallel section for chosen stack
- [x] [AGENT] Add `.cursor-session-state.example.json` schema; document restore flow in `docs/FOR_AGENTS.md`
- [x] [AGENT] Expand `docs/FOR_AGENTS.md` failure playbook (CI poll, GH_TOKEN, Dependabot conflicts, 3-strike escalation, parallel scope collision grep)
- [x] [AGENT] Add `android-release` CI job — `SOURCE_DATE_EPOCH=1700000000 ./gradlew assembleRelease`, FOSS grep, optional two-run APK hash compare with flake tolerance
- [x] [AGENT] Enforce `pytest --cov-fail-under=90` in CI for `examples/python`
- [x] [AGENT] Add Conventional Commits PR title check (`amannn/action-semantic-pull-request`) to `.github/workflows/ci.yml`
- [x] [AGENT] Draft `docs/adr/0001-core-architecture.md` pattern for child repos (MVVM / Clean / Hexagonal choice template)
- [x] [AGENT] Bump `.template-version` to `0.3.0`; update CHANGELOG, TEMPLATE_INDEX, README

## Sprint M2 — Template Features v0.4.0

- [x] [AGENT] Add `modules/node/MODULE.md` and `examples/node/` Golden Path stub (Fastify or Hono, MIT, typed, vitest)
- [x] [AGENT] Add Node CI job to `.github/workflows/ci.yml` (lint, test, locked install)
- [x] [AGENT] Add GitHub Pages deploy workflow for `examples/web` demo (FOSS, no tracking)
- [x] [AGENT] Add Dependabot auto-merge workflow — patch/minor only, requires CI + dependency-review pass, excludes major without `[HUMAN]` label
- [x] [AGENT] Add changelog automation (`release-please` or `git-cliff`) wired to Conventional Commits
- [x] [AGENT] Add `scripts/simulate-template-upgrade.sh` — clone, init, cherry-pick per `docs/UPGRADING_FROM_TEMPLATE.md`, assert validate-bootstrap passes
- [x] [AGENT] Add composite GitHub Action `action.yml` exporting `validate-bootstrap` for downstream repos
- [x] [AGENT] Bump `.template-version` to `0.4.0`; update CHANGELOG, TEMPLATE_INDEX, README
- [x] [AUTO] Upgrade simulation test passes in CI (optional scheduled job)
- [x] [AGENT] GitHub Actions stale bot (`actions/stale`); exempt `template-improvement` (`.github/workflows/stale.yml`)
- [x] [AGENT] PR coverage comment job (vitest + pytest artifacts; Codecov optional) (`.github/workflows/ci.yml`)
- [x] [AGENT] `scripts/generate-winget-manifest.sh` stub generator (`packaging/winget/**`, `scripts/`)
- [x] [AGENT] F-Droid `metadata/` template in `examples/android/` (`examples/android/metadata/**`)
- [x] [AGENT] Per-stack SBOM slices on GitHub Release (`examples/web`, `examples/python`) (`.github/workflows/release.yml`)
- [x] [AGENT] PROMPT_LIBRARY Entry 15 — Post-release regression (`PROMPT_LIBRARY.md`)
- [x] [AGENT] PROMPT_LIBRARY Entry 16 — Template upgrade simulation (`PROMPT_LIBRARY.md`)
- [x] [AGENT] Issue template: auto-suggest `.template-version` in placeholder text (`.github/ISSUE_TEMPLATE/*.yml`)

## Sprint M3 — Ecosystem Expansion v0.5.0+

- [x] [AGENT] Add `examples/lightroom/` minimal stub (`Info.lua`, SDK version doc) per `modules/lightroom/MODULE.md`
- [x] [AGENT] Update `TEMPLATE_INDEX.json` — set `examples/lightroom` module `example` path
- [x] [AGENT] (Optional) Add `modules/rust/MODULE.md` + `examples/rust/` stub behind stack picker
- [x] [AGENT] (Optional) Add `modules/go/MODULE.md` + `examples/go/` stub behind stack picker
- [x] [AGENT] Gate new module CI behind workflow matrix `inputs.stack` or path filters to control CI minutes

## Sprint M4 — Design System v0.6.0

- [x] [AGENT] Add `design-tokens/` + schema + `scripts/sync-design-tokens.py`
- [x] [AGENT] Migrate Android example to Compose M3 + theme toggle (DataStore) + `strings.xml` i18n
- [x] [AGENT] Refactor web example: CSS variables + theme toggle + `locales/` i18n scaffold
- [x] [AGENT] Add `docs/DESIGN_GUIDE.md` + `.cursor/rules/design-system.mdc`
- [x] [AGENT] Add `scripts/check-design-cohesion.sh` + validate-bootstrap wiring
- [x] [AUTO] `android-build` + web tests green (theme toggle smoke tests)
- [x] [AGENT] Web theme + i18n unit tests (`examples/web/src/theme.test.ts`, `examples/web/src/i18n/**`)
- [x] [AGENT] Android Compose theme components (`examples/android/.../ui/**`)

## Milestone Gates

- [x] [AUTO] Workflow action refs validated (`scripts/validate-workflow-actions.sh`)
- [x] [AUTO] Pre-commit bare-semver guard (`scripts/check-workflow-action-ref-format.sh`)
- [x] [AUTO] Android assembleDebug CI smoke on `examples/android/`
- [x] [AUTO] Weekly health-check workflow polls CI + Security Scan + CodeQL
- [x] [AUTO] UTF-8 encoding check clean (`scripts/check-file-encoding.sh`)
- [x] [AUTO] Lockfiles present and CI uses locked installs (`npm ci`, `uv sync --locked`)
- [x] [AUTO] `TEMPLATE_INDEX.json` complete (`scripts/validate-template-index.sh`)
- [x] [AUTO] Gitleaks CI job passes on `main` (M0)
- [x] [AUTO] Pre-commit includes file-limits and quick bootstrap validation (M0)
- [x] [AUTO] Android `assembleRelease` with `SOURCE_DATE_EPOCH` passes (M1)
- [x] [AUTO] Python coverage ≥ 90% in CI (M1)
- [x] [AUTO] Web bundle size budget within threshold (M1)
- [x] [AUTO] OpenSSF Scorecard run completed within last 30 days (M1)
- [x] [AUTO] Upgrade simulation test passes (M2)
- [x] [AUTO] GitHub Pages demo deploys successfully (M2)
- [x] [AUTO] Node example CI green when `examples/node/` present (M2)
## BUILD_PLAN Automation Pass (2026-06-13)

### Sprint 0 — Template (maintainer repo complete)

- [x] [AGENT] Create `SECURITY.md`, `CODE_OF_CONDUCT.md`, `docs/THREAT_MODEL.md`, `docs/PRIVACY.md`, `docs/RUNBOOK.md`
- [x] [AGENT] Add `.github/CODEOWNERS` and `THIRD_PARTY_LICENSES.md`
- [x] [AGENT] Initialize workspace memory files from template seeds (`AGENT_MEMORY.md`, etc.)
- [x] [AGENT] Wire update checker config into devcontainer and README
- [x] [HUMAN] Set GitHub repo About description from `docs/GITHUB_ABOUT.md` (via `gh repo edit`)
- [x] [AGENT] Commit lockfiles (`package-lock.json`, `uv.lock`) and `.env.example`
- [x] [AGENT] Ensure `TEMPLATE_INDEX.json` includes all scripts, workflows, and playbooks
- [x] [AUTO] `scripts/check-file-encoding.sh` passes
- [x] [AUTO] Full Build Verification Gate (INITIALIZATION_PROMPT Section 7) green
- [x] [AUTO] `scripts/validate-bootstrap.sh` (expanded) passes in CI
- [x] [HUMAN] Enable Dependabot alerts + security updates
- [x] [HUMAN] Enable private vulnerability reporting + branch protection on `main` (via `setup-github-repo.sh`)
- [x] [HUMAN] Replace `@[PROJECT_OWNER]` in CODEOWNERS with `@edwardlthompson`

### Sprint 0 Parallel (maintainer)

- [x] [AGENT] Confirm GitHub Pages uses Actions (not `/docs` folder)
- [x] [AUTO] Verify pre-commit hooks install

### Sprint 1 — Golden Path (maintainer)

- [x] [AGENT] Propose directory structure for target stack
- [x] [AGENT] Draft ADR-0001 core architecture (`docs/adr/0001-core-architecture.md`)
- [x] [AGENT] Implement Golden Path reference feature (design tokens, i18n, theme toggle)
- [x] [AUTO] `scripts/check-design-cohesion.sh` passes
- [x] [AUTO] CI matrix green on main
- [x] [AGENT] Web PWA offline cache + bundle budget + visual snapshots
- [x] [AGENT] Python CLI + 90% coverage gate + pyright
- [x] [AGENT] Android FOSS skeleton + Fastlane metadata stub
- [x] [AGENT] Node API stub
- [x] [AGENT] CodeQL + Trivy workflow wiring
- [x] [AGENT] Devcontainer + pre-commit hooks

### Sprint M0 Parallel

- [x] [AGENT] Cross-platform `scripts/check-file-encoding.py` (UTF-8/UTF-16 BOM)
- [x] [AGENT] Add `.cursor/rules/windows-encoding.mdc`
- [x] [AGENT] Add PROMPT_LIBRARY Entry 10 — Pre-release gate
- [x] [AGENT] Add PROMPT_LIBRARY Entry 11 — GitHub repo setup
- [x] [AGENT] Document setup script in `docs/SECURITY_TRIAGE.md` § Setup
- [x] [AGENT] Wire `setup-github-repo` reminder into `init-project.sh` / `.ps1`
- [x] [AUTO] Full Build Verification Gate + `scripts/pre-release-gate.sh` green on `main`

### Sprint M1 Parallel

- [x] [AGENT] Web bundle size budget in CI (`scripts/check-bundle-size.sh`)
- [x] [AGENT] Playwright visual snapshot regression test
- [x] [AGENT] Service-worker offline smoke test
- [x] [AGENT] Android Fastlane metadata stub
- [x] [AGENT] Android emulator checklist in `examples/android/README.md`
- [x] [AGENT] Optional pyright CI job for Python
- [x] [AGENT] Add `.cursor/rules/testing.mdc` (coverage budgets)
- [x] [AGENT] Add `.cursor/rules/ci-gates.mdc` (post-push poll protocol)
- [x] [AGENT] PROMPT_LIBRARY Entry 12 — Stack prune complete
- [x] [AGENT] PROMPT_LIBRARY Entry 13 — Session state restore
- [x] [AGENT] PROMPT_LIBRARY Entry 14 — Parallel agent scope map
- [x] [AGENT] OpenSSF Scorecard weekly workflow
- [x] [AGENT] `scripts/check-parallel-scope.sh`
- [x] [AUTO] CI matrix green including `android-release` and coverage gate
- [x] [AGENT] Conventional Commits PR title check (`amannn/action-semantic-pull-request`)

### Sprint M3 Parallel

- [x] [HUMAN] Decide which optional modules to ship — all three (Lightroom, Rust, Go); see `DECISION_LOG.md`
- [x] [AGENT] Lightroom lint/checklist in CI (Lua SDK namespace grep)
- [x] [AGENT] Rust CI job (`cargo fmt`, `clippy`, `test`)
- [x] [AGENT] Go CI job (`go vet`, `gofmt`, `test`)
- [x] [AGENT] F-Droid submission dry-run checklist doc (`modules/android/MODULE.md`)

### Milestone Gates

- [x] [AUTO] Regression tests: zero failures
- [x] [AUTO] Static analysis and vulnerability scans clean
- [x] [AUTO] `scripts/pre-release-gate.sh` passes before release tag (M0)

