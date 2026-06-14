# Build Plan

> Prioritized task board with owner labels. Completed sprints live in [COMPLETED_TASKS.md](COMPLETED_TASKS.md).

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

## Status (2026-06-14)

| Area | State |
|------|-------|
| Child repo Sprint 0–2 | Complete — [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
| Product Sprints 3–14 | Complete — [COMPLETED_TASKS.md](COMPLETED_TASKS.md) |
| Ongoing maintenance | Complete — recurring automation active |
| ADB / F-Droid | N/A — WinUI-only product (cancelled) |

**Board clear.** New work: add items below or open a sprint in COMPLETED_TASKS archive format.

---

## Release Sign-off (automated — reference)

| Check | Enforcer |
|-------|----------|
| CI + Security Scan + CodeQL green | `scripts/check-github-ci.sh` |
| Pre-release gate | `scripts/pre-release-gate.sh` |
| Release Please PR merge | `release-auto-merge.yml` |
| SBOM + Winget stub | `release.yml` on `release: published` |
| Scorecard recency | `scripts/check-scorecard-recency.sh` |
| KB-007 policy | `scripts/check-kb007-policy.sh` |

Local gates: `scripts/sprint-signoff-gate.sh` · `scripts/build-verification-gate.sh`

---

## Backlog (empty)

_Add the next sprint or maintenance item here._
