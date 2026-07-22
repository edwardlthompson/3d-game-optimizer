# Release notes — template alignment v0.15.0

> Bootstrap / agent-tooling track only. Product remains **SpatialLabsOptimizer v1.5.0** (no new product tag).

## Summary

Aligned this live product repo with [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) **v0.15.0** (from 0.7.1). Application code, product CI workflows, and Steam Connect HUMAN blockers are unchanged.

## Added

- Cursor rules: modes, batch-commands, local-compute, repo-hygiene, security-triage
- Batch command docs (`docs/BATCH_COMMANDS.md`, `docs/help/`), `/cleanup`
- Cursor skills, agents, feature radar, FOSS integrations docs
- Opt-in hooks (`.cursor/hooks.json` with `<!-- cursor-hooks: off -->` in BUILD_PLAN)
- `HUMAN_BACKLOG.md`, `.cursorignore`, hygiene/size guides
- Dispatch / template-sync / cursor-check scripts

## Changed

- Product-aware `docs/START_HERE.md` (Reference-first)
- `.template-version` / manifest / `TEMPLATE_INDEX.json` → **0.15.0**
- BUILD_PLAN status glyphs 🔲 / ✅ / ❌
- README “How agents should work”

## Skipped (intentional)

- `commercial-compliance.mdc` and commercial Cursor examples
- Replacement of product `ci.yml` / product-release / Steam / catalog workflows

## Validation (local)

- `validate-bootstrap.sh --quick` PASS
- `dotnet test` 225/225 PASS
- Worker Vitest 35/35 PASS
- Open Dependabot Critical/High: 0
- Feature-gate WinUI stage needs Windows `dotnet` on PATH (bash/WSL may miss it); CI WinUI job is authoritative

## Still HUMAN

- Steam Connect KV + secrets + post-deploy smoke
- Optional: enable Cursor hooks after smoke
- Hardware QA / screenshots / CodeQL SARIF strict

See `docs/BOOTSTRAP_ALIGNMENT.md`.
