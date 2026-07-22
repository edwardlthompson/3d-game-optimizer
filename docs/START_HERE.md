# Start Here

> **Read this file first** — whether you are a human or a Cursor agent.

## What is this?

**3D Game Optimizer** is a FOSS WinUI 3 / .NET 8 desktop hub for glasses-free 3D PC gaming, plus a public catalog site and optional Steam library sync worker. Agent process and tooling come from [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) (see `.template-version`).

Alignment status: [`docs/BOOTSTRAP_ALIGNMENT.md`](BOOTSTRAP_ALIGNMENT.md).

## Which mode are you in?

This is a **live product repo** (Reference mode), not a fresh template bootstrap.

- **Reference (default):** Read `FOR_AGENTS.md` next, then `AGENTS.md` + `BUILD_PLAN.md` Sequential lane
- **Bootstrap / re-init only:** Rare — use `INITIALIZATION_PROMPT.md` if re-running Sprint 0 style setup; do not overwrite product golden paths

## Cursor IDE mode (Ask / Plan / Agent / Debug)

Pick the correct Cursor mode before editing: [`docs/CURSOR_MODES.md`](CURSOR_MODES.md).

## Product read order (Reference)

1. `docs/START_HERE.md` (this file)
2. `docs/FOR_AGENTS.md`
3. `TEMPLATE_INDEX.json`
4. `AGENTS.md`
5. `BUILD_PLAN.md` Sequential lane
6. Active modules only (see `.cursor/stack-selection.json`):
   - `modules/winui/MODULE.md` → `src/SpatialLabsOptimizer*`
   - `modules/web/MODULE.md` → `site/catalog/`
   - `modules/node/MODULE.md` → `workers/steam-library/`
   - `modules/python/MODULE.md` → `scripts/sync-catalog/`
7. `docs/WEB_PROJECT_LAYOUT.md` for Pages / catalog hosting
8. `docs/DESIGN_GUIDE.md` / `docs/DESIGN_SYSTEM.md` for UI tokens
9. `.cursor/commands/README.md` — slash commands (`/build`, `/gates`, `/ship`, …)

## Do Not Read Yet

- Inactive `examples/` folders (stubs only; product code is under `src/`, `site/`, `workers/`)
- Entire `KNOWLEDGE_BASE.md` unless debugging
- `docs/MAINTAINING_THE_TEMPLATE.md` (template maintainers only)

## Slash commands

Type `/` in Cursor Agent chat. Workflows live in [`.cursor/commands/`](../.cursor/commands/README.md).

| Common | Command |
|--------|---------|
| Feature batch | `/build` |
| Local gates | `/gates` |
| Release | `/ship` or `/push` |
| After AGENT step | `/fix` or `watch-agent-gates.sh` |
| Archive done rows | `/cleanup` |
| Batch cheat sheet | [`docs/help/BATCH_COMMANDS.md`](help/BATCH_COMMANDS.md) |

## BUILD_PLAN Labels

`AGENT` | `HUMAN` | `ADB` | `AUTO` — filter with `grep '\[AGENT\]' BUILD_PLAN.md`

Status markers: 🔲 open · ✅ done · ❌ blocked (emoji only — never GitHub `- [ ]` checkboxes).

## Security

Dependabot alerts + weekly CVE triage: `docs/SECURITY_TRIAGE.md`. Vulnerability reporting: `SECURITY.md`.

## Agent prompt (this repo)

Read @docs/START_HERE.md, @docs/FOR_AGENTS.md, and @TEMPLATE_INDEX.json. Apply matching rules for active stacks only. Do not copy `examples/` wholesale. Prefer Sequential lane before Parallel. After each `[AGENT]` step: `bash scripts/watch-agent-gates.sh --once --autofix`.
