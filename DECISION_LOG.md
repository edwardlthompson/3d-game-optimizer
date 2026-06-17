# Decision Log

> Chronological register of major technical trade-offs, accepted architectures, and rejected alternatives.
> **Treat past entries as immutable history; append only.**

## Format

```markdown
### YYYY-MM-DD — [Title]
- **Status:** Accepted | Rejected | Superseded
- **Context:** ...
- **Decision:** ...
- **Alternatives considered:** ...
- **Consequences:** ...
```

## Entries

### 2026-06-15 — Sprint 39 ship gate v1.1.0 complete
- **Status:** Accepted
- **Context:** Local Sprints 32–52 blocked on single PR + remote CI/release
- **Decision:** Automated via `ship-sprint39-gate.ps1`; merged PR #2; Product Release 27548631008; post-merge CI flake fixed (snapshot filename GUID suffix)
- **Validation:** Remote CI 168/168; local `dotnet test` 168/168
- **Consequences:** BUILD_PLAN trimmed to active HUMAN follow-ups; WinGet v1.1.0 PR and hardware QA remain out-of-band

### 2026-06-15 — Sprint 44 residual file budget + local ship readiness
- **Status:** Accepted
- **Context:** BUILD_PLAN deferred items after Sprint 44 modularization; Sprint 39 ship gate still blocked on `[HUMAN]` PR to `main`
- **Decision:** Split `PlayIn3D` into partials (`PlayIn3D.cs`, `PlayIn3D.LaunchPath.cs`); extracted `PcvrRuntimeConnector` and `DiagnosticBundleService` from `PcvrServices.cs`; consolidated BUILD_PLAN to active-board format with ✅/⬜ status glyphs
- **Validation:** 168/168 `dotnet test`; `run-post-sprint-validation.ps1` green locally
- **Consequences:** Remote CI, `product-release.yml` dispatch, and WinGet v1.1.0 remain `[HUMAN]`/`[AUTO]` until single PR merges to `main`

### 2026-06-14 — Sprint 39 CI truth and update UX polish
- **Status:** Accepted
- **Context:** BUILD_PLAN Sprint 39 exit gate before landing Sprints 32–38 on `main`
- **Decision:** Fixed legal-consistency script paths; staged-update reuse with SHA-256 verify (`3DGO-0103`); About retry banner for `update_restart_pending`; shell update InfoBar; v2 toggle restart notice when DI differs from saved pref; 118/118 tests + post-sprint validation green
- **Validation:** `run-post-sprint-validation.ps1`, `check-file-encoding.py`, `dotnet test` Release
- **Consequences:** `product-release.yml` dispatch after push; WinGet v1.1.0 manifest remains `[HUMAN]` pending CLA on PR #387878

### 2026-06-17 — CI stabilization after v1.3.0
- **Status:** Accepted
- **Context:** v1.3.0 push broke CI (stale lockfile, invalid workflow `if`, stale action SHA, file limits)
- **Decision:** Three fix commits; worker deploy gated to workflow_dispatch; release-please manifest synced to 1.3.0
- **Validation:** All main workflows green; Dependabot 0 open; Product Release re-dispatched successfully

### 2026-06-17 — Drop WinGet; GitHub-only distribution
- **Status:** Accepted
- **Context:** MSIX removed; in-app `UpdateService` already uses GitHub Releases API; WinGet PR maintenance was unused overhead
- **Decision:** Removed all WinGet packaging, scripts, and CI manifest upload (product + template tracks)
- **Alternatives considered:** Keep WinGet for discoverability (rejected — GitHub Releases + in-app updater sufficient)
- **Consequences:** Close/abandon `microsoft/winget-pkgs` PRs; v1.3.0 release ships zip + MSI only

### 2026-06-16 — Drop MSIX; zip + MSI only
- **Status:** Accepted
- **Context:** MSIX sideload was optional, never shipped on GitHub Releases; user wants EXE (portable zip) + MSI only
- **Decision:** Removed `Package.appxmanifest`, `publish-product-msix.ps1`, `MsixUpdateApplier`, and CI/local MSIX build steps; splash uses `SplashLogo.png`
- **Alternatives considered:** Keep MSIX for future Store submission (rejected); remove portable zip and ship bare EXE only (rejected — zip is standard self-contained layout)
- **Consequences:** Legacy MSIX installs must migrate to MSI or zip; `install_artifact_type=Msix` prefs re-detect on read

### 2026-06-14 — Sprints 32–38 product roadmap closure
- **Status:** Accepted
- **Context:** BUILD_PLAN integration for distribution, coexistence, library, updates, diagnostics
- **Decision:** Shipped local release pipeline (zip+MSIX+WiX MSI), trainer/mod game-first launch, WMI hardware scan, local game folders, in-app update apply+restart (v1.1.0), display/PCVR UX, diagnostic dry-run and `3dgo://` protocol
- **Validation:** 114/114 Release tests; `dotnet build` WinUI + tests green
- **Alternatives considered:** Defer MSI to separate sprint (rejected: user requested classic installer in Sprint 32)
- **Consequences:** WinGet merge remains `[HUMAN]` until microsoft/winget-pkgs approves PR #387878

### 2026-06-14 — Winget manifest PR (SpatialLabsOptimizer v1.0.1)
- **Status:** Accepted
- **Context:** Sprint 31 optional `[HUMAN]` task — publish `edwardlthompson.SpatialLabsOptimizer` to WinGet after product release
- **Decision:** Created GitHub release `SpatialLabsOptimizer-v1.0.1` with self-contained zip; opened PR to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) using 1.6.0 multi-file schema (`zip` + `portable` nested installer)
- **PR:** https://github.com/microsoft/winget-pkgs/pull/387878
- **Alternatives considered:** Singleton stub only in product repo (rejected: winget-pkgs requires upstream PR); `edwardlthompson.3DGameOptimizer` identifier (rejected: align with CI `generate-winget-manifest.sh` and README path)
- **Consequences:** Source repo is public; PR merge blocked on CLA queue / branch policy as of 2026-06-14. Re-run validation after merge.

### 2026-06-14 — BUILD_PLAN maintenance closure (KB-007 + Scorecard)
- **Status:** Accepted
- **Context:** Remaining BUILD_PLAN items: Dependabot coverage, Scorecard triage, CodeQL/Release Please CI failures, KB-007 policy review
- **Decision:** Extend `dependabot.yml` with npm/pip ecosystems; gate major bumps via `dependabot-automerge.yml` + `scripts/check-kb007-policy.sh`; CodeQL/Scorecard use `upload: false` / `continue-on-error` until code scanning enabled in repo settings; workflow permissions allow Release Please PRs via `setup-github-repo.sh`
- **Alternatives considered:** Require manual code scanning enable before merge (deferred — documented in setup checklist)
- **Consequences:** OpenSSF SARIF upload is best-effort; enable **Settings → Code security → Code scanning** for full Security tab integration

### 2026-06-13 — @lhci/cli npm overrides for transitive CVEs
- **Status:** Accepted
- **Context:** Lighthouse CI (`@lhci/cli`) bundles transitive dependencies (`tmp`, `uuid`) with known CVEs; no patched `@lhci/cli` release available at triage time
- **Decision:** Add npm `overrides` in `examples/web/package.json` forcing `tmp >= 0.2.6` and `uuid >= 11.1.1`; document in KB-007
- **Alternatives considered:** Dismiss Dependabot alert (rejected: hides real risk); remove Lighthouse CI job (rejected: loses performance gate)
- **Consequences:** Lockfile must be regenerated after override changes; overrides should be removed when `@lhci/cli` ships fixed dependencies

### 2026-06-13 — Ship all optional ecosystem modules (M3)
- **Status:** Accepted
- **Context:** Sprint M3 asked whether to ship Lightroom, Rust, and Go optional modules in the template maintainer repo
- **Decision:** Ship all three with Golden Path stubs, MODULE.md guides, and path-gated CI jobs (`lightroom`, `rust`, `go`) that skip when child repos remove the directories
- **Alternatives considered:** Lightroom-only (rejected: Rust/Go stubs are low-cost and popular); defer all optional modules (rejected: COMPLETED_TASKS M3 work already landed)
- **Consequences:** Template CI runs more jobs on `main`; child repos can delete unused `examples/` folders to skip jobs via `hashFiles` guards

