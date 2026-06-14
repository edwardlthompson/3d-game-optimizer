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

_No project-specific decisions yet. The seed ADR is at `docs/adr/0001-template-baseline.md`._

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

