# Security Triage

Weekly CVE triage playbook for Dependabot alerts and release security gates.

## Setup (one-time, [AUTO])

1. Open GitHub -> **Settings** -> **Code security and analysis**
2. Enable **Dependabot alerts** and **Dependabot security updates** (CVE advisories on dependencies)
3. Enable **Private vulnerability reporting** (Settings -> Code security -> Private vulnerability reporting)
4. Verify `.github/dependabot.yml` exists for each active package ecosystem

**Automated setup (recommended):** run the idempotent script after clone or init:

```bash
bash scripts/setup-github-repo.sh
# Windows:
pwsh scripts/setup-github-repo.ps1
bash scripts/verify-github-settings.sh
```

Requires `gh` CLI authenticated with admin access. On API `422` (plan or permission limits), the script prints a manual UI checklist. Re-run after fixing permissions.

5. Configure branch protection on `main` requiring status checks: **CI**, **Security Scan**, **CodeQL** (script attempts this via API; verify in Settings -> Branches)

**Public repos:** Dependabot alerts are free.

`dependabot.yml` schedules version-update PRs; **Dependabot alerts** are a separate GitHub setting for CVE advisories - both are required.

## Automated Weekly Triage

`.github/workflows/security-triage.yml` runs every **Monday 06:30 UTC** (and on `workflow_dispatch`):

1. Counts open Critical/High Dependabot alerts
2. Enables auto-merge on open Dependabot PRs (patch/minor via `dependabot-automerge.yml`)
3. Opens or updates a GitHub issue labeled **`security-triage`**
4. Closes the triage issue when alert count is zero

Gate script: `bash scripts/check-security-triage.sh` (7-day recency; bootstrap grace when no triage issues exist yet).

## Weekly Triage Pass (manual supplement)

Recommended cadence: **Monday** (aligned with scheduled security scans and `health-check.yml`).

| Step | Owner | Action |
|------|-------|--------|
| 1 | AUTO | `security-triage.yml` summarizes Critical/High alerts in a `security-triage` issue |
| 2 | AUTO | Dependabot patch/minor PRs auto-merge when CI + Dependency Review pass |
| 3 | AGENT | Apply remaining dependency bumps, run tests locally, open PRs as needed |
| 4 | AUTO | CI (Trivy, CodeQL, matrix tests) validates merges |
| 5 | HUMAN | Merge major bumps or escalate deferred items (requires `HUMAN` label on Dependabot PR) |
| 6 | AUTO | `health-check.yml` weekly run (Monday 07:00 UTC); `check-security-triage.sh` |

## Triage Decisions

| Decision | When | Action |
|----------|------|--------|
| **Fix** | Patch available, low risk | Merge Dependabot PR or [AGENT] applies bump |
| **Defer** | No fix yet, acceptable risk window | Open issue with expiry date; log in DECISION_LOG.md |
| **Dismiss** | False positive or not applicable | Document rationale in issue or ADR |

After triage, confirm Trivy and CodeQL workflows are green on `main`.

## GitHub Actions Pin Policy

Third-party workflow actions must use **immutable refs** to reduce supply-chain risk (see Trivy action advisory, March 2026).

| Rule | Detail |
|------|--------|
| **Allowed** | `@vX.Y.Z` (-v prefix semver) or full commit SHA with `# vX.Y.Z` comment |
| **Forbidden** | Bare semver (`@0.28.0`), floating `@v0` / `@main`, unpinned third-party actions |
| **Pre-push** | Run `scripts/validate-workflow-actions.sh` (requires `gh` + `GH_TOKEN`) |
| **Local fast guard** | `scripts/check-workflow-action-ref-format.sh` (pre-commit; no network) |
| **Post-push** | `scripts/check-github-ci.sh --wait 300` - required workflows: **CI**, **Security Scan**, **CodeQL** |

## Release Gate (mandatory before tag)

Automated via `scripts/pre-release-gate.sh` and `.github/workflows/release-auto-merge.yml`:

- [AUTO] Weekly triage within last **7 days** (`scripts/check-security-triage.sh`)
- [AUTO] Zero open **Critical/High** Dependabot alerts (or `--allow-exception ISSUE_URL`)
- [AUTO] CHANGELOG section matches `.template-version`
- [AUTO] License compliance and state persistence tests pass
- [AUTO] CI, Security Scan, CodeQL green on `main`

If a Critical/High alert has no upstream fix, release may proceed only when:

1. A linked issue documents the advisory, impact, and mitigation
2. Issue URL passed to `pre-release-gate.sh --allow-exception`

## Related Files

| File | Purpose |
|------|---------|
| `.github/dependabot.yml` | Weekly grouped version-update PRs |
| `.github/workflows/security.yml` | Trivy filesystem scan |
| `.github/workflows/codeql.yml` | CodeQL static analysis |
| `.github/workflows/health-check.yml` | Weekly CI + Security Scan + CodeQL status on main |
| `scripts/validate-workflow-actions.sh` | Resolve action refs via GitHub API |
| `scripts/check-workflow-action-ref-format.sh` | Local bare-semver guard |
| `scripts/check-github-ci.sh` | Post-push workflow gate |
| `scripts/setup-github-repo.sh` | One-time Dependabot + reporting + branch protection setup |
| `docs/MAINTAINING_THE_TEMPLATE.md` | Maintainer release checklist |
| `docs/INITIALIZATION_PROMPT.md` | Section 7 pre-release gate |
