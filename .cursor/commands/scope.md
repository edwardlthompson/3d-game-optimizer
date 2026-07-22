# Parallel dispatch (manifest + scope lock)

> Skill: `.cursor/skills/parallel-scope/`

Read @docs/PARALLEL_AGENT_SCOPES.md and the active BUILD_PLAN Parallel table.

## 1. Preconditions

- Sequential schema-lock steps for the active sprint/feature are complete.
- Run:

```bash
bash scripts/check-parallel-scope.sh
```

Abort dispatch on overlap. Shared schema stays Sequential-only.

Optional manifest (when available):

```bash
python3 scripts/agent-run.py plan-parallel-dispatch --require-sequential-clear --json
```

## 2. Branching

Assign one branch per agent: `feature/agent-<task-slug>`.

## 3. After parallel work

Orchestrator runs `bash scripts/watch-agent-gates.sh --once --autofix` (or `python3 scripts/agent-run.py watch-agent-gates --once --autofix`). Parallel agents never edit `BUILD_PLAN.md`.

Begin now.
