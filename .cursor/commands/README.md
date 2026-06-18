# Cursor slash commands

Type `/` in Cursor Agent chat to invoke these workflows. Commands live in `.cursor/commands/` (from [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) v0.7.1+).

## Super workflows

| Command | Purpose |
|---------|---------|
| `/build` | Plan → (approve) → feature → gates |
| `/ship` | Prerelease → push → regress (grants push approval) |
| `/bootstrap` | Sprint 0: init → prune → setup → gates |
| `/verify` | docs → gates → ci |
| `/audit` | Full repo review + BUILD_PLAN execution |

## Planning & scope

| Command | Purpose |
|---------|---------|
| `/plan` | Plan Mode task with BUILD_PLAN + Critique |
| `/scope` | Check Parallel agent scope collisions |
| `/feature` | Execute one BUILD_PLAN feature row + watch gates |
| `/fix` | Gate autofix loop for current step |

## Validation & CI

| Command | Purpose |
|---------|---------|
| `/gates` | Local validate-bootstrap + feature-gate + hygiene |
| `/ci` | Poll GitHub Actions (`check-github-ci.sh`) |
| `/prerelease` | Product/template pre-release gate |
| `/regress` | Post-release regression checklist |
| `/debug` | Debug Mode playbook |

## Maintenance

| Command | Purpose |
|---------|---------|
| `/init` | Child-repo Sprint 0 checklist |
| `/setup` | GitHub repo settings via setup script |
| `/prune` | Optional stack pruning guidance |
| `/upgrade` | Template upgrade cherry-pick guide |
| `/maintain` | Quarterly maintenance scripts |
| `/dependabot` | CVE triage pass |
| `/triage` | Security triage issue workflow |
| `/docs` | README + doc health checks |
| `/compact` | Session checkpoint + clear chat |
| `/restore` | Restore from `.cursor-session-state.json` |
| `/push` | Commit, push, release notes (explicit push approval) |

## Product stack

This repo uses `--stack product` for gates (WinUI + `site/catalog/` + `workers/steam-library/`). See `AGENT_MEMORY.md` and `docs/FEATURE_MODULES.md`.

```bash
bash scripts/feature-gate.sh --stack product
bash scripts/watch-agent-gates.sh --once --autofix
```
