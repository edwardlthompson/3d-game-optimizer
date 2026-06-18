# Release: commit, push, and update docs

Framework: AGENT/HUMAN/ADB/AUTO; dual release tracks (template `v*` + product `SpatialLabsOptimizer-v*`).
**User invoked `/push` — explicit approval for `git push origin main`.**

## Step 1 — Pre-release validation

- `bash scripts/validate-bootstrap.sh --quick`
- `bash scripts/feature-gate.sh --stack product`
- `bash scripts/check-license-compliance.sh` after `npm ci` in `site/catalog` and `workers/steam-library` when changed
- Verify @README.md via `bash scripts/check-readme-health.sh --quick`
- Update @CHANGELOG.md `[Unreleased]` or product section

## Step 2 — Release notes

Create/update `docs/RELEASE_NOTES_vX.Y.Z.md` from CHANGELOG, BUILD_PLAN rows, recent commits.

## Step 3 — Commit and push

- Stage **explicit paths only** (never `git add .`)
- Conventional Commits; product releases use `feat:` / `fix:` / `chore(release):`
- `git push origin main`
- `bash scripts/check-github-ci.sh --wait 600` (or `pwsh scripts/check-github-ci.ps1 -WaitSeconds 600`)
- Zero open Critical/High Dependabot alerts

## Step 4 — Product release

- Tag `SpatialLabsOptimizer-vX.Y.Z` or dispatch `product-release.yml` with tag input
- Attach zip + MSI via Product Release workflow
- Update @AGENT_MEMORY.md and @DECISION_LOG.md at milestone boundary

## Step 5 — Cleanup

Mark BUILD_PLAN ✅; archive sprint to @COMPLETED_TASKS.md if applicable.

Do not force-push, amend published tags, or disable hooks. Halt and escalate [HUMAN] on failure.

Start executing now.
