# Build Plan

> Active sprint board. Archives: [COMPLETED_TASKS.md](COMPLETED_TASKS.md) · Evidence: [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md)

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | One-time actions scripts cannot perform |
| `AUTO` | CI / scripts / bots |

**Status key:** ✅ done · ⬜ open

**Task format:** `- ⬜ [OWNER] Description — \`paths\`` · mark done with `✅`

Filter: `grep '\[AGENT\]' BUILD_PLAN.md` · Count open: `grep -c '^- ⬜' BUILD_PLAN.md`

---

## Status (2026-06-15)

- ✅ **Product sprints** — Sprints 32–52 and 40–44 shipped locally ([archive](COMPLETED_TASKS.md))
- ✅ **Tests** — 168/168 green; `run-post-sprint-validation.ps1` passed locally
- ✅ **Local release** — framework-dependent publish — [docs/LOCAL_RELEASE.md](docs/LOCAL_RELEASE.md)
- ✅ **Blocking release** — Sprint 39 ship gate merged to `main` (PR #2); Product Release dispatched

---

## Sprint 39 — Ship gate `[SEQUENTIAL]` (blocking release)

**Goal:** One approved PR lands local work on `main`; CI and release automation match reality.

**Automate (recommended):**

```powershell
# Preview only — no push/PR/merge
pwsh scripts/ship-sprint39-gate.ps1 -DryRun

# Push + open PR + wait for CI
pwsh scripts/ship-sprint39-gate.ps1 -ApproveRemote

# Full gate: merge + dispatch Product Release for v1.1.0
pwsh scripts/ship-sprint39-gate.ps1 -ApproveRemote -Merge -DispatchRelease
```

Requires `gh auth login`. Bash: `bash scripts/ship-sprint39-gate.sh -ApproveRemote -Merge -DispatchRelease`

- ✅ [HUMAN] Single commit/PR to `main` (workflows + product + scripts + docs) — PR #2 merged
- ⬜ [AUTO] Verify CI green on `main` — `.github/workflows/ci.yml` (post-merge flake: snapshot filename collision; fix pushed)
- ✅ [AUTO] Dispatch `product-release.yml` for `SpatialLabsOptimizer-v1.1.0` — run 27547509122 (waiting on CI)
- ⬜ [AUTO] Test count matches CI on remote (not hand-counted)
- ⬜ [AUTO] `product-release.yml` dispatchable from remote
- ✅ [AGENT] Legal consistency script — `scripts/check-legal-consistency.sh`
- ✅ [AGENT] Build artifacts excluded — `.gitignore`, `artifacts/`
- ✅ [AGENT] Update UX (badge, retry banner, v2 restart prompt) — Sprints 39–40
- ✅ [AGENT] Docs synced — `UX_PROGRESS.md`, `PLANNING_REVIEW.md`

**Exit criteria:**

- ⬜ `main` CI green (fix snapshot collision; re-run CI after push)
- ✅ Release workflow dispatchable from remote
- ✅ Local validation gates pass (`run-post-sprint-validation.ps1`)

---

## Open follow-ups `[HUMAN]`

- ✅ Sprint 39 ship gate — PR #2 merged to `main` (2026-06-15)
- ⬜ WinGet merge — [PR #387878](https://github.com/microsoft/winget-pkgs/pull/387878) CLA/validation pending
- ⬜ WinGet v1.1.0 manifest — after merge: `scripts/prepare-winget-submission.ps1 -OpenPr`
- ⬜ EV Authenticode cert — optional; sideload signing via `scripts/codesign-common.ps1`
- ⬜ Org admin for GHAS — optional if API returns 403 on security settings
- ⬜ Cover art on user hardware — confirm Sprint 50 build; capture `debug-2ca1ae.log` if tiles blank
- ⬜ ASV15 EDID capture — confirm `5986:PROD` on SpatialLabs View / View Pro 15.6"
- ⬜ SpatialLabs 15" laptop EDID capture — tighten laptop catalog wildcards after panel capture
- ⬜ ADR-0002 PCVR manual QA — [HARDWARE_QA_OUT_OF_BAND.md](docs/HARDWARE_QA_OUT_OF_BAND.md)

---

## Deferred (optional)

- ✅ Split `PlayIn3D.cs` and `PcvrServices.cs` — partials + `PcvrRuntimeConnector.cs`, `DiagnosticBundleService.cs`
- ✅ Append [DECISION_LOG.md](DECISION_LOG.md) — Sprint 44 residual + local ship readiness (2026-06-15)

---

## Documentation sync

- ✅ Archive sprints → [COMPLETED_TASKS.md](COMPLETED_TASKS.md) (through Sprint 44)
- ✅ [docs/PLANNING_REVIEW.md](docs/PLANNING_REVIEW.md) · [docs/QA_MATRIX.md](docs/QA_MATRIX.md) (P1_MAP)
- ✅ `dotnet test` · `run-post-sprint-validation.ps1` · `check-file-encoding.py`

---

## Release sign-off

| Gate | Status | Notes |
|------|--------|-------|
| CI + tests (`ci.yml`) | ⬜ | Post-merge snapshot flake; fix f633eb1 pushed |
| QA matrix P0–v1.5 | ✅ | Local — `check-qa-matrix-coverage.sh` |
| QA matrix P1 offline/Steam | ✅ | Local — Sprint 43 |
| Compatibility seed | ✅ | Local |
| Legal consistency | ✅ | Local |
| Local release scripts | ✅ | Local |
| Post-sprint validation | ✅ | Local — Sprint 44 |
| Sprint 39 ship gate | ✅ | PR #2 merged 2026-06-15 |
| README UI previews | ✅ | Local — `generate-brand-assets.py` |
| Release credentials | ⬜ | `check-release-credentials.sh` |
| Product pre-release gate | ⬜ | Run 27547509122 — re-dispatch after CI green |
| Product zip + MSI + MSIX + signing | ⬜ | After product-release completes |
| Template release (`release.yml`) | ⬜ | On `v*` tag |
| Quarterly maintenance | ⬜ | Scheduled workflow |

**One-time credentials (optional):**

- ⬜ [HUMAN] `bash scripts/setup-release-credentials.sh owner/repo`
- ⬜ [HUMAN] `bash scripts/check-release-credentials.sh owner/repo`
- ⬜ [HUMAN] GitHub Actions **Release Credentials Setup** workflow_dispatch

---

## Ongoing maintenance `[AUTO]`

- ⬜ Weekly `health-check.yml` + `scorecard.yml`
- ⬜ On PR: `ci.yml`
- ⬜ Monthly `license-audit.yml` + `template-upgrade-simulation.yml`
- ⬜ Quarterly `quarterly-maintenance.yml` + credential audit

---

## Completed sprints

Detail and task lists: [COMPLETED_TASKS.md](COMPLETED_TASKS.md). Feature recommendations **#33–66** are all shipped.

| Sprint | Theme |
|--------|-------|
| 28–31 | v1.0.1 closure |
| 32 | Distribution + local build/sign (zip, MSIX, MSI) |
| 33 | Trainer/mod-manager coexistence |
| 34 | Hardware honesty & trust UX |
| 35 | Library intelligence & local installs |
| 36 | v2 toggle, About updates, launch depth |
| 37 | Display, PCVR & command palette |
| 38 | Diagnostics & onboarding polish |
| 39 | CI truth + release ops *(AGENT items done; ship pending)* |
| 40 | MVVM and shell UX |
| 41 | Launch depth and honesty |
| 42 | v2 productization |
| 43 | QA gate hardening |
| 44 | Modularization (FutureServices, UseCases, Launch) |
| 45 | UX polish, detection, theme |
| 46 | Cover art, ASV15 catalog, silent install fix |
| 47 | Library connections, persistence, metadata |
| 48 | Cover art, wizard toolchain, 3D Display settings, icon |
| 49 | Review hardening & production gaps |
| 50 | Toolchain manifest, cover UX, archive hygiene |
| 51 | Modularization & deferred carryover |
| 52 | Vendor toolchain & platform deferred work |

Per-sprint archive links: [COMPLETED_TASKS.md](COMPLETED_TASKS.md).
